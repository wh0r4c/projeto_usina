using System.ComponentModel.DataAnnotations;

namespace UsinaApi.DTOs
{
    public class DefinirPinDto
    {
        [Required]
        [Length(4, 4)] // Garante que tem 4 caracteres
        public string NovoPin { get; set; } = string.Empty;
    }
}