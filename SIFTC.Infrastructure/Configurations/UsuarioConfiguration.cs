using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // Nombre de la tabla en PostgreSQL
        builder.ToTable("usuarios");

        // Llave primaria
        builder.HasKey(u => u.IdUsuario);

        // Mapeo basado en el "Diccionario 1.pdf"
        builder.Property(u => u.Email)
            .HasColumnName("usuario_del_sistema")
            .HasMaxLength(50) // Restricción del diccionario
            .IsRequired();

        // Evitar correos duplicados en la base de datos
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("contrasena_hash")
            .HasMaxLength(100) // Restricción del diccionario para Hash bcrypt
            .IsRequired();

        builder.Property(u => u.PrimerNombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.PrimerApellido)
            .HasMaxLength(50)
            .IsRequired();

        // Guardamos el Enum nativo de C# como un texto literal ("Analista", "Administrador") en BD
        builder.Property(u => u.Rol)
            .HasColumnName("rol")
            .HasMaxLength(20) // Restricción del diccionario[cite: 8]
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}