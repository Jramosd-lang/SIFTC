using Microsoft.EntityFrameworkCore;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RolSistema> Roles => Set<RolSistema>();
    public DbSet<LoteCarga> LotesCarga => Set<LoteCarga>();
    public DbSet<MatrizFinancieraCruda> MatrizFinancieraCruda => Set<MatrizFinancieraCruda>();
    public DbSet<Sector> Sectores => Set<Sector>();
    public DbSet<Entidad> Entidades => Set<Entidad>();
    public DbSet<EntidadCotizacion> EntidadesCotizacion => Set<EntidadCotizacion>();
    public DbSet<DatoFinanciero> DatosFinancieros => Set<DatoFinanciero>();
    public DbSet<Dimension> Dimensiones => Set<Dimension>();
    public DbSet<Metrica> Metricas => Set<Metrica>();
    public DbSet<ValorMetrica> ValoresMetrica => Set<ValorMetrica>();
    public DbSet<IndiceCompetitividad> IndicesCompetitividad => Set<IndiceCompetitividad>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<EntidadSeguida> EntidadesSeguidas => Set<EntidadSeguida>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
