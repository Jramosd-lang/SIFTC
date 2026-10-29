using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class ValorMetricaConfiguration : IEntityTypeConfiguration<ValorMetrica>
{
    public void Configure(EntityTypeBuilder<ValorMetrica> builder)
    {
        builder.ToTable("fct_valores_metrica", "analitica");
        builder.HasKey(v => v.IdValor);
        builder.Property(v => v.IdValor).HasColumnName("id_valor").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(v => v.Periodo).HasColumnName("periodo");
        builder.Property(v => v.Valor).HasColumnName("valor").HasPrecision(14, 6);
        builder.Property(v => v.ValorNormalizado).HasColumnName("valor_normalizado").HasPrecision(9, 8);
        builder.Property(v => v.FechaCarga).HasColumnName("fecha_carga").HasDefaultValueSql("now()");
        builder.Property(v => v.IdLote).HasColumnName("id_lote");
        builder.Property(v => v.IdEntidad).HasColumnName("id_entidad");
        builder.Property(v => v.IdMetrica).HasColumnName("id_metrica");
        builder.Property(v => v.IdRegistroOrigen).HasColumnName("id_registro_origen");

        builder.HasIndex(v => new { v.IdEntidad, v.IdMetrica, v.Periodo }).IsUnique();
        builder.HasIndex(v => v.IdLote).HasDatabaseName("ix_valores_lote");
        builder.HasIndex(v => v.IdMetrica).HasDatabaseName("ix_valores_metrica");
        builder.HasIndex(v => v.IdRegistroOrigen).HasDatabaseName("ix_valores_origen");

        builder.HasOne(v => v.Entidad).WithMany(e => e.ValoresMetrica).HasForeignKey(v => v.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Metrica).WithMany().HasForeignKey(v => v.IdMetrica).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LoteCarga>().WithMany().HasForeignKey(v => v.IdLote).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DatoFinanciero>().WithMany().HasForeignKey(v => v.IdRegistroOrigen).OnDelete(DeleteBehavior.Restrict);
    }
}
