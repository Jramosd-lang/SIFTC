using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class MatrizFinancieraCrudaConfiguration : IEntityTypeConfiguration<MatrizFinancieraCruda>
{
    public void Configure(EntityTypeBuilder<MatrizFinancieraCruda> builder)
    {
        builder.ToTable("ext_matriz_financiera_entidades", "cruda");
        builder.HasKey(m => m.IdCruda);
        builder.Property(m => m.IdCruda).HasColumnName("id_cruda").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(m => m.IdLote).HasColumnName("id_lote");
        builder.HasIndex(m => m.IdLote).HasDatabaseName("ix_ext_matriz_lote");
        builder.HasOne(m => m.Lote).WithMany(l => l.RegistrosCrudos).HasForeignKey(m => m.IdLote).OnDelete(DeleteBehavior.Restrict);
        builder.Property(m => m.CodigoEntidad).HasColumnName("codigo_entidad");
        builder.Property(m => m.NombreEntidad).HasColumnName("nombre_entidad");
        builder.Property(m => m.Sector).HasColumnName("sector");
        builder.Property(m => m.Periodo).HasColumnName("periodo");
        builder.Property(m => m.UtilidadNeta).HasColumnName("utilidad_neta");
        builder.Property(m => m.Patrimonio).HasColumnName("patrimonio");
        builder.Property(m => m.ActivosTotales).HasColumnName("activos_totales");
        builder.Property(m => m.Ebitda).HasColumnName("ebitda");
        builder.Property(m => m.Ingresos).HasColumnName("ingresos");
        builder.Property(m => m.ActivoCorriente).HasColumnName("activo_corriente");
        builder.Property(m => m.PasivoCorriente).HasColumnName("pasivo_corriente");
        builder.Property(m => m.Inventarios).HasColumnName("inventarios");
        builder.Property(m => m.PasivoTotal).HasColumnName("pasivo_total");
        builder.Property(m => m.Ebit).HasColumnName("ebit");
        builder.Property(m => m.GastosFinancieros).HasColumnName("gastos_financieros");
        builder.Property(m => m.FechaCarga).HasColumnName("fecha_carga").HasDefaultValueSql("now()");
    }
}
