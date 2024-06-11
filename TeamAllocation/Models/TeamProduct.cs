namespace MFGAllocation.Models
{
    public class TeamProduct : Entity
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public string Category { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public Guid LayoutId { get; set; }
        public Layout Layout { get; set; } = default!;
        public Product Product { get; set; } = default!;
        public IEnumerable<TeamWorkstation> TeamWorkstations { get; set; } = default!;

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(t => Guid.Empty != t);
        }
    }
}