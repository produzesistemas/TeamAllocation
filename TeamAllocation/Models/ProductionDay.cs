using System.ComponentModel.DataAnnotations.Schema;

namespace MFGAllocation.Models
{
    public class ProductionDay : Entity
    {
        public ProductionDay()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductionPlanId { get; set; } = Guid.Empty;
        public Guid? TeamId { get; set; } = null;
        public Guid? ShiftId { get; set; } = null;
        public Guid? RiseId { get; set; } = null;
        [NotMapped] public Team Team { get; set; } = default!;
        [NotMapped] public Shift Shift { get; set; } = default!;
        [NotMapped] public Rise Rise { get; set; } = default!;

        public DateTime Date { get; set; }
        public int Quantity { get; set; }

        public ProductionPlan ProductionPlan { get; set; } = default!;

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(id => Guid.Empty != id);
            //validationHandler.Set(ShiftId).WithValidation(id => Guid.Empty != id);
            validationHandler.Set(ProductionPlanId).WithValidation(id => Guid.Empty != id);
            validationHandler.Set(Quantity).WithValidation(x => x >= 0);
        }

        public ProductionDay Clone()
        {
            return new()
            {
                ProductionPlanId = this.ProductionPlanId,
                Date = this.Date,
                Quantity = this.Quantity,
                ShiftId = this.ShiftId,
                TeamId= this.TeamId,
                Team = this.Team,
                Shift = this.Shift,
                RiseId = this.RiseId,
            };
        }


        public static IEnumerable<ProductionDay> CloneList(IEnumerable<ProductionDay> list)
        {
            var objs = new List<ProductionDay>();
            foreach (var item in list)
            {
                objs.Add(item.Clone());
            }
            return objs;
        }
    }
}
