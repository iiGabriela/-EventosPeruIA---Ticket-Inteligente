using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pgvector.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Infrastructure.Data;
using EventosIAPeru.Core.Infrastructure.Repositories;
using EventosIAPeru.API.Background;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using EventosIAPeru.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Base de datos PostgreSQL (con pgvector para el RAG)
builder.Services.AddDbContext<EventosPeruIAContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventosPeruIA"), npgsql => npgsql.UseVector()));

// Autenticación (US-02): validamos el token (JWT) que emite Firebase.
// El ProjectId se configura en appsettings.json -> Firebase:ProjectId
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true
        };
    });

// Usuarios (US-01..03, Gabriela)
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Reseñas (US-13, Gabriela)
builder.Services.AddScoped<IResenaRepository, ResenaRepository>();
builder.Services.AddScoped<IResenaService, ResenaService>();

// Moderación (US-15, Gabriela)
builder.Services.AddScoped<IModeracionRepository, ModeracionRepository>();
builder.Services.AddScoped<IModeracionService, ModeracionService>();

// Categorías y Eventos (US-04, US-05)
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IRecomendacionRepository, RecomendacionRepository>();
builder.Services.AddScoped<IRecomendacionService, RecomendacionService>();

// Notificaciones internas (US-14)
builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddHostedService<NotificacionRecordatorioWorker>();

// Ventas y reportes (US-11, US-12)
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IVentaService, VentaService>();

// Compras, entradas y check-in (US-06..09, AnaLu)
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IEntradaRepository, EntradaRepository>();
builder.Services.AddScoped<IEntradaService, EntradaService>();
builder.Services.AddScoped<IQrService, QrService>();

builder.Services.AddControllers();

// Chatbot con recuperación de eventos (US-16): proveedor y modelo configurados en la sección AI.
var chatProvider = builder.Configuration["AI:Provider"] ?? "Ollama";
if (chatProvider is not ("Ollama" or "Gemini")) throw new InvalidOperationException("AI:Provider debe ser Ollama o Gemini.");
var prefix = $"AI:{chatProvider}:";
// Ollama funciona localmente; Gemini requiere su modelo y API key configurados.
var chatEndpoint = builder.Configuration[prefix + "Endpoint"]
    ?? (chatProvider == "Ollama" ? "http://localhost:11434/v1/" : "https://generativelanguage.googleapis.com/v1beta/openai/");
var chatModel = builder.Configuration[prefix + "ChatModel"]
    ?? (chatProvider == "Ollama" ? "muse-glimmer:latest" : throw new InvalidOperationException("Configura AI:Gemini:ChatModel."));
var chatApiKey = builder.Configuration[prefix + "ApiKey"]
    ?? (chatProvider == "Ollama" ? "ollama" : throw new InvalidOperationException("Configura AI:Gemini:ApiKey."));
var chatClient = new ChatClient(chatModel, new ApiKeyCredential(chatApiKey), new OpenAIClientOptions { Endpoint = new Uri(chatEndpoint) });
// El controller usa IChatClient: cambiar de proveedor no modifica el flujo de recuperación.
builder.Services.AddSingleton<IChatClient>(chatClient.AsIChatClient());
// Los embeddings se generan con Ollama y se guardan en PostgreSQL/pgvector.
builder.Services.AddHttpClient("embeddings", client => client.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<EventoRagIndex>();
// Mantiene el índice actualizado por lotes al iniciar y cada 30 segundos.
builder.Services.AddHostedService<EventoRagWorker>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Primero se identifica quién es (token) y luego se revisan permisos
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
