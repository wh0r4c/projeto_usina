using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UsinaApi.Data;
using UsinaApi.Models; // Importe os models
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configurar Serviços ---
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// --- 1. Configurar Serviços ---

// Configura o banco de dados de forma inteligente
// (Usa SQLite localmente e PostgreSQL em Produção)
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
    //builder.Services.AddDbContext<AppDbContext>(options =>
        //options.UseNpgsql());
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
            ValidateIssuer = true,
            ValidateAudience = true,
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
                "http://127.0.0.1:5500",
                "https://projeto-usina.netlify.app",
                "https://admin-projeto-usina.netlify.app/"
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
    // 1. Define as configurações do seu projeto
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UsinaApi",
        Version = "v1"
    });

    // 2. Define o "Cadeado" (Segurança de Token Bearer)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Por favor, insira 'Bearer ' (com espaço) e depois o seu token JWT."
    });

    // 3. Diz ao Swagger para aplicar este "Cadeado" a todos os endpoints
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Criar/Alimentar o banco de dados ao iniciar
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
        // Em desenvolvimento (local), apenas GARANTE que o 
        // banco de dados (SQLite) seja criado.
        context.Database.EnsureCreated(); // <-- ESTA É A CORREÇÃO MÁGICA
    }

    SeedDatabase(context); // Alimenta com dados de teste
}

// --- 3. Configurar o Pipeline HTTP ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// (O seu bloco using/SeedDatabase vem aqui...)
using (var scope = app.Services.CreateScope())
{
    // ... (o seu código SeedDatabase) ...
}

// A CORREÇÃO DE ORDEM COMEÇA AQUI

// app.UseHttpsRedirection(); // Mantenha comentado por agora

// 1. Habilita o "roteamento" (para que ele saiba o que é um "controller")
app.UseRouting();

// 2. Habilita o CORS (agora ele pode adicionar cabeçalhos às rotas)
app.UseCors("AllowFrontend");

app.UseStaticFiles(); // (Este pode ficar aqui)

// 3. Habilita a Autenticação
app.UseAuthentication();

// 4. Habilita a Autorização
app.UseAuthorization();

// 5. Mapeia os controllers (agora eles exigem autorização e usam CORS)
app.MapControllers();

app.Run();

// ... (A sua função SeedDatabase fica aqui no fim) ...


// --- 4. Método para alimentar o banco (Seed) ---

static void SeedDatabase(AppDbContext context)
{
    // Verifica se já existe um utilizador
    if (!context.Usuarios.Any())
    {
        var usuarioTeste = new Usuario
        {
            Cpf = "12345678900",
            Nome = "Leandro",
            Matricula = "MATRICULA123", // Esta é a "senha" temporária
            PinHash = null, // Começa sem PIN
            PinFoiDefinido = false // O "interruptor" está desligado
        };
        context.Usuarios.Add(usuarioTeste);
        context.SaveChanges(); // Salva para obter o ID

        context.Holerites.Add(new Holerite
        {
            UsuarioId = usuarioTeste.Id,
            MesAno = "10-2025",
            ValorLiquido = 1500.75m,
            TextoParaFala = "O seu pagamento de Outubro foi de mil, quinhentos reais e setenta e cinco centavos.",
            CaminhoPdf = "pdfs/holerite_exemplo.pdf"
        });
        context.SaveChanges();
    }

    // Verifica se já existe um aviso
    if (!context.Avisos.Any())
    {
        context.Avisos.Add(new Aviso
        {
            Titulo = "Feriado Municipal - Dia 15",
            Conteudo = "Atenção a todos os colaboradores. Na próxima sexta-feira, dia 15, não haverá expediente devido ao feriado municipal. As atividades retornam normalmente na segunda-feira.",
            TextoParaFala = "Atenção a todos os colaboradores. Na próxima sexta-feira, dia 15, não haverá expediente devido ao feriado municipal. As atividades retornam normalmente na segunda-feira."
        });

        context.Avisos.Add(new Aviso
        {
            Titulo = "Pagamento do 13º Salário",
            Conteudo = "Informamos que a primeira parcela do 13º salário será depositada no dia 20 deste mês.",
            TextoParaFala = "Informamos que a primeira parcela do décimo terceiro salário será depositada no dia 20 deste mês."
        });

        context.SaveChanges();
    }

    // Verifica se já existem FAQs
    if (!context.Faqs.Any())
    {
        context.Faqs.Add(new Faq
        {
            Pergunta = "Como peço férias?",
            Resposta = "Para solicitar férias, deve aceder ao portal interno de RH ou contactar o seu supervisor direto com pelo menos 30 dias de antecedência.",
            TextoParaFala = "Para solicitar férias, deve aceder ao portal interno de R. H. ou contactar o seu supervisor direto com pelo menos 30 dias de antecedência.",
            Ordem = 1
        });

        context.Faqs.Add(new Faq
        {
            Pergunta = "Qual o telefone do sindicato?",
            Resposta = "O número de telefone principal do sindicato é (19) 3456-7890. O horário de atendimento é de segunda a sexta, das 8h às 17h.",
            TextoParaFala = "O número de telefone principal do sindicato é 19, 3456, 7890. O horário de atendimento é de segunda a sexta, das 8 às 17 horas.",
            Ordem = 2
        });

        // --- NOVAS PERGUNTAS ADICIONADAS AQUI ---

        context.Faqs.Add(new Faq
        {
            Pergunta = "Como justifico uma falta?",
            Resposta = "Atestados médicos devem ser entregues ao RH em até 48 horas. Para outras ausências, contacte o seu supervisor imediato.",
            TextoParaFala = "Atestados médicos devem ser entregues ao R. H. em até 48 horas. Para outras ausências, contacte o seu supervisor imediato.",
            Ordem = 3
        });

        context.Faqs.Add(new Faq
        {
            Pergunta = "Onde vejo o meu espelho de ponto?",
            Resposta = "O espelho de ponto está disponível no mesmo sistema onde consulta o seu holerite. Se tiver dúvidas, procure o RH.",
            TextoParaFala = "O espelho de ponto está disponível no mesmo sistema onde consulta o seu holerite. Se tiver dúvidas, procure o R. H.",
            Ordem = 4
        });

        context.Faqs.Add(new Faq
        {
            Pergunta = "Qual o protocolo para acidentes de trabalho?",
            Resposta = "Em caso de acidente, procure atendimento médico imediatamente e comunique o seu supervisor ou o departamento de Segurança do Trabalho assim que possível.",
            TextoParaFala = "Em caso de acidente, procure atendimento médico imediatamente e comunique o seu supervisor ou o departamento de Segurança do Trabalho assim que possível.",
            Ordem = 5
        });

        context.SaveChanges();
    }

    if (context.Usuarios.Any() && !context.BancoHoras.Any())
    {
        var usuarioTeste = context.Usuarios.First(u => u.Cpf == "12345678900");
        context.BancoHoras.Add(new BancoHoras
        {
            UsuarioId = usuarioTeste.Id,
            HorasAcumuladas = 12.5m, // 12 horas e meia
            DataAtualizacao = DateTime.UtcNow,
            TextoParaFala = "Você possui um saldo positivo de 12 horas e 30 minutos."
        });

        context.SaveChanges();
    }

    if (context.Usuarios.Any() && !context.Ferias.Any())
    {
        var usuarioTeste = context.Usuarios.First(u => u.Cpf == "12345678900");
        context.Ferias.Add(new Ferias
        {
            UsuarioId = usuarioTeste.Id,
            DataInicio = new DateTime(2025, 12, 20),
            DataFim = new DateTime(2026, 1, 5),
            DiasDeSaldo = 10, // Ainda tem 10 dias de saldo
            TextoParaFala = "As suas próximas férias estão programadas para começar no dia 20 de Dezembro de 2025."
        });

        context.SaveChanges();
    }

    // ... (depois do "if" das Ferias) ...

    // VERIFICA SE JÁ EXISTE UM ADMIN
    if (!context.Usuarios.Any(u => u.IsAdmin))
    {
        // Cria um utilizador Admin (RH)
        context.Usuarios.Add(new Usuario
        {
            Nome = "Admin RH",
            Email = "rh@usina.com", // O login dele
            // Senha complexa "Admin@123" (já com hash)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsAdmin = true,

            // Campos de colaborador (não necessários para o admin)
            Cpf = "00000000000",
            Matricula = "ADMIN001",
            PinFoiDefinido = true // Já está definido
        });

        context.SaveChanges();
    }
}