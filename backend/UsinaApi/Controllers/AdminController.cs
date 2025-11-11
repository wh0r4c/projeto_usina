using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UsinaApi.Data;
using UsinaApi.DTOs;
using UsinaApi.Models;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")] // Rota será /api/admin
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequestDto loginRequest)
    {
        // 1. Procura um utilizador que tenha o email E que seja Admin
        var adminUser = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == loginRequest.Email && u.IsAdmin);

        if (adminUser == null)
        {
            return Unauthorized(new { message = "Email ou senha inválidos." });
        }

        // 2. Verifica a senha complexa (PasswordHash)
        // (Nota: O PasswordHash do admin não pode ser nulo, garantido pelo SeedDatabase)
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, adminUser.PasswordHash);

        if (!isPasswordValid)
        {
            return Unauthorized(new { message = "Email ou senha inválidos." });
        }

        // 3. Se tudo estiver correto, gera um token
        var token = GenerateJwtToken(adminUser);

        return Ok(new AdminLoginResponseDto
        {
            Token = token,
            Nome = adminUser.Nome
        });
    }



    [HttpPost("colaboradores")]
    [Authorize(Roles = "Admin")] // <-- SEGURANÇA! Só Admins podem fazer isto.
    public async Task<IActionResult> CriarColaborador([FromBody] CriarColaboradorDto novoColaborador)
    {
        // 1. Verifica se o CPF já existe
        var cpfJaExiste = await _context.Usuarios.AnyAsync(u => u.Cpf == novoColaborador.Cpf);
        if (cpfJaExiste)
        {
            return BadRequest(new { message = "Este CPF já está cadastrado." });
        }

        // 2. Cria o novo utilizador
        var usuario = new Usuario
        {
            Nome = novoColaborador.Nome,
            Cpf = novoColaborador.Cpf,
            Matricula = novoColaborador.Matricula,

            // Define os padrões para o primeiro login
            PinHash = null,
            PinFoiDefinido = false,

            // Define os padrões para "não-admin"
            IsAdmin = false,
            Email = null,
            PasswordHash = null
        };

        // 3. Salva no banco de dados
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // 4. Retorna uma resposta de sucesso
        return CreatedAtAction(nameof(CriarColaborador), new { id = usuario.Id }, usuario);
    }


    // ... (depois da função CriarColaborador) ...

    [HttpGet("audios")]
    [Authorize(Roles = "Admin")] // <-- SEGURANÇA! Só Admins podem ver.
    public async Task<IActionResult> GetAudios()
    {
        // ETAPA 1: IR AO BANCO DE DADOS
        // Traz as gravações E os utilizadores para a memória do C#
        var audiosDoDb = await _context.GravacoesRh
            .Include(g => g.Usuario) // Junta os dados do Utilizador
            .OrderByDescending(g => g.DataCriacao)
            .ToListAsync(); // <-- EXECUTA A CONSULTA NO BANCO *AGORA*

        // ETAPA 2: FORMATAR OS DADOS (NA MEMÓRIA)
        // Agora "audiosDoDb" é uma List<GravacaoRh> (C# puro).
        // Como isto não é mais SQL, podemos usar o "?." à vontade.
        var audiosDto = audiosDoDb.Select(g => new AudioPendenteDto
        {
            Id = g.Id,
            DataCriacao = g.DataCriacao,
            UsuarioId = g.UsuarioId,

            // A CORREÇÃO (agora funciona):
            NomeColaborador = g.Usuario?.Nome ?? "Utilizador Desconhecido",
            CpfColaborador = g.Usuario?.Cpf ?? "N/A",

            CaminhoArquivo = g.CaminhoArquivo,
            Resolvido = g.Resolvido
        });

        return Ok(audiosDto);
    }


    // ... (depois da função GetAudios) ...

    [HttpGet("colaboradores")]
    [Authorize(Roles = "Admin")] // <-- SEGURANÇA!
    public async Task<IActionResult> GetColaboradores()
    {
        var colaboradores = await _context.Usuarios
            .Where(u => !u.IsAdmin) // <-- Filtra APENAS colaboradores
            .OrderBy(u => u.Nome)
            .Select(u => new ColaboradorResumoDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Cpf = u.Cpf,
                Matricula = u.Matricula,
                PinFoiDefinido = u.PinFoiDefinido
            })
            .ToListAsync();

        return Ok(colaboradores);
    }

    // ... (depois da função GetColaboradores) ...

    [HttpPost("colaboradores/{id}/bancohoras")]
    [Authorize(Roles = "Admin")] // <-- SEGURANÇA!
    public async Task<IActionResult> AtualizarBancoHoras(int id, [FromBody] AtualizarBancoHorasDto dados)
    {
        // 1. Verifica se o colaborador (pelo 'id' da URL) existe
        var colaborador = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && !u.IsAdmin);
        if (colaborador == null)
        {
            return NotFound(new { message = "Colaborador não encontrado." });
        }

        // 2. Procura o registo de banco de horas existente ou cria um novo
        var bancoHoras = await _context.BancoHoras.FirstOrDefaultAsync(b => b.UsuarioId == id);

        if (bancoHoras == null)
        {
            // Se não existir, cria um novo registo
            bancoHoras = new BancoHoras
            {
                UsuarioId = id
            };
            _context.BancoHoras.Add(bancoHoras);
        }

        // 3. Atualiza os dados
        bancoHoras.HorasAcumuladas = dados.HorasAcumuladas;
        bancoHoras.TextoParaFala = dados.TextoParaFala;
        bancoHoras.DataAtualizacao = DateTime.UtcNow;

        // 4. Salva no banco
        await _context.SaveChangesAsync();

        return Ok(bancoHoras); // Retorna o registo atualizado
    }

    // ... (depois da função AtualizarBancoHoras) ...

    [HttpPost("colaboradores/{id}/ferias")]
    [Authorize(Roles = "Admin")] // <-- SEGURANÇA!
    public async Task<IActionResult> AtualizarFerias(int id, [FromBody] AtualizarFeriasDto dados)
    {
        // 1. Verifica se o colaborador existe
        var colaborador = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && !u.IsAdmin);
        if (colaborador == null)
        {
            return NotFound(new { message = "Colaborador não encontrado." });
        }

        // 2. Procura o registo de férias existente ou cria um novo
        var ferias = await _context.Ferias.FirstOrDefaultAsync(f => f.UsuarioId == id);

        if (ferias == null)
        {
            // Se não existir, cria um novo registo
            ferias = new Ferias
            {
                UsuarioId = id
            };
            _context.Ferias.Add(ferias);
        }

        // 3. Atualiza os dados
        ferias.DataInicio = dados.DataInicio;
        ferias.DataFim = dados.DataFim;
        ferias.DiasDeSaldo = dados.DiasDeSaldo;
        ferias.TextoParaFala = dados.TextoParaFala;

        // 4. Salva no banco
        await _context.SaveChangesAsync();

        return Ok(ferias); // Retorna o registo atualizado
    }

    // ... (depois da função AtualizarFerias) ...

    [HttpPost("colaboradores/{id}/holerite")]
    [Authorize(Roles = "Admin")] // <-- SEGURANÇA!
    public async Task<IActionResult> AtualizarHolerite(int id, [FromForm] AtualizarHoleriteDto dados, IFormFile pdfFile)
    {
        // 1. Verifica se o colaborador existe
        var colaborador = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && !u.IsAdmin);
        if (colaborador == null)
        {
            return NotFound(new { message = "Colaborador não encontrado." });
        }

        // 2. Valida o ficheiro PDF
        if (pdfFile == null || pdfFile.Length == 0 || pdfFile.ContentType != "application/pdf")
        {
            return BadRequest(new { message = "Ficheiro PDF inválido ou em falta." });
        }

        // 3. Salva o ficheiro PDF no servidor (na pasta wwwroot/pdfs)
        // (Garante que a pasta 'pdfs' existe em 'wwwroot')
        var nomeFicheiroUnico = $"{Guid.NewGuid()}_{pdfFile.FileName}";
        var caminhoCompleto = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", nomeFicheiroUnico);

        try
        {
            await using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro ao salvar o PDF: {ex.Message}" });
        }

        // 4. Procura o registo de holerite existente (para este mês/ano) ou cria um novo
        var holerite = await _context.Holerites
            .FirstOrDefaultAsync(h => h.UsuarioId == id && h.MesAno == dados.MesAno);

        if (holerite == null)
        {
            holerite = new Holerite
            {
                UsuarioId = id,
                MesAno = dados.MesAno
            };
            _context.Holerites.Add(holerite);
        }

        // 5. Atualiza os dados
        holerite.ValorLiquido = dados.ValorLiquido;
        holerite.TextoParaFala = dados.TextoParaFala;
        holerite.CaminhoPdf = Path.Combine("pdfs", nomeFicheiroUnico); // Salva o caminho *relativo*

        // 6. Salva no banco
        await _context.SaveChangesAsync();

        return Ok(holerite);
    }

    // --- Gerador de Token ---
    // NOTA: No futuro, poderíamos mover esta função para uma classe "Serviço" 
    // separada, para que o AuthController e o AdminController a pudessem usar 
    // sem a duplicarmos. Por agora, copiá-la é o mais simples.
    private string GenerateJwtToken(Usuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                
                // ADICIONA UMA "CLAIM" DE ADMIN
                // É assim que o backend vai saber que este token é de um admin
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = DateTime.UtcNow.AddHours(8), // Admin pode ficar logado mais tempo
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}