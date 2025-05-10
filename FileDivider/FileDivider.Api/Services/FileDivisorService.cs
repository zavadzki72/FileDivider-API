using FileDivider.Api.Data;
using FileDivider.Api.Dtos;
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
    public class FileDivisorService
    {
        private readonly MongoContext _context;

        public FileDivisorService(MongoContext context)
        {
            _context = context;
        }

        public async Task<FileDivisorResponse> DivideFile(FileDivisorRequest request)
        {
            var template = await _context.PdfTemplates.Find(x => x.Id == request.TemplateId).FirstOrDefaultAsync()
                ?? throw new ArgumentException("Template not found.");

            var startRegex = template.ExtractionHelper.First(x => x.Key == ExtractionHelperMandatoryValues.StartRegex).Value;

            var blocks = Regex.Split(request.FileContent, startRegex)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var valueToInsert = Regex.Matches(request.FileContent, startRegex)
                .Cast<Match>()
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Value)
                .First();

            var response = new FileDivisorResponse
            {
                FileName = $"Response_FileDivider_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}"
            };

            for (int i = 0; i < blocks.Count; i++)
            {
                var content = blocks[i].Trim();
                string fileNameTemplate = $"{request.FilesName}_{i + 1}";

                var extractedValues = new Dictionary<string, string>();

                foreach (var helper in template.ExtractionHelper)
                {
                    if (helper.Key == ExtractionHelperMandatoryValues.StartRegex)
                        continue;

                    var match = Regex.Match(content, helper.Value);
                    if (match.Success)
                    {
                        extractedValues[helper.Key] = match.Groups[1].Value.Replace("/", "-");
                    }
                }

                string finalFileName = ReplacePlaceholders(fileNameTemplate, extractedValues);
                finalFileName = SanitizeFileName(finalFileName);

                response.Files.Add(new FileDivisorItemResponse
                {
                    FileName = finalFileName,
                    Content = $"{valueToInsert}\n{content}"
                });
            }

            return response;
        }

        public async Task<byte[]> DivideFromFile(string fileName, Guid templateId, IFormFile formFile)
        {
            using var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            var pdfBytes = ms.ToArray();

            var template = await _context.PdfTemplates.Find(x => x.Id == templateId).FirstOrDefaultAsync()
                ?? throw new ArgumentException("Template not found.");

            var startRegexPattern = template.ExtractionHelper.First(x => x.Key == ExtractionHelperMandatoryValues.StartRegex).Value;
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
                foreach (var helper in template.ExtractionHelper)
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
