namespace UsinaApi.DTOs
{
    public class AtualizarFeriasDto
    {
        // Data de início das férias. O '?' torna opcional (pode ser nulo)
        public DateTime? DataInicio { get; set; }

        // Data de fim das férias.
        public DateTime? DataFim { get; set; }

        public int DiasDeSaldo { get; set; } = 0;

        public string TextoParaFala { get; set; } = string.Empty;
    }
}