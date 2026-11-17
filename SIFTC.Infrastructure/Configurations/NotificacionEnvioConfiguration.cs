using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class NotificacionEnvioConfiguration : IEntityTypeConfiguration<NotificacionEnvio>
{
    public void Configure(EntityTypeBuilder<NotificacionEnvio> builder)
    {
        builder.ToTable("notificaciones_envios", "seguridad");
        builder.HasKey(e => e.IdEnvio);
        builder.Property(e => e.IdEnvio).HasColumnName("id_envio").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.IdUsuario).HasColumnName("id_usuario").IsRequired();
        builder.Property(e => e.IdRegla).HasColumnName("id_regla");
        builder.Property(e => e.Canal).HasColumnName("canal").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Asunto).HasColumnName("asunto").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Cuerpo).HasColumnName("cuerpo").IsRequired();
        builder.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(12).IsRequired();
        builder.Property(e => e.Intentos).HasColumnName("intentos").IsRequired();
        builder.Property(e => e.MaxIntentos).HasColumnName("max_intentos").IsRequired();
        builder.Property(e => e.UltimoError).HasColumnName("ultimo_error").HasMaxLength(500);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("now()");
        builder.Property(e => e.FechaUltimoIntento).HasColumnName("fecha_ultimo_intento");
        builder.Property(e => e.FechaEnvio).HasColumnName("fecha_envio");

        builder.HasIndex(e => e.FechaCreacion).HasDatabaseName("ix_notificaciones_envios_fecha");
        builder.HasIndex(e => e.IdUsuario).HasDatabaseName("ix_notificaciones_envios_usuario");
    }
}
