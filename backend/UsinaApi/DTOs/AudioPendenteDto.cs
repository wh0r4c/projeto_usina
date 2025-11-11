namespace UsinaApi.DTOs
{
    public class AudioPendenteDto
    {
        public int Id { get; set; } // O ID da gravação (protocolo)
        public DateTime DataCriacao { get; set; }

        // Informações do Colaborador
        public int UsuarioId { get; set; }
        public string NomeColaborador { get; set; } = string.Empty;
        public string CpfColaborador { get; set; } = string.Empty;

        // O link para o áudio (relativo à wwwroot)
        public string CaminhoArquivo { get; set; } = string.Empty;
        public bool Resolvido { get; set; }
    }
}