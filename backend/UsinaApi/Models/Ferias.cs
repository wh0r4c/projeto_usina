using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UsinaApi.Models
{
    public class Ferias
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        [JsonIgnore]
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        // Datas das férias programadas
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        
        // Ou quantos dias ele tem de saldo
        public int DiasDeSaldo { get; set; } = 0;
        
        public string TextoParaFala { get; set; } = string.Empty;
    }
}