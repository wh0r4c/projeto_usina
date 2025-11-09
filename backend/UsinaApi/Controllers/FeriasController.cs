using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UsinaApi.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- Segurança!
public class FeriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public FeriasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/ferias
    [HttpGet]
    public async Task<IActionResult> GetFerias()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        var ferias = await _context.Ferias
            .Where(f => f.UsuarioId == userId)
            .FirstOrDefaultAsync();

        if (ferias == null)
        {
            return Ok(new
            {
                Status = "Nenhum registo encontrado.",
                DataProgramada = "-",
                DiasDeSaldo = "0",
                TextoParaFala = "Não encontrámos registos de férias para si. Por favor, contacte o R. H."
            });
        }

        string dataFormatada = "-";
        if (ferias.DataInicio.HasValue)
        {
            // Formata a data (ex: 20/12/2025)
            dataFormatada = ferias.DataInicio.Value.ToString("dd/MM/yyyy");
        }

        return Ok(new
        {
            Status = $"Programadas a partir de:",
            DataProgramada = dataFormatada,
            DiasDeSaldo = $"{ferias.DiasDeSaldo} dias",
            ferias.TextoParaFala
        });
    }
}