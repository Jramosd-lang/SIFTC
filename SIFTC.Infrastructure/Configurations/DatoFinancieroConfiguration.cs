using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class DatoFinancieroConfiguration : IEntityTypeConfiguration<DatoFinanciero>
{
    public void Configure(EntityTypeBuilder<DatoFinanciero> builder)
    {
        builder.ToTable("tbl_datos_financieros", "limpia");
        builder.HasKey(d => d.IdDato);
        builder.Property(d => d.IdDato).HasColumnName("id_dato").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.Periodo).HasColumnName("periodo");
        builder.Property(d => d.UtilidadNeta).HasColumnName("utilidad_neta").HasPrecision(15, 2);
        builder.Property(d => d.Patrimonio).HasColumnName("patrimonio").HasPrecision(15, 2);
        builder.Property(d => d.ActivosTotales).HasColumnName("activos_totales").HasPrecision(15, 2);
        builder.Property(d => d.PasivoTotal).HasColumnName("pasivo_total").HasPrecision(15, 2);
        builder.Property(d => d.ActivoCorriente).HasColumnName("activo_corriente").HasPrecision(15, 2);
        builder.Property(d => d.PasivoCorriente).HasColumnName("pasivo_corriente").HasPrecision(15, 2);
        builder.Property(d => d.Inventarios).HasColumnName("inventarios").HasPrecision(15, 2);
        builder.Property(d => d.Ebitda).HasColumnName("ebitda").HasPrecision(15, 2);
        builder.Property(d => d.Ebit).HasColumnName("ebit").HasPrecision(15, 2);
        builder.Property(d => d.Ingresos).HasColumnName("ingresos").HasPrecision(15, 2);
        builder.Property(d => d.GastosFinancieros).HasColumnName("gastos_financieros").HasPrecision(15, 2);
        builder.Property(d => d.FechaCarga).HasColumnName("fecha_carga").HasDefaultValueSql("now()");
        builder.Property(d => d.IdLote).HasColumnName("id_lote");
        builder.Property(d => d.IdEntidad).HasColumnName("id_entidad");
        builder.Property(d => d.IdRegistroOrigen).HasColumnName("id_registro_origen");

        builder.HasIndex(d => d.IdRegistroOrigen).IsUnique();
        builder.HasIndex(d => new { d.IdEntidad, d.Periodo }).IsUnique();
        builder.HasIndex(d => d.IdLote).HasDatabaseName("ix_datos_fin_lote");
        builder.HasIndex(d => d.Periodo).HasDatabaseName("ix_datos_fin_periodo");

        builder.HasOne(d => d.Entidad).WithMany(e => e.DatosFinancieros).HasForeignKey(d => d.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LoteCarga>().WithMany().HasForeignKey(d => d.IdLote).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MatrizFinancieraCruda>().WithOne().HasForeignKey<DatoFinanciero>(d => d.IdRegistroOrigen).OnDelete(DeleteBehavior.Restrict);
    }
}
