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
        [StringLength(6, MinimumLength = 6, ErrorMessage = "A matrícula deve ter exatamente 6 dígitos.")]
        public string Matricula { get; set; } = string.Empty;
    }
}