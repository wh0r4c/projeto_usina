using System.ComponentModel.DataAnnotations.Schema; // <-- ESTA LINHA FALTAVA
using System.Text.Json.Serialization;

namespace UsinaApi.Models
{
    public class Holerite
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        
        [JsonIgnore]
        public Usuario? Usuario { get; set; }
        
        public string MesAno { get; set; } = string.Empty; // ex: "11-2025"
        
        // --- NOVOS CAMPOS PARA O HOLERITE DINÂMICO ---
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SalarioBruto { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Descontos { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorLiquido { get; set; }

        // ---------------------------------------------

        public string TextoParaFala { get; set; } = string.Empty;
        
        // Mantemos o PDF como opcional (string?) caso queira usar no futuro
        // ou para manter compatibilidade com dados antigos.
        public string? CaminhoPdf { get; set; } 
    }
}