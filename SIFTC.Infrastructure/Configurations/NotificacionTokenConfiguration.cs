using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class NotificacionTokenConfiguration : IEntityTypeConfiguration<NotificacionToken>
{
    public void Configure(EntityTypeBuilder<NotificacionToken> builder)
    {
        builder.ToTable("notificaciones_tokens", "seguridad");
        builder.HasKey(t => t.IdToken);
        builder.Property(t => t.IdToken).HasColumnName("id_token").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.IdUsuario).HasColumnName("id_usuario").IsRequired();
        builder.Property(t => t.Plataforma).HasColumnName("plataforma").HasMaxLength(10).IsRequired();
        builder.Property(t => t.TokenFcm).HasColumnName("token_fcm").IsRequired();
        builder.Property(t => t.Activo).HasColumnName("activo").IsRequired();
        builder.Property(t => t.FechaRegistro).HasColumnName("fecha_registro").HasDefaultValueSql("now()");
        builder.Property(t => t.UltimoUso).HasColumnName("ultimo_uso");

        builder.HasIndex(t => t.TokenFcm).IsUnique().HasDatabaseName("uq_notificaciones_tokens");
        builder.HasIndex(t => t.IdUsuario).HasDatabaseName("ix_notificaciones_tokens_activos");
    }
}
