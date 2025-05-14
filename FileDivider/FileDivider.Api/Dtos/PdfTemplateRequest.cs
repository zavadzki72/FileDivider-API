namespace FileDivider.Api.Dtos
{
    public class PdfTemplateRequest
    {
        public required string Name { get; set; }
        public Dictionary<string, string> ExtractionHelpers { get; set; } = [];
    }
}
