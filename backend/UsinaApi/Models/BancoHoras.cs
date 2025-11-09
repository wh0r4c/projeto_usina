using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UsinaApi.Models
{
    public class BancoHoras
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        [JsonIgnore]
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        // Vamos armazenar como um número decimal.
        // Ex: 10.5 = 10 horas e 30 minutos.
        // -8.0 = 8 horas negativas.
        [Column(TypeName = "decimal(10, 2)")]
        public decimal HorasAcumuladas { get; set; } = 0;

        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
        
        // Texto otimizado para a API de fala
        public string TextoParaFala { get; set; } = string.Empty;
    }
}