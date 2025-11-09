using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UsinaApi.Models
{
    public class GravacaoRh
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        [JsonIgnore]
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [Required]
        public string CaminhoArquivo { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public bool Resolvido { get; set; } = false;
    }
}