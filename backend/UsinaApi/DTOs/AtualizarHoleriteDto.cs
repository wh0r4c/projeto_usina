using System.ComponentModel.DataAnnotations;

namespace UsinaApi.DTOs
{
    public class AtualizarHoleriteDto
    {
        [Required]
        public string MesAno { get; set; } = string.Empty; // Ex: "11-2025"

        [Required]
        public decimal ValorLiquido { get; set; }

        [Required]
        public string TextoParaFala { get; set; } = string.Empty;
    }
}