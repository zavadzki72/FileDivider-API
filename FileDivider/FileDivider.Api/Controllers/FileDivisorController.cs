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

        [HttpPost]
        public async Task<IActionResult> Post(FileDivisorRequest request)
        {
            var response = await _service.DivideFile(request);
            return Ok(new { 
                Data = response
            });
        }
    }
}
