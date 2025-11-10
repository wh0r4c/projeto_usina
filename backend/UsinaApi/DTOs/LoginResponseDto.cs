namespace UsinaApi.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}