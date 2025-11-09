using System.ComponentModel.DataAnnotations;

namespace UsinaApi.Models
{
    public class Aviso
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Conteudo { get; set; } = string.Empty;

        // Texto otimizado para a API de fala
        public string TextoParaFala { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Para quem é este aviso? (0 = Todos, 1 = Admin, etc. - podemos usar no futuro)
        public int NivelPrioridade { get; set; } = 0;
    }
}
