using System.Text;

namespace FileDivider.Api.Extensions
{
    public static class FormFileExtensions
    {
        public static async Task<List<string>> ReadAsList(this IFormFile file)
        {
            var result = new List<string>();
            using(var reader = new StreamReader(file.OpenReadStream()))
            {
                while(reader.Peek() >= 0)
                    result.Add((await reader.ReadLineAsync()) ?? "");
            }
            return result;
        }

        public static async Task<string> ReadAsString(this IFormFile file)
        {
            var result = new StringBuilder();
            using(var reader = new StreamReader(file.OpenReadStream()))
            {
                while(reader.Peek() >= 0)
                    result.AppendLine(await reader.ReadLineAsync());
            }
            return result.ToString();
        }
    }
}
