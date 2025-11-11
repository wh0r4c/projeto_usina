using System.ComponentModel.DataAnnotations;

namespace UsinaApi.DTOs
{
    public class AtualizarBancoHorasDto
    {
        [Required]
        public decimal HorasAcumuladas { get; set; } // Ex: 10.5 (10h 30min) ou -5.0 (5h negativas)

        [Required]
        public string TextoParaFala { get; set; } = string.Empty; // Ex: "Seu saldo é de 10 horas e 30 minutos."
    }
}