using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsinaApi.Data;

[ApiController]
[Route("api/[controller]")]
// SEM [Authorize] desta vez
public class FaqController : ControllerBase
{
    private readonly AppDbContext _context;

    public FaqController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/faq
    [HttpGet]
    public async Task<IActionResult> GetFaqs()
    {
        // Pega todos os FAQs, ordenados pelo campo 'Ordem'
        var faqs = await _context.Faqs
            .OrderBy(f => f.Ordem)
            .Select(f => new // Seleciona só os dados que o app precisa
            {
                f.Id,
                f.Pergunta,
                f.Resposta,
                f.TextoParaFala
            })
            .ToListAsync();
            
        return Ok(faqs);
    }
}