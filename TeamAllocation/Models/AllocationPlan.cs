
namespace MFGAllocation.Models
{
    public sealed class AllocationPlan : Entity
    {
        private AllocationPlan() 
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SiteId { get; private set; } = Guid.Empty;
        public Guid? CompanyId { get; private set; }
        public Guid? BuildingId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public Guid? DivisionId { get; private set; }
        public Guid? AreaId { get; private set; }

        public int Number { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
		public DateTime CreationDate { get; set; } = DateTime.Now;
		public AllocationPlanStatus Status { get; set; } = default!;

        public AllocationPlanType Type { get; set; } = default!;

        public AllocationPlanEngine Engine { get; set; } = default!;

        public IEnumerable<ProductionPlan> ProductionPlans { get; set; } = default!;

		public bool IsActive { get; set; } = true;

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(id => !Guid.Empty.Equals(id));
            validationHandler.Set(SiteId).WithValidation(x => x != Guid.Empty);
        }

        public static AllocationPlan CreateEmpty(){
            return new(){}; 
        }
		public static AllocationPlan Create(
            Guid siteId, Guid? companyId, Guid? buildingId, Guid? customerId, Guid? divisionId, Guid? areaId,
            int number, DateTime startDate, DateTime endDate, 
            AllocationPlanType type, AllocationPlanEngine engine,
            IEnumerable<ProductionPlan> productionPlans)
		{
			return new()
			{
                SiteId = siteId,
                CompanyId = companyId == Guid.Empty ? null : companyId,
                BuildingId = buildingId == Guid.Empty ? null : buildingId,
                CustomerId = customerId == Guid.Empty ? null : customerId,
                DivisionId = divisionId == Guid.Empty ? null : divisionId,
                AreaId = areaId == Guid.Empty ? null : areaId,

                Number = number,
				StartDate = startDate,
				EndDate = endDate,
				Status = AllocationPlanStatus.Simulation(),
                Type = type,
                Engine = engine,
                ProductionPlans = productionPlans
            };
		}

		public void Update(Guid siteId, DateTime startDate, DateTime endDate,
            AllocationPlanType type, AllocationPlanEngine engine,
            IEnumerable<ProductionPlan> productionPlans)
		{
            SiteId = siteId;
            StartDate = startDate;
            EndDate = endDate;
            Type = type;
            Engine = engine;
            ProductionPlans = productionPlans;
		}

		public AllocationPlan Clone(int number)
        {
            return new()
            {
                Number = number,
                SiteId = this.SiteId,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                Status = AllocationPlanStatus.Simulation(),
                Type = AllocationPlanType.FromName(this.Type.Name),
                Engine = AllocationPlanEngine.FromName(this.Engine.Name),
				ProductionPlans = ProductionPlan.CloneList(this.ProductionPlans)
			};
        }

        public void UpdateStatusToValid()
        {
            Status = AllocationPlanStatus.Validated();
        }

        public void Delete(bool isActive)
        {
            IsActive = isActive;
        }

    }
}
