using FileDivider.Api.Dtos;
using FileDivider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileDivider.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileDivisorController(FileDivisorService service) : ControllerBase
    {
        private readonly FileDivisorService _service = service;

        [HttpPost("pdf")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DividePdf(IFormFile formFile, Guid templateId, string fileName)
        {
            var zipBytes = await _service.DividePdf(fileName, templateId, formFile);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}.zip");
        }

        [HttpPost("pdf/without-template")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DividePdfWithoutTemplate([FromForm] FileDivisorWithoutTemplateRequest request)
        {
            var zipBytes = await _service.DividePdfWithoutTemplate(request.FileName, request.FormFile, request.ExtractorHelper);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}.zip");
        }

        [HttpPost("txt/without-template")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DivideTxtWithoutTemplate([FromForm] Dtos.FileDivisorWithoutTemplateRequest request)
        {
            var zipBytes = await _service.DivideTxtWithoutTemplate(request.FileName, request.FormFile, request.ExtractorHelper);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}.zip");
        }

        [HttpPost("txt/by-line")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DivideTxtByLine(IFormFile formFile, int line)
        {
            var zipBytes = await _service.DivideTxtByLines(formFile, line);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}.zip");
        }
    }
}
