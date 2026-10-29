using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class LoteCargaConfiguration : IEntityTypeConfiguration<LoteCarga>
{
    public void Configure(EntityTypeBuilder<LoteCarga> builder)
    {
        builder.ToTable("lotes_carga", "cruda");
        builder.HasKey(l => l.IdLote);
        builder.Property(l => l.IdLote).HasColumnName("id_lote").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(l => l.NombreArchivo).HasColumnName("nombre_archivo").HasMaxLength(150).IsRequired();
        builder.Property(l => l.FechaCarga).HasColumnName("fecha_carga").HasDefaultValueSql("now()");
        builder.Property(l => l.RegistrosTotales).HasColumnName("registros_totales").HasDefaultValue(0);
        builder.Property(l => l.RegistrosValidos).HasColumnName("registros_validos").HasDefaultValue(0);
        // Columna generada en PostgreSQL (registros_validos / registros_totales).
        builder.Property(l => l.IndiceCalidad).HasColumnName("indice_calidad").HasPrecision(5, 2).ValueGeneratedOnAddOrUpdate();
        builder.Property(l => l.IdUsuario).HasColumnName("id_usuario");
        builder.HasIndex(l => l.IdUsuario).HasDatabaseName("ix_lotes_usuario");
        builder.HasOne(l => l.Usuario).WithMany().HasForeignKey(l => l.IdUsuario).OnDelete(DeleteBehavior.Restrict);
    }
}
