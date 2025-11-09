using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsinaApi.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- Segurança! Só usuários logados podem ver.
public class AvisosController : ControllerBase
{
    private readonly AppDbContext _context;

    public AvisosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/avisos
    [HttpGet]
    public async Task<IActionResult> GetAvisos()
    {
        // Pega os 10 avisos mais recentes
        var avisos = await _context.Avisos
            .OrderByDescending(a => a.DataCriacao)
            .Take(10)
            .Select(a => new // Seleciona só os dados que o app precisa
            {
                a.Id,
                a.Titulo,
                a.Conteudo,
                a.TextoParaFala,
                Data = a.DataCriacao.ToString("dd/MM/yyyy") // Formata a data
            })
            .ToListAsync();
            
        return Ok(avisos);
    }
}