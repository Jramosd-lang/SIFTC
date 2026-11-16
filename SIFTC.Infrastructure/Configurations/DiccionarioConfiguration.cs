using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class DiccionarioConfiguration : IEntityTypeConfiguration<Diccionario>
{
    public void Configure(EntityTypeBuilder<Diccionario> builder)
    {
        builder.ToTable("diccionario", "analitica");
        builder.HasKey(d => d.Codigo);
        builder.Property(d => d.Codigo).HasColumnName("codigo").HasMaxLength(60).IsRequired();
        builder.Property(d => d.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired();
        builder.Property(d => d.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(d => d.Definicion).HasColumnName("definicion").IsRequired();
        builder.Property(d => d.Sinonimos).HasColumnName("sinonimos");
        builder.Property(d => d.Formula).HasColumnName("formula");
        builder.Property(d => d.Unidad).HasColumnName("unidad").HasMaxLength(60);
        builder.Property(d => d.Clasificacion).HasColumnName("clasificacion").HasMaxLength(20).IsRequired();

        // HU-DD-08 2.2: índice sobre nombre (la búsqueda ILIKE lo aprovecha).
        builder.HasIndex(d => d.Nombre).HasDatabaseName("ix_diccionario_nombre");
        builder.HasIndex(d => d.Sinonimos).HasDatabaseName("ix_diccionario_sinonimos");
    }
}
