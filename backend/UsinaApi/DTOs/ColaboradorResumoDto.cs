namespace UsinaApi.DTOs
{
    public class ColaboradorResumoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public bool PinFoiDefinido { get; set; }
    }
}