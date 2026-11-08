using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class PublicacionLoteConfiguration : IEntityTypeConfiguration<PublicacionLote>
{
    public void Configure(EntityTypeBuilder<PublicacionLote> builder)
    {
        builder.ToTable("publicaciones_lote", "analitica");
        builder.HasKey(p => p.IdLote);
        builder.Property(p => p.IdLote).HasColumnName("id_lote");
        builder.Property(p => p.Estado).HasColumnName("estado").HasMaxLength(10).IsRequired();
        builder.Property(p => p.IdUsuarioEjecutor).HasColumnName("id_usuario_ejecutor");
        builder.Property(p => p.IdUsuarioDecision).HasColumnName("id_usuario_decision");
        builder.Property(p => p.FechaDecision).HasColumnName("fecha_decision");
        builder.Property(p => p.Comentario).HasColumnName("comentario").HasMaxLength(500);
        builder.HasIndex(p => p.Estado).HasDatabaseName("ix_publicaciones_estado");
    }
}
