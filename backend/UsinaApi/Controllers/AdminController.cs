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
using System.IO;
using System.Globalization;

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

// --- IMPORTAÇÃO GERAL (CORRIGIDA) ---
    [HttpPost("importar/geral")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportarGeral(IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(new { message = "Arquivo inválido." });

        int usrsCount = 0; // Contador de novos usuários
        int bhCount = 0;
        int holCount = 0;
        int ferCount = 0;
        int erros = 0;

        using (var reader = new StreamReader(arquivo.OpenReadStream()))
        {
            while (reader.Peek() >= 0)
            {
                var linha = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(linha)) continue;

                var dados = linha.Split(';');
                var tipoRegistro = dados[0].ToUpper().Trim();
                
                try 
                {
                    // ==================================================
                    // 1. LÓGICA DE CRIAR USUÁRIO (USR)
                    // ==================================================
                    // Formato: USR;NOME;CPF;MATRICULA;DEPARTAMENTO
                    if (tipoRegistro == "USR" && dados.Length >= 4)
                    {
                        var nome = dados[1].Trim();
                        var cpf = dados[2].Trim();
                        var novaMatricula = dados[3].Trim();
                        var depto = dados.Length > 4 ? dados[4].Trim() : "Geral";

                        // Verifica se já existe para não duplicar
                        var existe = await _context.Usuarios.AnyAsync(u => u.Cpf == cpf || u.Matricula == novaMatricula);
                        
                        if (!existe)
                        {
                            var novoUser = new Usuario
                            {
                                Nome = nome,
                                Cpf = cpf,
                                Matricula = novaMatricula,
                                Departamento = depto,
                                PinHash = null,
                                PinFoiDefinido = false,
                                IsAdmin = false
                            };
                            _context.Usuarios.Add(novoUser);
                            
                            // IMPORTANTE: Salvar IMEDIATAMENTE para que as próximas linhas
                            // (BH, HOL) consigam encontrar este usuário novo.
                            await _context.SaveChangesAsync(); 
                            usrsCount++;
                        }
                        continue; // Vai para a próxima linha
                    }

                    // ==================================================
                    // PARA AS OUTRAS LÓGICAS, PRECISAMOS BUSCAR O USUÁRIO
                    // ==================================================
                    if (dados.Length < 2) continue;
                    var matriculaBusca = dados[1].Trim();
                    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Matricula == matriculaBusca);

                    if (usuario == null) {
                        // Se não achou o usuário (e não era uma linha USR), é erro.
                        Console.WriteLine($"[ERRO] Usuário não encontrado: {matriculaBusca}");
                        erros++;
                        continue;
                    }

                    // 2. LÓGICA DE BANCO DE HORAS (BH)
                    if (tipoRegistro == "BH" && dados.Length >= 3)
                    {
                        decimal horas = ConverterParaDecimal(dados[2]);
                        
                        var banco = await _context.BancoHoras.FirstOrDefaultAsync(b => b.UsuarioId == usuario.Id);
                        if (banco == null) { banco = new BancoHoras { UsuarioId = usuario.Id }; _context.BancoHoras.Add(banco); }
                        
                        banco.HorasAcumuladas = horas;
                        banco.DataAtualizacao = DateTime.UtcNow;
                        var sinal = horas >= 0 ? "positivo" : "negativo";
                        banco.TextoParaFala = $"Olá {usuario.Nome}. Seu saldo atual é {sinal} de {Math.Abs(horas)} horas.";
                        bhCount++;
                    }
                    
                    // 3. LÓGICA DE HOLERITE (HOL)
                    else if (tipoRegistro == "HOL" && dados.Length >= 5)
                    {
                        decimal bruto = ConverterParaDecimal(dados[2]);
                        decimal desc = ConverterParaDecimal(dados[3]);
                        var mes = dados[4].Trim();

                        var hol = await _context.Holerites.FirstOrDefaultAsync(h => h.UsuarioId == usuario.Id && h.MesAno == mes);
                        if (hol == null) { hol = new Holerite { UsuarioId = usuario.Id, MesAno = mes }; _context.Holerites.Add(hol); }

                        hol.SalarioBruto = bruto;
                        hol.Descontos = desc;
                        hol.ValorLiquido = bruto - desc;
                        
                        var liqFormatado = hol.ValorLiquido.ToString("C", new CultureInfo("pt-BR"));
                        hol.TextoParaFala = $"Olá {usuario.Nome}. Seu salário líquido de {mes} é de {liqFormatado}.";
                        holCount++;
                    }

                    // 4. LÓGICA DE FÉRIAS (FER)
                    else if (tipoRegistro == "FER" && dados.Length >= 5)
                    {
                        DateTime.TryParse(dados[2], out DateTime inicio);
                        DateTime.TryParse(dados[3], out DateTime fim);
                        int.TryParse(dados[4], out int saldo);

                        var ferias = await _context.Ferias.FirstOrDefaultAsync(f => f.UsuarioId == usuario.Id);
                        if (ferias == null) { ferias = new Ferias { UsuarioId = usuario.Id }; _context.Ferias.Add(ferias); }

                        ferias.DataInicio = inicio;
                        ferias.DataFim = fim;
                        ferias.DiasDeSaldo = saldo;
                        ferias.TextoParaFala = $"Olá {usuario.Nome}. Suas férias começam dia {inicio:dd/MM}.";
                        ferCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO GRAVE] {ex.Message}");
                    erros++;
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { 
            message = "Processamento Geral Concluído!", 
            detalhes = $"Novos Usuários: {usrsCount} | Banco Horas: {bhCount} | Holerites: {holCount} | Férias: {ferCount} | Erros: {erros}" 
        });
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

    [HttpGet("colaboradores")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetColaboradores(string? termo, string? departamento)
    {
        // Começa a consulta (ainda não vai ao banco)
        var query = _context.Usuarios.Where(u => !u.IsAdmin);

        // Aplica filtro de Nome ou Matrícula ou CPF
        if (!string.IsNullOrEmpty(termo))
        {
            termo = termo.ToLower();
            query = query.Where(u =>
                u.Nome.ToLower().Contains(termo) ||
                u.Matricula.Contains(termo) ||
                u.Cpf.Contains(termo));
        }

        // Aplica filtro de Departamento
        if (!string.IsNullOrEmpty(departamento))
        {
            query = query.Where(u => u.Departamento == departamento);
        }

        var colaboradores = await query
            .OrderBy(u => u.Nome)
            .Select(u => new
            {
                u.Id,
                u.Nome,
                u.Cpf,
                u.Matricula,
                u.PinFoiDefinido,
                u.Departamento // Adicionei Departamento aqui
            })
            .ToListAsync();

        return Ok(colaboradores);
    }
    // --- CRIAR NOVO COLABORADOR (Manual) ---
    [HttpPost("colaboradores")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CriarColaborador([FromBody] CriarColaboradorDto novoColaborador)
    {
        // 1. Verifica se o CPF já existe
        var cpfJaExiste = await _context.Usuarios.AnyAsync(u => u.Cpf == novoColaborador.Cpf);
        if (cpfJaExiste)
        {
            return BadRequest(new { message = "Este CPF já está cadastrado." });
        }

        // 2. Cria o objeto do utilizador
        var usuario = new Usuario
        {
            Nome = novoColaborador.Nome,
            Cpf = novoColaborador.Cpf,
            Matricula = novoColaborador.Matricula,

            // ATENÇÃO: Aqui está a linha que faz o filtro funcionar!
            Departamento = novoColaborador.Departamento,

            // Configurações padrão para o "Primeiro Acesso"
            PinHash = null,
            PinFoiDefinido = false,

            // Garante que não é admin
            IsAdmin = false,
            Email = null,
            PasswordHash = null
        };

        // 3. Salva no banco de dados
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // 4. Retorna sucesso
        return CreatedAtAction(nameof(GetColaboradores), new { id = usuario.Id }, usuario);
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
    [HttpGet("colaboradores/{id}/holerites")] // Define a URL
    [Authorize(Roles = "Admin")] // Protege o acesso
    public async Task<IActionResult> GetHistoricoHolerites(int id)
    {
        var holerites = await _context.Holerites
            .Where(h => h.UsuarioId == id)
            .OrderByDescending(h => h.Id) // Ou ordenar por data se possível
            .Select(h => new
            {
                h.Id,
                h.MesAno,
                h.ValorLiquido,
                h.TextoParaFala
            })
            .ToListAsync();

        return Ok(holerites);
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

    // Função auxiliar para converter qualquer texto de dinheiro em Decimal
    private decimal ConverterParaDecimal(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return 0;

        try
        {
            // 1. Limpeza básica
            var limpo = valor.Trim().Replace("R$", "").Replace(" ", "");

            // 2. Lógica de Detecção: É Brasil (vírgula) ou EUA (ponto)?
            if (limpo.Contains(","))
            {
                // Se tem vírgula, assumimos formato BR (ex: 3.500,50 ou 3500,50)
                // Removemos os pontos de milhar (3.500 -> 3500)
                limpo = limpo.Replace(".", "");
                // Trocamos a vírgula decimal por ponto para o sistema entender (3500,50 -> 3500.50)
                limpo = limpo.Replace(",", ".");
            }

            // 3. Converte usando a cultura Invariante (padrão de computador: ponto é decimal)
            return decimal.Parse(limpo, CultureInfo.InvariantCulture);
        }
        catch
        {
            // Se falhar (ex: texto inválido), retorna 0
            // DICA: Coloque um breakpoint aqui se quiser ver o erro acontecendo
            return 0;
        }
    }
}