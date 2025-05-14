namespace FileDivider.Api.Dtos
{
    public class UserAuthenticationRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
