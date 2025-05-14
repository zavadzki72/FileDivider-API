using FileDivider.Api.Data;
using FileDivider.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace FileDivider.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(MongoContext context) : ControllerBase
    {
        private readonly MongoContext _context = context;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserAuthenticationRequest request)
        {
            var existUser = await _context.Users.Find(x => x.Email == request.Email && x.Password == request.Password).AnyAsync();

            if (!existUser)
                return Unauthorized();

            return NoContent();
        }
    }
}
