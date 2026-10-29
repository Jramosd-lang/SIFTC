using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class IndiceCompetitividadConfiguration : IEntityTypeConfiguration<IndiceCompetitividad>
{
    public void Configure(EntityTypeBuilder<IndiceCompetitividad> builder)
    {
        builder.ToTable("fct_indices_competitividad", "analitica");
        builder.HasKey(i => i.IdIndice);
        builder.Property(i => i.IdIndice).HasColumnName("id_indice").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(i => i.FechaCarga).HasColumnName("fecha_carga").HasDefaultValueSql("now()");
        builder.Property(i => i.Ic).HasColumnName("ic").HasPrecision(6, 3);
        builder.Property(i => i.IdLote).HasColumnName("id_lote");
        builder.Property(i => i.IdEntidad).HasColumnName("id_entidad");

        builder.HasIndex(i => new { i.IdEntidad, i.IdLote }).IsUnique();
        builder.HasIndex(i => i.IdLote).HasDatabaseName("ix_indices_lote");
        builder.HasIndex(i => i.FechaCarga).HasDatabaseName("ix_indices_fecha");

        builder.HasOne(i => i.Entidad).WithMany(e => e.IndicesCompetitividad).HasForeignKey(i => i.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LoteCarga>().WithMany().HasForeignKey(i => i.IdLote).OnDelete(DeleteBehavior.Restrict);
    }
}
