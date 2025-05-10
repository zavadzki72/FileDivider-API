using FileDivider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileDivider.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileDivisorController(FileDivisorService service) : ControllerBase
    {
        private readonly FileDivisorService _service = service;

        [HttpPost("from-file")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DivideFromFile(IFormFile formFile, Guid templateId, string fileName)
        {
            var zipBytes = await _service.DivideFromFile(fileName, templateId, formFile);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}.zip");
        }

        [HttpPost("from-file-without-template")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DivideFromFileWithoutTemplate(IFormFile formFile, string fileName, Dictionary<string, string> extractorHelper)
        {
            var zipBytes = await _service.DivideFromFileWithoutTemplate(fileName, formFile, extractorHelper);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}.zip");
        }
    }
}
