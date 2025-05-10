using FileDivider.Api.Dtos;
using FileDivider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileDivider.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileDivisorController : ControllerBase
    {
        private readonly FileDivisorService _service;

        public FileDivisorController(FileDivisorService service)
        {
            _service = service;
        }

        [HttpPost("from-text")]
        public async Task<IActionResult> Post(FileDivisorRequest request)
        {
            var response = await _service.DivideFile(request);
            return Ok(new { 
                Data = response
            });
        }

        [HttpPost("from-file")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DivideFromFile(IFormFile formFile, Guid templateId, string fileName)
        {
            var zipBytes = await _service.DivideFromFile(fileName, templateId, formFile);
            return File(zipBytes, "application/zip", $"FileDivider_{DateTime.Now.ToString("dd-MM-yyyy:HH:mm:ss")}.zip");
        }
    }
}
