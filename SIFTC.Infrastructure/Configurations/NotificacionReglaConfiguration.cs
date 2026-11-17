using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class NotificacionReglaConfiguration : IEntityTypeConfiguration<NotificacionRegla>
{
    public void Configure(EntityTypeBuilder<NotificacionRegla> builder)
    {
        builder.ToTable("notificaciones_reglas", "seguridad");
        builder.HasKey(r => r.IdRegla);
        builder.Property(r => r.IdRegla).HasColumnName("id_regla").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.IdUsuario).HasColumnName("id_usuario").IsRequired();
        builder.Property(r => r.Canal).HasColumnName("canal").HasMaxLength(10).IsRequired();
        builder.Property(r => r.IdEntidad).HasColumnName("id_entidad");
        builder.Property(r => r.Umbral).HasColumnName("umbral").HasPrecision(8, 4);
        builder.Property(r => r.Activa).HasColumnName("activa").IsRequired();
        builder.Property(r => r.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("now()");
        builder.Property(r => r.FechaActualizacion).HasColumnName("fecha_actualizacion").HasDefaultValueSql("now()");

        builder.HasIndex(r => r.IdUsuario).HasDatabaseName("ix_notificaciones_reglas_usuario");
    }
}
