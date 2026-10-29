using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class EntidadConfiguration : IEntityTypeConfiguration<Entidad>
{
    public void Configure(EntityTypeBuilder<Entidad> builder)
    {
        builder.ToTable("tbl_entidades", "limpia");
        builder.HasKey(e => e.IdEntidad);
        builder.Property(e => e.IdEntidad).HasColumnName("id_entidad").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Ticker).HasColumnName("ticker").HasMaxLength(10).IsRequired();
        builder.HasIndex(e => e.Ticker).IsUnique();
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
        builder.Property(e => e.IdSector).HasColumnName("id_sector");
        builder.HasIndex(e => e.IdSector).HasDatabaseName("ix_entidades_sector");
        builder.HasOne(e => e.Sector).WithMany(s => s.Entidades).HasForeignKey(e => e.IdSector).OnDelete(DeleteBehavior.Restrict);
    }
}
