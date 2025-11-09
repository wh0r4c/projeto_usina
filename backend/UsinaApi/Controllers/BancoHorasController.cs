using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UsinaApi.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- Segurança! Só o utilizador logado pode ver.
public class BancoHorasController : ControllerBase
{
    private readonly AppDbContext _context;

    public BancoHorasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/bancohoras
    [HttpGet]
    public async Task<IActionResult> GetBancoHoras()
    {
        // Pega o ID do utilizador de dentro do Token JWT
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        // Procura o saldo de horas mais recente daquele utilizador
        var saldo = await _context.BancoHoras
            .Where(b => b.UsuarioId == userId)
            .OrderByDescending(b => b.DataAtualizacao)
            .FirstOrDefaultAsync();

        if (saldo == null)
        {
            // Se não houver registo, retorna 0
            return Ok(new
            {
                HorasFormatadas = "0h 00min",
                TextoParaFala = "Você não possui registo de banco de horas.",
                DataAtualizacao = DateTime.UtcNow.ToString("dd/MM/yyyy")
            });
        }

        // Lógica para formatar 12.5m em "12h 30min"
        var horas = (int)Math.Truncate(saldo.HorasAcumuladas);
        var minutos = (int)((Math.Abs(saldo.HorasAcumuladas) - Math.Abs(horas)) * 60);
        var sinal = saldo.HorasAcumuladas < 0 ? "-" : "+";

        return Ok(new
        {
            // Envia o saldo formatado
            HorasFormatadas = $"{sinal} {Math.Abs(horas)}h {minutos:00}min",
            saldo.TextoParaFala,
            DataAtualizacao = saldo.DataAtualizacao.ToString("dd/MM/yyyy")
        });
    }
}