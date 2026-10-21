using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios", "seguridad");
        builder.HasKey(u => u.IdUsuario);
        builder.Property(u => u.IdUsuario).HasColumnName("id_usuario").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(120)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(u => u.PrimerNombre).HasColumnName("primer_nombre").HasMaxLength(30).IsRequired();
        builder.Property(u => u.SegundoNombre).HasColumnName("segundo_nombre").HasMaxLength(30);
        builder.Property(u => u.PrimerApellido).HasColumnName("primer_apellido").HasMaxLength(30).IsRequired();
        builder.Property(u => u.SegundoApellido).HasColumnName("segundo_apellido").HasMaxLength(30);
        builder.Property(u => u.Estado).HasColumnName("estado").HasMaxLength(10).HasDefaultValue("activo").IsRequired();
        builder.Property(u => u.IdRol).HasColumnName("id_rol");
        builder.HasOne(u => u.Rol).WithMany(r => r.Usuarios).HasForeignKey(u => u.IdRol).OnDelete(DeleteBehavior.Restrict);
    }
}
