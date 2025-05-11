using FileDivider.Api.Data;
using FileDivider.Api.Extensions;
using FileDivider.Api.Models;
using MongoDB.Driver;
using PdfSharpCore.Pdf.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;
using PdfSharpDocument = PdfSharpCore.Pdf.PdfDocument;

namespace FileDivider.Api.Services
{
    public class FileDivisorService(MongoContext context)
    {
        private readonly MongoContext _context = context;

        public async Task<byte[]> DividePdf(string fileName, Guid templateId, IFormFile formFile)
        {
            var template = await _context.PdfTemplates.Find(x => x.Id == templateId).FirstOrDefaultAsync()
                ?? throw new ArgumentException("Template not found.");

            return await DividePdf(fileName, formFile, template.ExtractionHelper);
        }

        public async Task<byte[]> DividePdfWithoutTemplate(string fileName, IFormFile formFile, Dictionary<string, string> extractorHelper)
        {
            return await DividePdf(fileName, formFile, extractorHelper);
        }

        public async Task<byte[]> DivideTxtWithoutTemplate(string fileName, IFormFile formFile, Dictionary<string, string> extractorHelper)
        {
            using var reader = new StreamReader(formFile.OpenReadStream());
            var fullText = await reader.ReadToEndAsync();

            var startRegexPattern = extractorHelper.First(x => x.Key == ExtractionHelperMandatoryValues.StartRegex).Value;
            var startRegex = new Regex(startRegexPattern, RegexOptions.Multiline);

            var matches = startRegex.Matches(fullText);

            if (matches.Count == 0)
                throw new InvalidOperationException("Nenhum bloco identificado com o StartRegex.");

            var blockIndexes = matches.Cast<Match>().Select(m => m.Index).ToList();
            blockIndexes.Add(fullText.Length);

            var outputFiles = new List<(string FileName, string Content)>();

            for (int i = 0; i < blockIndexes.Count - 1; i++)
            {
                int start = blockIndexes[i];
                int length = blockIndexes[i + 1] - start;
                var blockText = fullText.Substring(start, length);

                var extractedValues = new Dictionary<string, string>();
                foreach (var helper in extractorHelper)
                {
                    if (helper.Key == ExtractionHelperMandatoryValues.StartRegex)
                        continue;

                    var match = Regex.Match(blockText, helper.Value);
                    if (match.Success)
                    {
                        extractedValues[helper.Key] = match.Groups[1].Value.Replace("/", "-");
                    }
                }

                string baseName = ReplacePlaceholders(fileName, extractedValues);
                string cleanName = extractedValues.Count == 0 ? SanitizeFileName($"{baseName}_{i + 1}") : SanitizeFileName($"{baseName}");
                outputFiles.Add((cleanName, blockText));
            }

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var (FileName, Content) in outputFiles)
                {
                    var entry = archive.CreateEntry($"{FileName}.txt");
                    using var entryStream = new StreamWriter(entry.Open());
                    entryStream.Write(Content);
                }
            }

            return zipStream.ToArray();
        }

        public async Task<byte[]> DivideTxtByLines(IFormFile formFile, int linesLimit)
        {
            if (linesLimit < 10)
                throw new ArgumentException("O Numero de linhas precisa ser no minimo 10.");

            var lines = await formFile.ReadAsList();
            lines = [.. lines.Where(x => !string.IsNullOrWhiteSpace(x))];

            var linesChunck = lines
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / linesLimit)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            List<byte[]> byteFiles = [];

            foreach (var lineChunck in linesChunck)
            {
                using var ms = new MemoryStream();
                using TextWriter tw = new StreamWriter(ms);

                var last = lineChunck.Last();
                foreach (var line in lineChunck)
                {
                    if (line.Equals(last))
                    {
                        tw.Write(line);
                    } 
                    else
                    {
                        tw.WriteLine(line);
                    }

                }

                tw.Close();
                byteFiles.Add(ms.ToArray());
            }

            byte[] result = [];
            int count = 1;

            using MemoryStream zipArchiveMemoryStream = new();
            using (ZipArchive zipArchive = new(zipArchiveMemoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in byteFiles)
                {
                    var fileName = $"Arquivo_{count}.txt";

                    ZipArchiveEntry zipEntry = zipArchive.CreateEntry(fileName);
                    using Stream entryStream = zipEntry.Open();

                    using (MemoryStream tmpMemory = new(file))
                    {
                        tmpMemory.CopyTo(entryStream);
                    }

                    count++;
                }
            }

            zipArchiveMemoryStream.Seek(0, SeekOrigin.Begin);
            return zipArchiveMemoryStream.ToArray();
        }

        private static async Task<byte[]> DividePdf(string fileName, IFormFile formFile, Dictionary<string, string> extractorHelper)
        {
            using var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            var pdfBytes = ms.ToArray();

            var startRegexPattern = extractorHelper.First(x => x.Key == ExtractionHelperMandatoryValues.StartRegex).Value;
            var startRegex = new Regex(startRegexPattern, RegexOptions.Multiline);

            var pages = new List<(int Number, string Text)>();
            using (var doc = PdfPigDocument.Open(pdfBytes))
            {
                int pageNumber = 1;
                foreach (var page in doc.GetPages())
                {
                    pages.Add((pageNumber++, page.Text));
                }
            }

            var startPages = new List<int>();
            foreach (var (Number, Text) in pages)
            {
                if (startRegex.IsMatch(Text))
                    startPages.Add(Number);
            }

            if (startPages.Count == 0)
                throw new InvalidOperationException("Nenhum bloco identificado com o StartRegex.");

            startPages.Add(pages.Count + 1);

            using var inputDoc = PdfReader.Open(new MemoryStream(pdfBytes), PdfDocumentOpenMode.Import);
            var outputFiles = new List<(string FileName, byte[] Content)>();

            for (int i = 0; i < startPages.Count - 1; i++)
            {
                int start = startPages[i] - 1;
                int end = startPages[i + 1] - 2;

                var blockText = new StringBuilder();
                for (int j = start; j <= end; j++)
                    blockText.AppendLine(pages[j].Text);

                var extractedValues = new Dictionary<string, string>();
                foreach (var helper in extractorHelper)
                {
                    if (helper.Key == ExtractionHelperMandatoryValues.StartRegex)
                        continue;

                    var match = Regex.Match(blockText.ToString(), helper.Value);
                    if (match.Success)
                    {
                        extractedValues[helper.Key] = match.Groups[1].Value.Replace("/", "-");
                    }
                }

                string baseName = ReplacePlaceholders(fileName, extractedValues);
                string cleanName = extractedValues.Count == 0 ? SanitizeFileName($"{baseName}_{i + 1}") : SanitizeFileName($"{baseName}");

                var newDoc = new PdfSharpDocument();
                for (int j = start; j <= end; j++)
                {
                    newDoc.AddPage(inputDoc.Pages[j]);
                }

                using var msOut = new MemoryStream();
                newDoc.Save(msOut, false);

                outputFiles.Add((cleanName, msOut.ToArray()));
            }

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var (FileName, Content) in outputFiles)
                {
                    var entry = archive.CreateEntry($"{FileName}.pdf");
                    using var entryStream = entry.Open();
                    entryStream.Write(Content, 0, Content.Length);
                }
            }

            return zipStream.ToArray();
        }

        private static string ReplacePlaceholders(string template, Dictionary<string, string> values)
        {
            foreach (var placeholder in values)
            {
                string placeholderKey = $"{{{placeholder.Key}}}";
                if (template.Contains(placeholderKey))
                {
                    template = template.Replace(placeholderKey, placeholder.Value);
                }
            }

            return template;
        }

        private static string SanitizeFileName(string name)
        {
            return string.Concat(name.Where(x => !Path.GetInvalidFileNameChars().Contains(x)))
                .Replace(" ", "_")
                .Replace(":", "-")
                .Replace("/", "-")
                .ToUpperInvariant();
        }
    }
}
