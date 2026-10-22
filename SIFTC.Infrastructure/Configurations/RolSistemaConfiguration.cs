using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class RolSistemaConfiguration : IEntityTypeConfiguration<RolSistema>
{
    public void Configure(EntityTypeBuilder<RolSistema> builder)
    {
        builder.ToTable("roles", "seguridad");
        builder.HasKey(r => r.IdRol);
        builder.Property(r => r.IdRol).HasColumnName("id_rol").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(30).IsRequired();
        builder.HasIndex(r => r.Nombre).IsUnique();
        builder.Property(r => r.Descripcion).HasColumnName("descripcion").HasMaxLength(150);
        builder.Property(r => r.PermisosClave).HasColumnName("permisos_clave").HasMaxLength(60).IsRequired();
    }
}
