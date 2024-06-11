using System.ComponentModel.DataAnnotations.Schema;

namespace MFGAllocation.Models
{
    public class ProductionPlan : Entity
    {
        public ProductionPlan()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AllocationPlanId { get; set; } = Guid.Empty;
		public Guid ProductionLineId { get; set; } = Guid.Empty;
        public Guid ProductId { get; set; } = Guid.Empty;
        public Guid LayoutId { get; set; } = Guid.Empty;
        public Guid? RiseId { get; set; } = null;
        public Guid? ShiftId { get; set; } = null;
        public Guid? TeamId { get; set; } = null;
        public int Quantity { get; set; }
        public int Velocity { get; set; }
        public int Priority { get; set; }

        public string Alias { get; set; } = string.Empty;

        public AllocationPlan AllocationPlan { get; set; } = default!;
        public ProductionLine ProductionLine { get; set; } = default!;
        public Product Product { get; set; } = default!;
        public Layout Layout { get; set; } = default!;

        [NotMapped] public Rise Rise { get; set; } = default!;
        [NotMapped] public Shift Shift { get; set; } = default!;
        [NotMapped] public Team Team { get; set; } = default!;

        public IEnumerable<ProductionDay> ProductionDays { get; set; } = default!;

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(id=>Guid.Empty != id);
            validationHandler.Set(AllocationPlanId).WithValidation(id=>Guid.Empty != id);
            validationHandler.Set(ProductionLineId).WithValidation(id=>Guid.Empty != id);
            validationHandler.Set(ProductId).WithValidation(id=>Guid.Empty != id);
            validationHandler.Set(LayoutId).WithValidation(id=>Guid.Empty != id);
            validationHandler.Set(Priority).WithValidation(x => x >= 0);
        }

        public ProductionPlan Clone()
        {
            return new()
            {
                AllocationPlanId = this.AllocationPlanId,
                ProductionLineId = this.ProductionLineId,
                ProductId = this.ProductId,
                RiseId = this.RiseId,
                LayoutId = this.LayoutId,
                Quantity = this.Quantity,
                Velocity = this.Velocity,
                ShiftId = this.ShiftId,
                TeamId = this.TeamId,
                Priority = this.Priority,
                ProductionDays = ProductionDay.CloneList(this.ProductionDays)
            };
        }


        public static IEnumerable<ProductionPlan> CloneList(IEnumerable<ProductionPlan> list)
        {
            var objs = new List<ProductionPlan>();
            foreach (var item in list)
            {
                objs.Add(item.Clone());
            }
            return objs;
        }
    }
}
