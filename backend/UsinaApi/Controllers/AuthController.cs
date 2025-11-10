using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UsinaApi.Data;
using UsinaApi.DTOs;
using UsinaApi.Models;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // --- PASSO 1: A NOVA LÓGICA DE LOGIN ---
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Cpf == loginRequest.Cpf);

        // 1. Se o CPF não for encontrado, falha.
        if (usuario == null)
        {
            return Unauthorized(new { message = "CPF ou PIN/Matrícula inválidos." });
        }

        string statusLogin;
        bool loginValido;

        // 2. Verifica se o utilizador JÁ DEFINIU um PIN
        if (usuario.PinFoiDefinido)
        {
            // --- FLUXO DE LOGIN NORMAL ---
            // Compara o PIN enviado com o HASH salvo no banco
            loginValido = BCrypt.Net.BCrypt.Verify(loginRequest.Pin, usuario.PinHash);
            statusLogin = "ok";
        }
        else
        {
            // --- FLUXO DE PRIMEIRO LOGIN ---
            // Compara o PIN enviado (que é a Matrícula) com a MATRÍCULA salva no banco
            loginValido = (loginRequest.Pin == usuario.Matricula);
            statusLogin = "primeiro_login";
        }

        // 3. Se a verificação (seja qual for) falhou, falha.
        if (!loginValido)
        {
            return Unauthorized(new { message = "CPF ou PIN/Matrícula inválidos." });
        }

        // 4. Se a verificação foi um sucesso, gera o "crachá" (Token)
        var token = GenerateJwtToken(usuario);

        // 5. Retorna o Token e o NOVO STATUS
        return Ok(new LoginResponseDto
        {
            Token = token,
            Nome = usuario.Nome,
            Status = statusLogin // Envia "ok" ou "primeiro_login"
        });
    }

    // --- PASSO 2: O NOVO ENDPOINT PARA DEFINIR O PIN ---
    [HttpPost("definir-pin")]
    [Authorize] // <-- Segurança! Só pode definir o PIN se estiver logado (com o token do "primeiro_login")
    public async Task<IActionResult> DefinirPin([FromBody] DefinirPinDto definirPinDto)
    {
        // Pega o ID do utilizador de dentro do Token que ele enviou
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized(); // Token inválido
        }

        var usuario = await _context.Usuarios.FindAsync(userId);
        
        if (usuario == null)
        {
            return NotFound("Utilizador não encontrado.");
        }

        // Se o utilizador já definiu um PIN, não o deixamos definir de novo (por esta rota)
        if (usuario.PinFoiDefinido)
        {
            return BadRequest(new { message = "O PIN já foi definido." });
        }

        // Atualiza o utilizador com o HASH do novo PIN
        usuario.PinHash = BCrypt.Net.BCrypt.HashPassword(definirPinDto.NovoPin);
        usuario.PinFoiDefinido = true; // O "interruptor" agora está ligado

        await _context.SaveChangesAsync();

        return Ok(new { message = "PIN definido com sucesso!" });
    }


    // --- PASSO 3: O GERADOR DE TOKEN 
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
                new Claim("cpf", usuario.Cpf)
            }),
            Expires = DateTime.UtcNow.AddHours(3),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}