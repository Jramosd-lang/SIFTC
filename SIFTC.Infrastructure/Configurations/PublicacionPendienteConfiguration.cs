using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Configurations;

public sealed class PublicacionPendienteConfiguration : IEntityTypeConfiguration<PublicacionPendiente>
{
    public void Configure(EntityTypeBuilder<PublicacionPendiente> builder)
    {
        builder.ToView("v_publicaciones_pendientes", "analitica");
        builder.HasKey(p => p.IdLote);
        builder.Property(p => p.IdLote).HasColumnName("id_lote");
        builder.Property(p => p.NombreArchivo).HasColumnName("nombre_archivo").HasMaxLength(150);
        builder.Property(p => p.FechaCarga).HasColumnName("fecha_carga");
        builder.Property(p => p.RegistrosTotales).HasColumnName("registros_totales");
        builder.Property(p => p.RegistrosValidos).HasColumnName("registros_validos");
        builder.Property(p => p.EstadoCarga).HasColumnName("estado_carga").HasMaxLength(32);
        builder.Property(p => p.Estado).HasColumnName("estado").HasMaxLength(10);
        builder.Property(p => p.IdUsuarioEjecutor).HasColumnName("id_usuario_ejecutor");
        builder.Property(p => p.IdUsuarioDecision).HasColumnName("id_usuario_decision");
        builder.Property(p => p.FechaDecision).HasColumnName("fecha_decision");
        builder.Property(p => p.Comentario).HasColumnName("comentario").HasMaxLength(500);
    }
}
