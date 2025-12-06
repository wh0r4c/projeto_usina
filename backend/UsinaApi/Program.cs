using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Importante para o Swagger funcionar
using System.Text;
using UsinaApi.Data;
using UsinaApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configurar Serviços ---

// Configuração para evitar erro de timestamp no PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configura o banco de dados de forma inteligente
if (builder.Environment.IsProduction())
{
    // Usa Npgsql (PostgreSQL) quando estiver no Render
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Usa SQLite quando estiver no seu PC (dotnet run)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

// Adiciona Autenticação JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new ArgumentNullException("Jwt:Key", "Chave JWT não configurada no appsettings.json");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // Em dev/testes, desativamos para facilitar
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Adiciona Autorização
builder.Services.AddAuthorization();

// Adiciona Controladores
builder.Services.AddControllers();

// Adiciona CORS (MUITO IMPORTANTE!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:5500",
                "http://localhost:5501", // Porta alternativa do Live Server
                "http://127.0.0.1:5500",
                "http://127.0.0.1:5501",
                "https://projeto-usina.netlify.app",
                "https://admin-projeto-usina.netlify.app",
                "https://projeto-usina.vercel.app",
                "https://admin-projeto-usina.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

// Adiciona serviços de API (Swagger - documentação)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UsinaApi", Version = "v1" });

    // Configura o botão "Authorize" (Cadeado)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o seu token JWT aqui."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- 2. Construir a Aplicação ---
var app = builder.Build();

// --- 3. Configurar o Pipeline HTTP ---

// Cria/Alimenta o banco de dados ao iniciar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    if (app.Environment.IsProduction())
    {
        // Em produção (Render), executa as migrações (PostgreSQL)
        context.Database.Migrate();
    }
    else
    {
        // Em desenvolvimento (local), apenas GARANTE que o banco (SQLite) existe
        context.Database.EnsureCreated();
    }

    SeedDatabase(context); // Alimenta com dados de teste
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Desativado para facilitar testes locais http

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// --- 4. Método para alimentar o banco (Seed) ---
static void SeedDatabase(AppDbContext context)
{
    // Verifica se já existe um utilizador Colaborador
    if (!context.Usuarios.Any(u => !u.IsAdmin))
    {
        var usuarioTeste = new Usuario
        {
            Cpf = "12345678900",
            Nome = "Leandro",
            Matricula = "123456",
            PinHash = null,
            PinFoiDefinido = false,
            Departamento = "Operação" // Adicionado e vírgula corrigida
        };
        context.Usuarios.Add(usuarioTeste);
        context.SaveChanges();

        // Adiciona Holerite de Teste
        context.Holerites.Add(new Holerite
        {
            UsuarioId = usuarioTeste.Id,
            MesAno = "10-2025",
            ValorLiquido = 1500.75m,
            SalarioBruto = 2000.00m,
            Descontos = 499.25m,
            TextoParaFala = "O seu pagamento de Outubro foi de mil, quinhentos reais e setenta e cinco centavos.",
            CaminhoPdf = "pdfs/holerite_exemplo.pdf"
        });

        // Adiciona Banco de Horas
        context.BancoHoras.Add(new BancoHoras
        {
            UsuarioId = usuarioTeste.Id,
            HorasAcumuladas = 12.5m,
            DataAtualizacao = DateTime.UtcNow,
            TextoParaFala = "Você possui um saldo positivo de 12 horas e 30 minutos."
        });

        // Adiciona Férias
        context.Ferias.Add(new Ferias
        {
            UsuarioId = usuarioTeste.Id,
            DataInicio = new DateTime(2025, 12, 20).ToUniversalTime(), // UTC para evitar erro de PostgreSQL
            DataFim = new DateTime(2026, 1, 5).ToUniversalTime(),
            DiasDeSaldo = 10,
            TextoParaFala = "As suas próximas férias estão programadas para começar no dia 20 de Dezembro de 2025."
        });

        context.SaveChanges();
    }

    // Verifica e adiciona Avisos
    if (!context.Avisos.Any())
    {
        context.Avisos.AddRange(
            new Aviso
            {
                Titulo = "Feriado Municipal - Dia 15",
                Conteudo = "Não haverá expediente devido ao feriado.",
                TextoParaFala = "Atenção: Não haverá expediente dia 15 devido ao feriado."
            },
            new Aviso
            {
                Titulo = "Pagamento do 13º Salário",
                Conteudo = "A primeira parcela será depositada no dia 20.",
                TextoParaFala = "A primeira parcela do décimo terceiro será depositada dia 20."
            }
        );
        context.SaveChanges();
    }

    // Verifica e adiciona FAQs
    if (!context.Faqs.Any())
    {
        context.Faqs.AddRange(
            new Faq { Pergunta = "Como peço férias?", Resposta = "Fale com seu supervisor.", TextoParaFala = "Fale com seu supervisor.", Ordem = 1 },
            new Faq { Pergunta = "Qual o telefone do sindicato?", Resposta = "(19) 3456-7890", TextoParaFala = "O telefone é 19, 3456, 7890", Ordem = 2 }
        );
        context.SaveChanges();
    }

    // VERIFICA SE JÁ EXISTE UM ADMIN
    if (!context.Usuarios.Any(u => u.IsAdmin))
    {
        context.Usuarios.Add(new Usuario
        {
            Nome = "Admin RH",
            Email = "rh@usina.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsAdmin = true,
            Cpf = "00000000000",
            Matricula = "000001",
            PinFoiDefinido = true,
            Departamento = "RH"
        });
        context.SaveChanges();
    }
}