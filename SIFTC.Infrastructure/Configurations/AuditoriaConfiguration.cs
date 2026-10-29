using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("auditorias", "seguridad");
        builder.HasKey(a => a.IdBitacora);
        builder.Property(a => a.IdBitacora).HasColumnName("id_bitacora").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.Accion).HasColumnName("accion").HasMaxLength(40).IsRequired();
        builder.Property(a => a.Detalle).HasColumnName("detalle").HasMaxLength(250);
        builder.Property(a => a.FechaEvento).HasColumnName("fecha_evento").HasDefaultValueSql("now()");
        builder.Property(a => a.IdUsuario).HasColumnName("id_usuario");
        builder.HasIndex(a => a.IdUsuario).HasDatabaseName("ix_auditorias_usuario");
        builder.HasIndex(a => a.FechaEvento).HasDatabaseName("ix_auditorias_fecha");
    }
}
