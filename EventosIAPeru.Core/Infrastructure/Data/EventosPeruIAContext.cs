using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Infrastructure.Data
{
    public partial class EventosPeruIaContext : DbContext
    {
        public EventosPeruIaContext()
        {
        }

        public EventosPeruIaContext(DbContextOptions<EventosPeruIaContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Categorias> Categorias { get; set; }
        public virtual DbSet<Compras> Compras { get; set; }
        public virtual DbSet<Entradas> Entradas { get; set; }
        public virtual DbSet<Eventos> Eventos { get; set; }
        public virtual DbSet<HistorialVistas> HistorialVistas { get; set; }
        public virtual DbSet<LogAuditoria> LogAuditoria { get; set; }
        public virtual DbSet<Notificaciones> Notificaciones { get; set; }
        public virtual DbSet<PreferenciasUsuario> PreferenciasUsuario { get; set; }
        public virtual DbSet<ReglasModeracionIa> ReglasModeracionIa { get; set; }
        public virtual DbSet<ReportesEventos> ReportesEventos { get; set; }
        public virtual DbSet<ReportesUsuarios> ReportesUsuarios { get; set; }
        public virtual DbSet<Resenas> Resenas { get; set; }
        public virtual DbSet<Roles> Roles { get; set; }
        public virtual DbSet<UsuarioRoles> UsuarioRoles { get; set; }
        public virtual DbSet<Usuarios> Usuarios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=EventosPeruIA;Integrated Security=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Categorias>(entity =>
            {
                entity.HasKey(e => e.CategoriaId);
                entity.Property(e => e.CategoriaId).HasColumnName("CategoriaID");
                entity.Property(e => e.Nombre).HasMaxLength(100);
                entity.Property(e => e.Activa).HasDefaultValue(true);
            });

            modelBuilder.Entity<Compras>(entity =>
            {
                entity.HasKey(e => e.CompraId);
                entity.Property(e => e.CompraId).HasColumnName("CompraID");
                entity.Property(e => e.AsistenteId).HasColumnName("AsistenteID");
                entity.Property(e => e.EventoId).HasColumnName("EventoID");
                entity.Property(e => e.MontoTotal).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.MetodoPago).HasMaxLength(30);
                entity.Property(e => e.TitularNombre).HasMaxLength(150);
                entity.Property(e => e.TitularDocumento).HasMaxLength(20);

                entity.HasOne(d => d.Asistente)
                    .WithMany(p => p.Compras)
                    .HasForeignKey(d => d.AsistenteId);

                entity.HasOne(d => d.Evento)
                    .WithMany(p => p.Compras)
                    .HasForeignKey(d => d.EventoId);
            });

            modelBuilder.Entity<Entradas>(entity =>
            {
                entity.HasKey(e => e.EntradaId);
                entity.Property(e => e.EntradaId).HasColumnName("EntradaID");
                entity.Property(e => e.CompraId).HasColumnName("CompraID");
                entity.Property(e => e.PrecioPagado).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Valida");
                entity.Property(e => e.TipoAcceso).HasMaxLength(50).HasDefaultValue("General");
                entity.Property(e => e.TokenSecreto).HasMaxLength(64);

                entity.HasOne(d => d.Compra)
                    .WithMany(p => p.Entradas)
                    .HasForeignKey(d => d.CompraId);
            });

            modelBuilder.Entity<Eventos>(entity =>
            {
                entity.HasKey(e => e.EventoId);
                entity.Property(e => e.EventoId).HasColumnName("EventoID");
                entity.Property(e => e.CategoriaId).HasColumnName("CategoriaID");
                entity.Property(e => e.OrganizadorId).HasColumnName("OrganizadorID");
                entity.Property(e => e.Titulo).HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("text");
                entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Distrito).HasMaxLength(60);
                entity.Property(e => e.Ubicacion).HasMaxLength(300);
                entity.Property(e => e.ImagenUrl).HasMaxLength(300).HasColumnName("ImagenURL");
                entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Activo");
                entity.Property(e => e.EstadoModeracion).HasMaxLength(20).HasDefaultValue("Conforme");
                entity.Property(e => e.MotivoModeracion).HasMaxLength(200);

                entity.HasOne(d => d.Categoria)
                    .WithMany(p => p.Eventos)
                    .HasForeignKey(d => d.CategoriaId);

                entity.HasOne(d => d.Organizador)
                    .WithMany(p => p.Eventos)
                    .HasForeignKey(d => d.OrganizadorId);
            });

            modelBuilder.Entity<HistorialVistas>(entity =>
            {
                entity.HasKey(e => e.VistaId);
                entity.ToTable("HistorialVistas");

                entity.HasOne(d => d.Usuario)
                    .WithMany(p => p.HistorialVistas)
                    .HasForeignKey(d => d.UsuarioId);

                entity.HasOne(d => d.Evento)
                    .WithMany(p => p.HistorialVistas)
                    .HasForeignKey(d => d.EventoId);

                entity.HasOne(d => d.Categoria)
                    .WithMany(p => p.HistorialVistas)
                    .HasForeignKey(d => d.CategoriaId);
            });

            modelBuilder.Entity<LogAuditoria>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.Accion).HasMaxLength(100);
                entity.Property(e => e.EntidadAfectada).HasMaxLength(100);
                entity.Property(e => e.Detalle).HasColumnType("text");

                entity.HasOne(d => d.Admin)
                    .WithMany(p => p.LogAuditoria)
                    .HasForeignKey(d => d.AdminId);
            });

            modelBuilder.Entity<Notificaciones>(entity =>
            {
                entity.HasKey(e => e.NotificacionId);
                entity.Property(e => e.Tipo).HasMaxLength(50);
                entity.Property(e => e.Mensaje).HasColumnType("text");

                entity.HasOne(d => d.Usuario)
                    .WithMany(p => p.Notificaciones)
                    .HasForeignKey(d => d.UsuarioId);

                entity.HasOne(d => d.Evento)
                    .WithMany(p => p.Notificaciones)
                    .HasForeignKey(d => d.EventoId)
                    .IsRequired(false);
            });

            modelBuilder.Entity<PreferenciasUsuario>(entity =>
            {
                entity.HasKey(e => new { e.UsuarioId, e.CategoriaId });

                entity.HasOne(d => d.Usuario)
                    .WithMany(p => p.PreferenciasUsuario)
                    .HasForeignKey(d => d.UsuarioId);

                entity.HasOne(d => d.Categoria)
                    .WithMany(p => p.PreferenciasUsuario)
                    .HasForeignKey(d => d.CategoriaId);
            });

            modelBuilder.Entity<ReglasModeracionIa>(entity =>
            {
                entity.HasKey(e => e.ReglaId);
                entity.Property(e => e.Nombre).HasMaxLength(150);
                entity.Property(e => e.Descripcion).HasColumnType("text");
                entity.Property(e => e.AccionAutomatica).HasMaxLength(100);
            });

            modelBuilder.Entity<ReportesEventos>(entity =>
            {
                entity.HasKey(e => e.ReporteId);
                entity.Property(e => e.Motivo).HasColumnType("text");
                entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Pendiente");

                entity.HasOne(d => d.Evento)
                    .WithMany(p => p.ReportesEventos)
                    .HasForeignKey(d => d.EventoId);

                entity.HasOne(d => d.UsuarioReporta)
                    .WithMany(p => p.ReportesEventos)
                    .HasForeignKey(d => d.UsuarioReportaId)
                    .IsRequired(false);
            });

            modelBuilder.Entity<ReportesUsuarios>(entity =>
            {
                entity.HasKey(e => e.ReporteId);
                entity.Property(e => e.Motivo).HasColumnType("text");
                entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Pendiente");

                entity.HasOne(d => d.UsuarioReportado)
                    .WithMany(p => p.ReportesUsuariosUsuarioReportado)
                    .HasForeignKey(d => d.UsuarioReportadoId);

                entity.HasOne(d => d.UsuarioReporta)
                    .WithMany(p => p.ReportesUsuariosUsuarioReporta)
                    .HasForeignKey(d => d.UsuarioReportaId)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Resenas>(entity =>
            {
                entity.HasKey(e => e.ResenaId);
                entity.Property(e => e.Calificacion).HasDefaultValue(0);
                entity.Property(e => e.Comentario).HasColumnType("text");

                entity.HasOne(d => d.Asistente)
                    .WithMany(p => p.Resenas)
                    .HasForeignKey(d => d.AsistenteId);

                entity.HasOne(d => d.Evento)
                    .WithMany(p => p.Resenas)
                    .HasForeignKey(d => d.EventoId);
            });

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.HasKey(e => e.RolId);
                entity.Property(e => e.NombreRol).HasMaxLength(50);
            });

            modelBuilder.Entity<UsuarioRoles>(entity =>
            {
                entity.HasKey(e => new { e.UsuarioId, e.RolId });

                entity.HasOne(d => d.Usuario)
                    .WithMany(p => p.UsuarioRoles)
                    .HasForeignKey(d => d.UsuarioId);

                entity.HasOne(d => d.Rol)
                    .WithMany(p => p.UsuarioRoles)
                    .HasForeignKey(d => d.RolId);
            });

            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.HasKey(e => e.UsuarioId);
                entity.Property(e => e.FirebaseUid).HasMaxLength(128);
                entity.Property(e => e.Nombre).HasMaxLength(150);
                entity.Property(e => e.Correo).HasMaxLength(100);
                entity.Property(e => e.Dni).HasMaxLength(20);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
