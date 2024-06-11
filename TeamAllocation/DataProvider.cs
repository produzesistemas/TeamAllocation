using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MFGAllocation
{
    public interface DataProvider : IDataProvider
    {
        IQueryable<Team> Teams => Query<Team>();
        IQueryable<TeamProduct> TeamProducts => Query<TeamProduct>();
        IQueryable<TeamWorkstation> TeamWorkstations => Query<TeamWorkstation>();
        IQueryable<TeamWorkstationHC> TeamWorkstationHCs => Query<TeamWorkstationHC>();
        IQueryable<Company> Companies => Query<Company>();
        IQueryable<Customer> Customers => Query<Customer>();
        IQueryable<HC> HCs => Query<HC>();
        IQueryable<HCTrainning> HCTrainnings => Query<HCTrainning>();
        IQueryable<Building> Buildings => Query<Building>();
        IQueryable<Division> Divisions => Query<Division>();
        IQueryable<Area> Areas => Query<Area>();
        IQueryable<Product> Products => Query<Product>();
        IQueryable<Site> Sites => Query<Site>();
        IQueryable<Shift> Shifts => Query<Shift>();
        IQueryable<Statuses> Statuses => Query<Statuses>();
        IQueryable<Rise> Rises => Query<Rise>();
        IQueryable<RiseParam> RiseParams => Query<RiseParam>();
        IQueryable<AllocationPlan> AllocationPlans => Query<AllocationPlan>();
		IQueryable<ProductionPlan> ProductionPlans => Query<ProductionPlan>();
        IQueryable<ProductionDay> ProductionDays => Query<ProductionDay>();
        IQueryable<ProductionLine> ProductionLines => Query<ProductionLine>();
        IQueryable<Workstation> Workstations => Query<Workstation>();
        IQueryable<Layout> Layouts => Query<Layout>();

        IQueryable<HistoricChange> HistoricChanges => Query<HistoricChange>();
    }

    public interface IMFGAllocationDbContext : DataProvider, IDataHandler
    {
        DatabaseFacade GetDatabase();

        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : Entity;
    }

    internal class MFGAllocationDbContext : DataContext, IMFGAllocationDbContext
    {
        public MFGAllocationDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MFGAllocationDbContext).Assembly);
        }

        EntityEntry<TEntity> IMFGAllocationDbContext.Entry<TEntity>(TEntity entity) => base.Entry(entity);

        public DatabaseFacade GetDatabase()
        {
            return Database;
        }
    }
}