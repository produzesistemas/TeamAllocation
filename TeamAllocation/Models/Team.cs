namespace MFGAllocation.Models
{
    public sealed class Team : Entity
    {
        public Team()
        {
        }

        public Guid Id { get; private init; } = Guid.NewGuid();

        public Guid SiteId { get; private set; } = Guid.Empty;
        public Guid? CompanyId { get; private set; }
        public Guid? BuildingId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public Guid? DivisionId { get; private set; }
        public Guid? AreaId { get; private set; }

        public string Description { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;
        public Guid ShiftId { get; set; } = Guid.Empty;
		public Shift Shift { get; set; } = default!;
		public Guid ResponsibleId { get; private set; } = Guid.Empty;
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public IEnumerable<TeamProduct> TeamProducts { get; set; } = default!;

        public static Team CreateTeam(
            Guid siteId, Guid? companyId, Guid? buildingId, Guid? customerId, Guid? divisionId, Guid? areaId,
            string description, Guid responsibleId, Guid shiftId, IEnumerable<TeamProduct> products)
        {
            var teamId = Guid.NewGuid();
            foreach (var product in products)
            {
                product.TeamId = teamId;
            }
            return new()
            {
                SiteId = siteId,
                CompanyId = companyId == Guid.Empty ? null : companyId,
                BuildingId = buildingId == Guid.Empty ? null : buildingId,
                CustomerId = customerId == Guid.Empty ? null : customerId,
                DivisionId = divisionId == Guid.Empty ? null : divisionId,
                AreaId = areaId == Guid.Empty ? null : areaId,

                Id = teamId,
                Description = description,
                ResponsibleId = responsibleId,
                TeamProducts = products,
                ShiftId = shiftId
            };
        }

        public void UpdateTeam(string description, Guid responsibleId, Guid shiftId, IEnumerable<TeamProduct> products)
        {
            Description = description;
            ResponsibleId = responsibleId;
            TeamProducts = products;
            ShiftId = shiftId;
        }

        public void DeleteTeam(bool isActive)
        {
            IsActive = isActive;
        }

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(id => !Guid.Empty.Equals(id));
            validationHandler.Set(Description).WithValidation(x => x != string.Empty);
            validationHandler.Set(ResponsibleId).WithValidation(x => x != Guid.Empty);
        }
    }
}