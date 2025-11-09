namespace UsinaApi.DTOs
{
    public class LoginRequestDto
    {
        public string Cpf { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }
}