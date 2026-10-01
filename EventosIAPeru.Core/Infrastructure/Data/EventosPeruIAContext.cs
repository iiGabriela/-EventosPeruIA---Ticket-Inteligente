using System;
using System.Collections.Generic;
using EventosIAPeru.Core.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventosIAPeru.Core.Infrastructure.Data;

public partial class EventosPeruIAContext : DbContext
{
    public EventosPeruIAContext()
    {
    }

    public EventosPeruIAContext(DbContextOptions<EventosPeruIAContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auditoria> Auditoria { get; set; }

    public virtual DbSet<CategoriaEvento> CategoriaEvento { get; set; }

    public virtual DbSet<CheckIn> CheckIn { get; set; }

    public virtual DbSet<Compra> Compra { get; set; }

    public virtual DbSet<ConversacionChat> ConversacionChat { get; set; }

    public virtual DbSet<Entrada> Entrada { get; set; }

    public virtual DbSet<Evento> Evento { get; set; }

    public virtual DbSet<EventoEmbedding> EventoEmbedding { get; set; }

    public virtual DbSet<MensajeChat> MensajeChat { get; set; }

    public virtual DbSet<Notificacion> Notificacion { get; set; }

    public virtual DbSet<ReglaModeracion> ReglaModeracion { get; set; }

    public virtual DbSet<ReporteEvento> ReporteEvento { get; set; }

    public virtual DbSet<ResenaEvento> ResenaEvento { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(e => e.AuditoriaId).HasName("auditoria_pkey");

            entity.ToTable("auditoria");

            entity.HasIndex(e => e.Fecha, "ix_auditoria_fecha");

            entity.Property(e => e.AuditoriaId).HasColumnName("auditoria_id");
            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .HasColumnName("accion");
            entity.Property(e => e.ActorUsuarioId).HasColumnName("actor_usuario_id");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha");
            entity.Property(e => e.ReglaId).HasColumnName("regla_id");
            entity.Property(e => e.UsuarioAfectadoId).HasColumnName("usuario_afectado_id");

            entity.HasOne(d => d.ActorUsuario).WithMany(p => p.AuditoriaActorUsuario)
                .HasForeignKey(d => d.ActorUsuarioId)
                .HasConstraintName("auditoria_actor_usuario_id_fkey");

            entity.HasOne(d => d.Evento).WithMany(p => p.Auditoria)
                .HasForeignKey(d => d.EventoId)
                .HasConstraintName("auditoria_evento_id_fkey");

            entity.HasOne(d => d.Regla).WithMany(p => p.Auditoria)
                .HasForeignKey(d => d.ReglaId)
                .HasConstraintName("auditoria_regla_id_fkey");

            entity.HasOne(d => d.UsuarioAfectado).WithMany(p => p.AuditoriaUsuarioAfectado)
                .HasForeignKey(d => d.UsuarioAfectadoId)
                .HasConstraintName("auditoria_usuario_afectado_id_fkey");
        });

        modelBuilder.Entity<CategoriaEvento>(entity =>
        {
            entity.HasKey(e => e.CategoriaId).HasName("categoria_evento_pkey");

            entity.ToTable("categoria_evento");

            entity.HasIndex(e => e.Nombre, "categoria_evento_nombre_key").IsUnique();

            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(60)
                .HasColumnName("nombre");
            entity.Property(e => e.TotalConsultas).HasColumnName("total_consultas");
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.CheckInId).HasName("check_in_pkey");

            entity.ToTable("check_in");

            entity.HasIndex(e => e.EntradaId, "check_in_entrada_id_key").IsUnique();

            entity.Property(e => e.CheckInId).HasColumnName("check_in_id");
            entity.Property(e => e.EntradaId).HasColumnName("entrada_id");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.ValidadoPorUsuarioId).HasColumnName("validado_por_usuario_id");

            entity.HasOne(d => d.Entrada).WithOne(p => p.CheckIn)
                .HasForeignKey<CheckIn>(d => d.EntradaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_in_entrada_id_fkey");

            entity.HasOne(d => d.ValidadoPorUsuario).WithMany(p => p.CheckIn)
                .HasForeignKey(d => d.ValidadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_in_validado_por_usuario_id_fkey");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.CompraId).HasName("compra_pkey");

            entity.ToTable("compra");

            entity.HasIndex(e => new { e.UsuarioId, e.CodigoOperacion }, "compra_usuario_id_codigo_operacion_key").IsUnique();

            entity.HasIndex(e => new { e.EventoId, e.Estado }, "ix_compra_evento_estado");

            entity.HasIndex(e => e.UsuarioId, "ix_compra_usuario");

            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.CodigoOperacion).HasColumnName("codigo_operacion");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .HasDefaultValueSql("'PENDIENTE'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaCompra)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_compra");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(10)
                .HasColumnName("metodo_pago");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(10, 2)
                .HasColumnName("precio_unitario");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.Compra)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("compra_evento_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Compra)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("compra_usuario_id_fkey");
        });

        modelBuilder.Entity<ConversacionChat>(entity =>
        {
            entity.HasKey(e => e.ConversacionId).HasName("conversacion_chat_pkey");

            entity.ToTable("conversacion_chat");

            entity.Property(e => e.ConversacionId).HasColumnName("conversacion_id");
            entity.Property(e => e.FechaInicio)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_inicio");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ConversacionChat)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("conversacion_chat_usuario_id_fkey");
        });

        modelBuilder.Entity<Entrada>(entity =>
        {
            entity.HasKey(e => e.EntradaId).HasName("entrada_pkey");

            entity.ToTable("entrada");

            entity.HasIndex(e => e.CodigoQr, "entrada_codigo_qr_key").IsUnique();

            entity.HasIndex(e => new { e.AsistenteUsuarioId, e.CompraId }, "ix_entrada_asistente");

            entity.HasIndex(e => e.CompraId, "ix_entrada_compra");

            entity.Property(e => e.EntradaId).HasColumnName("entrada_id");
            entity.Property(e => e.AsistenteUsuarioId).HasColumnName("asistente_usuario_id");
            entity.Property(e => e.CodigoQr)
                .HasMaxLength(64)
                .HasColumnName("codigo_qr");
            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(10)
                .HasDefaultValueSql("'VALIDA'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_generacion");
            entity.Property(e => e.TipoAcceso)
                .HasMaxLength(20)
                .HasDefaultValueSql("'GENERAL'::character varying")
                .HasColumnName("tipo_acceso");
            entity.Property(e => e.TitularDocumento)
                .HasMaxLength(20)
                .HasColumnName("titular_documento");
            entity.Property(e => e.TitularNombre)
                .HasMaxLength(120)
                .HasColumnName("titular_nombre");

            entity.HasOne(d => d.AsistenteUsuario).WithMany(p => p.Entrada)
                .HasForeignKey(d => d.AsistenteUsuarioId)
                .HasConstraintName("entrada_asistente_usuario_id_fkey");

            entity.HasOne(d => d.Compra).WithMany(p => p.Entrada)
                .HasForeignKey(d => d.CompraId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("entrada_compra_id_fkey");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.EventoId).HasName("evento_pkey");

            entity.ToTable("evento");

            entity.HasIndex(e => new { e.Estado, e.EstadoModeracion, e.CategoriaId, e.FechaInicio }, "ix_evento_busqueda");

            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.AforoTotal).HasColumnName("aforo_total");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Departamento)
                .HasMaxLength(60)
                .HasColumnName("departamento");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(300)
                .HasColumnName("direccion");
            entity.Property(e => e.Distrito)
                .HasMaxLength(60)
                .HasColumnName("distrito");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .HasDefaultValueSql("'BORRADOR'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.EstadoModeracion)
                .HasMaxLength(15)
                .HasDefaultValueSql("'EN_REVISION'::character varying")
                .HasColumnName("estado_moderacion");
            entity.Property(e => e.FechaActualizacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_actualizacion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
            entity.Property(e => e.MotivoModeracion)
                .HasMaxLength(500)
                .HasColumnName("motivo_moderacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.Provincia)
                .HasMaxLength(60)
                .HasColumnName("provincia");
            entity.Property(e => e.Sede)
                .HasMaxLength(150)
                .HasColumnName("sede");
            entity.Property(e => e.ValidacionMunicipal).HasColumnName("validacion_municipal");
            entity.Property(e => e.ValidacionSunat).HasColumnName("validacion_sunat");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Evento)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("evento_categoria_id_fkey");

            entity.HasOne(d => d.Organizador).WithMany(p => p.Evento)
                .HasForeignKey(d => d.OrganizadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("evento_organizador_id_fkey");
        });

        modelBuilder.Entity<EventoEmbedding>(entity =>
        {
            entity.HasKey(e => e.EmbeddingId).HasName("evento_embedding_pkey");

            entity.ToTable("evento_embedding");

            entity.HasIndex(e => new { e.EventoId, e.NumeroFragmento }, "evento_embedding_evento_id_numero_fragmento_key").IsUnique();

            entity.Property(e => e.EmbeddingId).HasColumnName("embedding_id");
            entity.Property(e => e.Contenido).HasColumnName("contenido");
            entity.Property(e => e.Embedding).HasColumnName("embedding");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaIndexacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_indexacion");
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .HasColumnName("modelo");
            entity.Property(e => e.NumeroFragmento).HasColumnName("numero_fragmento");

            entity.HasOne(d => d.Evento).WithMany(p => p.EventoEmbedding)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("evento_embedding_evento_id_fkey");
        });

        modelBuilder.Entity<MensajeChat>(entity =>
        {
            entity.HasKey(e => e.MensajeId).HasName("mensaje_chat_pkey");

            entity.ToTable("mensaje_chat");

            entity.Property(e => e.MensajeId).HasColumnName("mensaje_id");
            entity.Property(e => e.Contenido).HasColumnName("contenido");
            entity.Property(e => e.ConversacionId).HasColumnName("conversacion_id");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha");
            entity.Property(e => e.Rol)
                .HasMaxLength(10)
                .HasColumnName("rol");

            entity.HasOne(d => d.Conversacion).WithMany(p => p.MensajeChat)
                .HasForeignKey(d => d.ConversacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("mensaje_chat_conversacion_id_fkey");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.NotificacionId).HasName("notificacion_pkey");

            entity.ToTable("notificacion");

            entity.Property(e => e.NotificacionId).HasColumnName("notificacion_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(1000)
                .HasColumnName("mensaje");
            entity.Property(e => e.Titulo)
                .HasMaxLength(120)
                .HasColumnName("titulo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.Notificacion)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notificacion_evento_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificacion)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notificacion_usuario_id_fkey");
        });

        modelBuilder.Entity<ReglaModeracion>(entity =>
        {
            entity.HasKey(e => e.ReglaId).HasName("regla_moderacion_pkey");

            entity.ToTable("regla_moderacion");

            entity.Property(e => e.ReglaId).HasColumnName("regla_id");
            entity.Property(e => e.Accion)
                .HasMaxLength(15)
                .HasColumnName("accion");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.Campo)
                .HasMaxLength(20)
                .HasColumnName("campo");
            entity.Property(e => e.CreadaPorUsuarioId).HasColumnName("creada_por_usuario_id");
            entity.Property(e => e.Criterio).HasColumnName("criterio");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Operador)
                .HasMaxLength(12)
                .HasColumnName("operador");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .HasColumnName("tipo");
            entity.Property(e => e.Valor)
                .HasPrecision(12, 2)
                .HasColumnName("valor");

            entity.HasOne(d => d.CreadaPorUsuario).WithMany(p => p.ReglaModeracion)
                .HasForeignKey(d => d.CreadaPorUsuarioId)
                .HasConstraintName("regla_moderacion_creada_por_usuario_id_fkey");
        });

        modelBuilder.Entity<ReporteEvento>(entity =>
        {
            entity.HasKey(e => e.ReporteId).HasName("reporte_evento_pkey");

            entity.ToTable("reporte_evento");

            entity.HasIndex(e => new { e.Estado, e.EventoId }, "ix_reporte_evento_estado");

            entity.Property(e => e.ReporteId).HasColumnName("reporte_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .HasDefaultValueSql("'PENDIENTE'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaReporte)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_reporte");
            entity.Property(e => e.FechaResolucion).HasColumnName("fecha_resolucion");
            entity.Property(e => e.Motivo)
                .HasMaxLength(500)
                .HasColumnName("motivo");
            entity.Property(e => e.Origen)
                .HasMaxLength(10)
                .HasDefaultValueSql("'USUARIO'::character varying")
                .HasColumnName("origen");
            entity.Property(e => e.ReglaId).HasColumnName("regla_id");
            entity.Property(e => e.ReportadoPorUsuarioId).HasColumnName("reportado_por_usuario_id");
            entity.Property(e => e.Resolucion).HasColumnName("resolucion");
            entity.Property(e => e.ResueltoPorUsuarioId).HasColumnName("resuelto_por_usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.ReporteEvento)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reporte_evento_evento_id_fkey");

            entity.HasOne(d => d.Regla).WithMany(p => p.ReporteEvento)
                .HasForeignKey(d => d.ReglaId)
                .HasConstraintName("reporte_evento_regla_id_fkey");

            entity.HasOne(d => d.ReportadoPorUsuario).WithMany(p => p.ReporteEventoReportadoPorUsuario)
                .HasForeignKey(d => d.ReportadoPorUsuarioId)
                .HasConstraintName("reporte_evento_reportado_por_usuario_id_fkey");

            entity.HasOne(d => d.ResueltoPorUsuario).WithMany(p => p.ReporteEventoResueltoPorUsuario)
                .HasForeignKey(d => d.ResueltoPorUsuarioId)
                .HasConstraintName("reporte_evento_resuelto_por_usuario_id_fkey");
        });

        modelBuilder.Entity<ResenaEvento>(entity =>
        {
            entity.HasKey(e => e.ResenaId).HasName("resena_evento_pkey");

            entity.ToTable("resena_evento");

            entity.HasIndex(e => new { e.UsuarioId, e.EventoId }, "resena_evento_usuario_id_evento_id_key").IsUnique();

            entity.Property(e => e.ResenaId).HasColumnName("resena_id");
            entity.Property(e => e.Calificacion).HasColumnName("calificacion");
            entity.Property(e => e.Comentario)
                .HasMaxLength(1000)
                .HasColumnName("comentario");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.ResenaEvento)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resena_evento_evento_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ResenaEvento)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resena_evento_usuario_id_fkey");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("rol_pkey");

            entity.ToTable("rol");

            entity.HasIndex(e => e.Nombre, "rol_nombre_key").IsUnique();

            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("usuario_pkey");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.FirebaseUid, "usuario_firebase_uid_key").IsUnique();

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .HasDefaultValueSql("'ACTIVO'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.FirebaseUid)
                .HasMaxLength(128)
                .HasColumnName("firebase_uid");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
            entity.Property(e => e.Verificado).HasColumnName("verificado");

            entity.HasMany(d => d.Categoria).WithMany(p => p.Usuario)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioInteres",
                    r => r.HasOne<CategoriaEvento>().WithMany()
                        .HasForeignKey("CategoriaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("usuario_interes_categoria_id_fkey"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("usuario_interes_usuario_id_fkey"),
                    j =>
                    {
                        j.HasKey("UsuarioId", "CategoriaId").HasName("usuario_interes_pkey");
                        j.ToTable("usuario_interes");
                        j.IndexerProperty<int>("UsuarioId").HasColumnName("usuario_id");
                        j.IndexerProperty<int>("CategoriaId").HasColumnName("categoria_id");
                    });

            entity.HasMany(d => d.Rol).WithMany(p => p.Usuario)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioRol",
                    r => r.HasOne<Rol>().WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("usuario_rol_rol_id_fkey"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("usuario_rol_usuario_id_fkey"),
                    j =>
                    {
                        j.HasKey("UsuarioId", "RolId").HasName("usuario_rol_pkey");
                        j.ToTable("usuario_rol");
                        j.IndexerProperty<int>("UsuarioId").HasColumnName("usuario_id");
                        j.IndexerProperty<int>("RolId").HasColumnName("rol_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
