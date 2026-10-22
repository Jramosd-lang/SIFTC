using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class MetricaConfiguration : IEntityTypeConfiguration<Metrica>
{
    public void Configure(EntityTypeBuilder<Metrica> builder)
    {
        builder.ToTable("dim_metrica", "analitica");
        builder.HasKey(m => m.IdMetrica);
        builder.Property(m => m.IdMetrica).HasColumnName("id_metrica").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(m => m.Codigo).HasColumnName("codigo").HasMaxLength(10).IsRequired();
        builder.HasIndex(m => m.Codigo).IsUnique();
        builder.Property(m => m.Nombre).HasColumnName("nombre").HasMaxLength(50).IsRequired();
        builder.Property(m => m.Formula).HasColumnName("formula").HasMaxLength(120).IsRequired();
        builder.Property(m => m.Frecuencia).HasColumnName("frecuencia").HasMaxLength(12).IsRequired();
        builder.Property(m => m.IdDimension).HasColumnName("id_dimension");
        builder.HasIndex(m => m.IdDimension).HasDatabaseName("ix_metricas_dimension");
        builder.HasOne(m => m.Dimension).WithMany(d => d.Metricas).HasForeignKey(m => m.IdDimension).OnDelete(DeleteBehavior.Restrict);
    }
}
