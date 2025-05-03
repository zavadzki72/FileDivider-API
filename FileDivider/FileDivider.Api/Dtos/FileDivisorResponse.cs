namespace FileDivider.Api.Dtos
{
    public class FileDivisorResponse
    {
        public string FileName { get; set; } = string.Empty;
        public List<FileDivisorItemResponse> Files { get; set; } = [];
    }

    public class FileDivisorItemResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
