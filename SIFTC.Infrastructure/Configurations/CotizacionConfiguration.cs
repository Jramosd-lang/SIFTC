using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class CotizacionConfiguration : IEntityTypeConfiguration<Cotizacion>
{
    public void Configure(EntityTypeBuilder<Cotizacion> builder)
    {
        builder.ToTable("fct_cotizaciones", "analitica");
        builder.HasKey(c => c.IdCotizacion);
        builder.Property(c => c.IdCotizacion).HasColumnName("id_cotizacion").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.Fecha).HasColumnName("fecha");
        builder.Property(c => c.Apertura).HasColumnName("apertura").HasPrecision(6, 3);
        builder.Property(c => c.Maximo).HasColumnName("maximo").HasPrecision(6, 3);
        builder.Property(c => c.Minimo).HasColumnName("minimo").HasPrecision(6, 3);
        builder.Property(c => c.Cierre).HasColumnName("cierre").HasPrecision(6, 3);
        builder.Property(c => c.VariacionPct).HasColumnName("variacion_pct").HasPrecision(8, 4);
        builder.Property(c => c.VolumenActualizaciones).HasColumnName("volumen_actualizaciones").HasDefaultValue(0);
        builder.Property(c => c.FechaCarga).HasColumnName("fecha_carga").HasDefaultValueSql("now()");
        builder.Property(c => c.IdEntidad).HasColumnName("id_entidad");

        // Sirve también para leer la última cotización de cada entidad.
        builder.HasIndex(c => new { c.IdEntidad, c.Fecha }).IsUnique();

        builder.HasOne(c => c.Entidad).WithMany(e => e.Cotizaciones).HasForeignKey(c => c.IdEntidad).OnDelete(DeleteBehavior.Restrict);
    }
}
