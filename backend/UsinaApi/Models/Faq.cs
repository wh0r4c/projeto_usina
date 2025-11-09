using System.ComponentModel.DataAnnotations;

namespace UsinaApi.Models
{
    public class Faq
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Pergunta { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Resposta { get; set; } = string.Empty;
        
        // Texto otimizado para a API de fala
        public string TextoParaFala { get; set; } = string.Empty;

        // Para ordenar (ex: 1 = mais importante)
        public int Ordem { get; set; } = 100;
    }
}