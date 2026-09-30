using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Infrastructure.Data;
using EventosIAPeru.Core.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Base de datos PostgreSQL (con pgvector para el RAG)
builder.Services.AddDbContext<EventosPeruIAContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventosPeruIA"), npgsql => npgsql.UseVector()));

// Usuarios (US-01..03, Gabriela) - por ahora solo lo que usan Eventos y Ventas
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Categorías y Eventos (US-04, US-05)
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<IEventoService, EventoService>();

// Ventas y reportes (US-11, US-12)
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IVentaService, VentaService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
