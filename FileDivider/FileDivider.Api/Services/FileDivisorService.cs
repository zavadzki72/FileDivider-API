using System.Text.RegularExpressions;
using FileDivider.Api.Data;
using FileDivider.Api.Dtos;
using FileDivider.Api.Models;
using MongoDB.Driver;

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
