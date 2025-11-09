using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Cpf == loginRequest.Cpf);

        // ATENÇÃO: NUNCA faça isso em produção. Aqui o PIN está em texto puro.
        // O correto é usar um HASH (ex: BCrypt.Net)
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Pin, usuario.PinHash))
        {
            return Unauthorized(new { message = "CPF ou PIN inválidos." });
        }

        // Se o usuário é válido, gera o Token
        var token = GenerateJwtToken(usuario);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Nome = usuario.Nome
        });
    }

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