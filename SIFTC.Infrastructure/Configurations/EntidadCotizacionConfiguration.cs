using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class EntidadCotizacionConfiguration : IEntityTypeConfiguration<EntidadCotizacion>
{
    public void Configure(EntityTypeBuilder<EntidadCotizacion> builder)
    {
        // ToView: EF solo la consulta y la excluye de las migraciones.
        builder.ToView("v_entidades_cotizacion", "analitica");
        builder.HasKey(e => e.IdEntidad);
        builder.Property(e => e.IdEntidad).HasColumnName("id_entidad");
        builder.Property(e => e.Ticker).HasColumnName("ticker").HasMaxLength(10);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(80);
        builder.Property(e => e.Sector).HasColumnName("sector").HasMaxLength(30);
        builder.Property(e => e.Ic).HasColumnName("ic").HasPrecision(6, 3);
        builder.Property(e => e.Variacion).HasColumnName("variacion").HasPrecision(6, 3);
        builder.Property(e => e.VariacionPct).HasColumnName("variacion_pct").HasPrecision(8, 4);
    }
}
