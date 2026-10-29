using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class EntidadSeguidaConfiguration : IEntityTypeConfiguration<EntidadSeguida>
{
    public void Configure(EntityTypeBuilder<EntidadSeguida> builder)
    {
        builder.ToTable("entidades_seguidas", "seguridad");
        builder.HasKey(s => new { s.IdUsuario, s.IdEntidad });
        builder.Property(s => s.IdUsuario).HasColumnName("id_usuario");
        builder.Property(s => s.IdEntidad).HasColumnName("id_entidad");
        builder.HasIndex(s => s.IdEntidad).HasDatabaseName("ix_seguidas_entidad");

        builder.HasOne(s => s.Usuario).WithMany().HasForeignKey(s => s.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Entidad).WithMany(e => e.Seguidores).HasForeignKey(s => s.IdEntidad).OnDelete(DeleteBehavior.Cascade);
    }
}
