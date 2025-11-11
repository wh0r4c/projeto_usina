using System.ComponentModel.DataAnnotations;

namespace UsinaApi.DTOs
{
    public class CriarColaboradorDto
    {
        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Length(11, 11)] // Garante 11 dígitos
        public string Cpf { get; set; } = string.Empty;

        [Required]
        public string Matricula { get; set; } = string.Empty;
    }
}