using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UsinaApi.Data;
using UsinaApi.Models;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- Segurança! Só usuários logados podem enviar.
public class SuporteController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public SuporteController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost("audio")]
    public async Task<IActionResult> UploadAudio(IFormFile audioFile)
    {
        // 1. Validar o usuário (pelo Token)
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        // 2. Validar o arquivo
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { message = "Nenhum arquivo de áudio enviado." });
        }

        // 3. Criar um nome de arquivo único e seguro
        var extensao = Path.GetExtension(audioFile.FileName); // Pega a extensão (ex: .wav)
        var nomeArquivo = $"{Guid.NewGuid()}{extensao}"; // Cria um ID único
        
        // Define o caminho para salvar (ex: .../wwwroot/gravacoes/arquivo-unico.wav)
        var caminhoCompleto = Path.Combine(_env.WebRootPath, "gravacoes", nomeArquivo);

        // 4. Salvar o arquivo no disco
        try
        {
            await using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            // Se der erro ao salvar no disco
            return StatusCode(500, new { message = $"Erro interno ao salvar arquivo: {ex.Message}" });
        }
        
        // 5. Registrar no Banco de Dados
        var gravacao = new GravacaoRh
        {
            UsuarioId = userId,
            CaminhoArquivo = Path.Combine("gravacoes", nomeArquivo), // Salva o caminho relativo
            DataCriacao = DateTime.UtcNow
        };

        _context.GravacoesRh.Add(gravacao);
        await _context.SaveChangesAsync();

        // 6. Enviar a resposta
        return Ok(new 
        { 
            message = "Mensagem de voz enviada com sucesso!",
            ticketId = gravacao.Id // Devolve um "protocolo"
        });
    }
}