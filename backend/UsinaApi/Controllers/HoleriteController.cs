using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UsinaApi.Data;
using Microsoft.AspNetCore.Hosting; // Importante para servir arquivos
using System.IO; // Importante para caminhos

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- ISSO EXIGE QUE O USUÁRIO ESTEJA LOGADO (envie o token)
public class HoleriteController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public HoleriteController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: api/holerite
    [HttpGet]
    public async Task<IActionResult> GetHoleriteAtual()
    {
        // Pega o ID do usuário de dentro do Token JWT
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        // Pega o holerite mais recente (ex: 10-2025)
        var holerite = await _context.Holerites
            .Where(h => h.UsuarioId == userId)
            .OrderByDescending(h => h.MesAno)
            .FirstOrDefaultAsync();

        if (holerite == null)
        {
            return NotFound(new { message = "Nenhum holerite encontrado." });
        }

        return Ok(new
        {
            valor_liquido = holerite.ValorLiquido,
            texto_para_fala = holerite.TextoParaFala,
            tem_pdf = !string.IsNullOrEmpty(holerite.CaminhoPdf)
        });
    }

    // GET: api/holerite/pdf
    [HttpGet("pdf")]
    public async Task<IActionResult> GetHoleritePdf()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        var holerite = await _context.Holerites
            .Where(h => h.UsuarioId == userId)
            .OrderByDescending(h => h.MesAno)
            .FirstOrDefaultAsync();

        if (holerite == null || string.IsNullOrEmpty(holerite.CaminhoPdf))
        {
            return NotFound(new { message = "PDF do holerite não encontrado." });
        }

        // Pega o caminho do arquivo (ex: wwwroot/pdfs/holerite_exemplo.pdf)
        var filePath = Path.Combine(_env.WebRootPath, holerite.CaminhoPdf);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Arquivo físico não encontrado no servidor." });
        }

        // Envia o arquivo físico para o usuário
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "application/pdf", $"holerite_{holerite.MesAno}.pdf");
    }
}