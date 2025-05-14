using FileDivider.Api.Dtos;
using FileDivider.Api.Models;
using FileDivider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileDivider.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly TemplateService _service;

        public TemplateController(TemplateService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<PdfTemplate>>> Get()
        {
            return await _service.GetAll();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PdfTemplate>> Get(Guid id)
        {
            var item = await _service.GetById(id);

            if (item == null) 
                return BadRequest("O template informado não foi encontrado na base de dados.");
            
            return item;
        }

        [HttpPost]
        public async Task<IActionResult> Post(PdfTemplateRequest request)
        {
            var item = PdfTemplate.CreateFromDto(request);

            var validatiopn = item.IsValid();
            if (!validatiopn.Item1)
                return BadRequest(validatiopn.Item2);

            await _service.CreateAsync(item);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, PdfTemplateRequest request)
        {
            var item = PdfTemplate.CreateFromDto(request);

            await _service.Update(id, item);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.Delete(id);
            return NoContent();
        }
    }
}
