using FileDivider.Api.Data;
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

        public async Task<byte[]> DivideFromFile(string fileName, Guid templateId, IFormFile formFile)
        {
            var template = await _context.PdfTemplates.Find(x => x.Id == templateId).FirstOrDefaultAsync()
                ?? throw new ArgumentException("Template not found.");

            return await DivideFile(fileName, formFile, template.ExtractionHelper);
        }

        public async Task<byte[]> DivideFromFileWithoutTemplate(string fileName, IFormFile formFile, Dictionary<string, string> extractorHelper)
        {
            return await DivideFile(fileName, formFile, extractorHelper);
        }

        private static async Task<byte[]> DivideFile(string fileName, IFormFile formFile, Dictionary<string, string> extractorHelper)
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
                string cleanName = SanitizeFileName($"{baseName}_Bloco_{i + 1}");

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
