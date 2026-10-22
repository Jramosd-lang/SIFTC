using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("tbl_sectores", "limpia");
        builder.HasKey(s => s.IdSector);
        builder.Property(s => s.IdSector).HasColumnName("id_sector").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.Nombre).HasColumnName("nombre").HasMaxLength(30).IsRequired();
        builder.HasIndex(s => s.Nombre).IsUnique();
    }
}
