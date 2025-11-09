using System.Text.Json.Serialization;

namespace UsinaApi.Models
{
    public class Holerite
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        
        [JsonIgnore] // Impede que o "Usuario" seja enviado junto no JSON (evita loop)
        public Usuario? Usuario { get; set; }
        
        public string MesAno { get; set; } = string.Empty;
        public decimal ValorLiquido { get; set; }
        public string TextoParaFala { get; set; } = string.Empty;
        public string CaminhoPdf { get; set; } = string.Empty; // ex: "pdfs/holerite_exemplo.pdf"
    }
}