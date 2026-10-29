using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class DimensionConfiguration : IEntityTypeConfiguration<Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension> builder)
    {
        builder.ToTable("dim_dimension", "analitica");
        builder.HasKey(d => d.IdDimension);
        builder.Property(d => d.IdDimension).HasColumnName("id_dimension").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.Nombre).HasColumnName("nombre").HasMaxLength(30).IsRequired();
        builder.HasIndex(d => d.Nombre).IsUnique();
        builder.Property(d => d.PesoIc).HasColumnName("peso_ic").HasPrecision(4, 3).HasDefaultValue(0m);
    }
}
