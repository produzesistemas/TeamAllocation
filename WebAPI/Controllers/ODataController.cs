using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("odata")]
    [ODataAttributeRouting]
    public sealed class ODataController : ControllerBase
    {
        [EnableQuery]
        [HttpGet("hcs")]
        [ProducesResponseType(typeof(ODataValue<HC>), 200)]
        public IQueryable<HC> GetHCs([FromServices] DataProvider dataProvider)
        {
            return dataProvider.HCs
                .Where(x => x.SiteId == flexSite)
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("hcTrainnings")]
        [ProducesResponseType(typeof(ODataValue<HCTrainning>), 200)]
        public IQueryable<HCTrainning> GetHCTrainnings([FromServices] DataProvider dataProvider)
        {
            return dataProvider.HCTrainnings
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("shifts")]
        [ProducesResponseType(typeof(ODataValue<Shift>), 200)]
        public IQueryable<Shift> GetShifts(
            [FromServices] DataProvider dataProvider)
        {
            return dataProvider.Shifts
                .Where(x => x.SiteId == flexSite)
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery(MaxExpansionDepth = 6)]
        [HttpGet("teams")]
        [ProducesResponseType(typeof(ODataValue<Team>), 200)]
        public IQueryable<Team> GetTeamsAsync(
            [FromServices] DataProvider dataProvider
            [FromHeader(Name = "include-inactives")] bool includeInactives)
        {
            return dataProvider.Teams
                .Where(
                    && (x.IsActive || includeInactives)
                )
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery(MaxExpansionDepth = 5)]
        [HttpGet("teamProducts")]
		[ProducesResponseType(typeof(ODataValue<TeamProduct>), 200)]
		public IQueryable<TeamProduct> GetTeamProducts([FromServices] DataProvider dataProvider)
		{
			return dataProvider.TeamProducts.IgnoreAutoIncludes().AsNoTrackingWithIdentityResolution();
		}

        [EnableQuery(MaxExpansionDepth = 5)]
        [HttpGet("teamWorkstations")]
        [ProducesResponseType(typeof(ODataValue<TeamWorkstation>), 200)]
        public IQueryable<TeamWorkstation> GetTeamWorkstations([FromServices] DataProvider dataProvider)
        {
            return dataProvider.TeamWorkstations.IgnoreAutoIncludes().AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("rises")]
        [ProducesResponseType(typeof(ODataValue<Rise>), 200)]
        public IQueryable<Rise> GetRises(
            [FromServices] DataProvider dataProvider
            [FromHeader(Name = "include-inactives")] bool includeInactives)
        {
            return dataProvider.Rises
                .Where(x => 
                    && (x.IsActive || includeInactives)
                )
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("sites")]
        [ProducesResponseType(typeof(ODataValue<Site>), 200)]
        public IQueryable<Site> GetSites([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Sites
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("companies")]
        [ProducesResponseType(typeof(ODataValue<Company>), 200)]
        public IQueryable<Company> GetCompanies([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Companies
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("customers")]
        [ProducesResponseType(typeof(ODataValue<Customer>), 200)]
        public IQueryable<Customer> GetCustomers([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Customers
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("buildings")]
        [ProducesResponseType(typeof(ODataValue<Building>), 200)]
        public IQueryable<Building> GetBuildings([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Buildings
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("divisions")]
        [ProducesResponseType(typeof(ODataValue<Division>), 200)]
        public IQueryable<Division> GetDivisions([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Divisions
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("areas")]
        [ProducesResponseType(typeof(ODataValue<Area>), 200)]
        public IQueryable<Area> GetAreas([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Areas
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("products")]
        [ProducesResponseType(typeof(ODataValue<Product>), 200)]
        public IQueryable<Product> GetProducts(
            [FromServices] DataProvider dataProvider
            [FromHeader(Name = "include-without-workstations")] bool includeWithoutWorkstations
            )
        {
            if (includeWithoutWorkstations)
            {
                return dataProvider.Products
               .IgnoreAutoIncludes()
               .AsNoTrackingWithIdentityResolution();
            }

            var ids = dataProvider.Products
                .Include(x => x.Workstations)
                .Where(x => x.Workstations.Any())
                .Select(x => x.Id);

            return dataProvider.Products
              .Where(
                   (ids.Any(y => y == x.Id))
              )
              .IgnoreAutoIncludes()
              .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("allocationPlans")]
        [ProducesResponseType(typeof(ODataValue<AllocationPlan>), 200)]
        public IQueryable<AllocationPlan> GetAllocationPlans(
            [FromServices] DataProvider dataProvider)
        {
            return dataProvider.AllocationPlans
                .Where(x =>  x.IsActive == true)
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery(MaxExpansionDepth = 5)]
        [HttpGet("productionPlans")]
        [ProducesResponseType(typeof(ODataValue<ProductionPlan>), 200)]
        public IQueryable<ProductionPlan> GetProductionPlans(
        [FromServices] DataProvider dataProvider)
        {
            return dataProvider.ProductionPlans
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("productionDays")]
        [ProducesResponseType(typeof(ODataValue<ProductionDay>), 200)]
        public IQueryable<ProductionDay> GetProductionDays(
        [FromServices] DataProvider dataProvider)
        {
            return dataProvider.ProductionDays
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("workstations")]
        [ProducesResponseType(typeof(ODataValue<Workstation>), 200)]
        public IQueryable<Workstation> GetWorkstations(
        [FromServices] DataProvider dataProvider)
        {
            return dataProvider.Workstations
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("layouts")]
        [ProducesResponseType(typeof(ODataValue<Layout>), 200)]
        public IQueryable<Layout> GetLayouts([FromServices] DataProvider dataProvider)
        {
            return dataProvider.Layouts
                .IgnoreAutoIncludes()
                .Where(x => x.IsActive)
                .AsNoTrackingWithIdentityResolution();
        }

        [EnableQuery]
        [HttpGet("historicChanges")]
        [ProducesResponseType(typeof(ODataValue<HistoricChange>), 200)]
        public IQueryable<HistoricChange> GetHistoricChanges([FromServices] DataProvider dataProvider)
        {
            return dataProvider.HistoricChanges
                .IgnoreAutoIncludes()
                .AsNoTrackingWithIdentityResolution();
        }
    }
}