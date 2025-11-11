using System.ComponentModel.DataAnnotations;

namespace UsinaApi.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string Cpf { get; set; } = string.Empty;

        public string? PinHash { get; set; }

        public string Nome { get; set; } = string.Empty;

        public ICollection<Holerite> Holerites { get; set; } = new List<Holerite>();

        [Required]
        public string Matricula { get; set; } = string.Empty;

        public bool PinFoiDefinido { get; set; } = false;

        public bool IsAdmin { get; set; } = false;

        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
    }
}