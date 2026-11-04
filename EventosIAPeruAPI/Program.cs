using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pgvector.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Core.Settings;
using EventosIAPeru.Core.Infrastructure.Data;
using EventosIAPeru.Core.Infrastructure.Repositories;
using EventosIAPeru.API.Background;
using EventosIAPeru.API.Middleware;
using EventosIAPeru.API.OpenApi;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using EventosIAPeru.API.Services;
using EventosIAPeru.API.Hubs;

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
// US-07: QR dinámico firmado (appsettings -> Qr:ClaveFirma, Qr:SegundosValidez)
var qrSettings = builder.Configuration.GetSection("Qr").Get<QrSettings>() ?? new QrSettings();
builder.Services.AddSingleton<IQrService>(new QrService(qrSettings));

// US-09: cupos en tiempo real (SignalR en /hubs/aforo)
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificadorAforo, NotificadorAforoSignalR>();

// Frontend Vue + Quasar: orígenes permitidos en appsettings.json -> Cors:Origenes
var origenesFrontend = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(origenesFrontend)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // SignalR (US-09) necesita credenciales cuando el frontend está en otro origen
              .AllowCredentials()
              // Para que el navegador pueda leer el nombre del archivo exportado (US-12)
              .WithExposedHeaders("Content-Disposition"));
});

builder.Services.AddControllers();

// Chatbot con recuperación de eventos (US-16): proveedor y modelo configurados en la sección AI.
var chatProvider = builder.Configuration["AI:Provider"] ?? "Ollama";
if (chatProvider is not ("Ollama" or "Gemini")) throw new InvalidOperationException("AI:Provider debe ser Ollama o Gemini.");
var prefix = $"AI:{chatProvider}:";
// Ollama funciona localmente; Gemini requiere su modelo y API key configurados.
var chatEndpoint = builder.Configuration[prefix + "Endpoint"]
    ?? (chatProvider == "Ollama" ? "http://localhost:11434/v1/" : "https://generativelanguage.googleapis.com/v1beta/openai/");
var chatModel = builder.Configuration[prefix + "ChatModel"]
    ?? (chatProvider == "Ollama" ? "qwen2.5:7b" : throw new InvalidOperationException("Configura AI:Gemini:ChatModel."));
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

// Documentación OpenAPI (incluye los comentarios /// de los controllers) + esquemas de seguridad
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<SeguridadOpenApiTransformer>());

var app = builder.Build();

// Middleware propio: errores no controlados -> JSON { mensaje } sin trazas internas
app.UseMiddleware<ManejoErroresMiddleware>();

// Swagger UI en /swagger (solo Development)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "EventosPeruIA API v1");
        options.DocumentTitle = "EventosPeruIA API";
    });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

// Primero se identifica quién es (token) y luego se revisan permisos
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AforoHub>("/hubs/aforo");

app.Run();
