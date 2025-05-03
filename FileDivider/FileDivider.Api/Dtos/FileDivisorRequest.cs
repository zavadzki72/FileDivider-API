namespace FileDivider.Api.Dtos
{
    public class FileDivisorRequest
    {
        public Guid TemplateId { get; set; }
        public string FileContent { get; set; } = string.Empty;
        public string FilesName { get; set; } = string.Empty;
    }
}
