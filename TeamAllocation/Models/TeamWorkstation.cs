namespace MFGAllocation.Models
{
    public class TeamWorkstation : Entity
    {
        public Guid Id { get; set; } = Guid.Empty;
        public Guid TeamProductId { get; set; }
        public Guid WorkstationId { get; set; } = Guid.Empty;
        public Workstation Workstation { get; set; } = default!;
        //public TeamProduct TeamProduct { get; set; } = default!;

        public IEnumerable<TeamWorkstationHC> TeamWorkstationHCs { get; set; } = new List<TeamWorkstationHC>();

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(id => !Guid.Empty.Equals(id));
            validationHandler.Set(TeamProductId).WithValidation(id => !Guid.Empty.Equals(id));
            validationHandler.Set(WorkstationId).WithValidation(id => !Guid.Empty.Equals(id));
        }
    }
}