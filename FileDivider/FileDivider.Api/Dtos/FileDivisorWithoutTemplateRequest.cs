namespace FileDivider.Api.Dtos
{
    public class FileDivisorWithoutTemplateRequest
    {
        public required IFormFile FormFile { get; set; }
        public required string FileName { get; set; }
        public required Dictionary<string, string> ExtractorHelper { get; set; }
    }
} 