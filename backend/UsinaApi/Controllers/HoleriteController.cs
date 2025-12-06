using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UsinaApi.Data;
using Microsoft.AspNetCore.Hosting;
using System.IO;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HoleriteController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public HoleriteController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // 1. Endpoint para pegar a lista de meses disponíveis
    [HttpGet("meses")]
    public async Task<IActionResult> GetMesesDisponiveis()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var meses = await _context.Holerites
            .Where(h => h.UsuarioId == userId)
            // Ordenar por ID é mais seguro que por String de data
            .OrderByDescending(h => h.Id) 
            .Select(h => h.MesAno)
            .ToListAsync();

        return Ok(meses);
    }

    // 2. Pegar os DADOS do Holerite (Dinâmico)
// 2. Pegar os DADOS do Holerite (Dinâmico + Histórico)
    [HttpGet]
    public async Task<IActionResult> GetHolerite([FromQuery] string? mes)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var query = _context.Holerites.Where(h => h.UsuarioId == userId);

        // Lógica do Histórico
        if (!string.IsNullOrEmpty(mes))
        {
            query = query.Where(h => h.MesAno == mes);
        }
        else
        {
            query = query.OrderByDescending(h => h.Id); 
        }

        var holerite = await query.FirstOrDefaultAsync();

        if (holerite == null) return NotFound(new { message = "Holerite não encontrado." });

        // AQUI ESTAVA O ERRO: Faltavam os campos numéricos no retorno!
        return Ok(new
        {
            mesAno = holerite.MesAno,
            
            // --- ESTES SÃO OS CAMPOS QUE FALTAVAM ---
            salarioBruto = holerite.SalarioBruto,
            descontos = holerite.Descontos,
            valorLiquido = holerite.ValorLiquido,
            // ----------------------------------------

            textoParaFala = holerite.TextoParaFala,
            temPdf = !string.IsNullOrEmpty(holerite.CaminhoPdf) 
        });
    }

    // 3. Pegar o ARQUIVO PDF (CORRIGIDO)
    [HttpGet("pdf")]
    public async Task<IActionResult> GetHoleritePdf([FromQuery] string? mes) // <-- ADICIONADO O PARÂMETRO
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var query = _context.Holerites.Where(h => h.UsuarioId == userId);

        // --- LÓGICA DE FILTRO ADICIONADA ---
        if (!string.IsNullOrEmpty(mes))
        {
            query = query.Where(h => h.MesAno == mes);
        }
        else
        {
            query = query.OrderByDescending(h => h.Id);
        }
        // -----------------------------------

        var holerite = await query.FirstOrDefaultAsync();

        if (holerite == null || string.IsNullOrEmpty(holerite.CaminhoPdf))
        {
            return NotFound(new { message = "PDF do holerite não encontrado." });
        }

        var filePath = Path.Combine(_env.WebRootPath, holerite.CaminhoPdf);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Arquivo físico não encontrado no servidor." });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "application/pdf", $"holerite_{holerite.MesAno}.pdf");
    }
}