namespace MFGAllocation.Models
{
    public class HCTrainning : Entity
    {
        public Guid HCId { get; set; } = Guid.Empty;
        public Guid WorkstationId { get; set; } = Guid.Empty;
        public Guid LayoutId { get; set; } = Guid.Empty;
        public Workstation Workstation { get; set; } = default!;
        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler
                .Set(HCId)
                .WithValidation(x => x != Guid.Empty, "HCId is required.");
            validationHandler
                .Set(WorkstationId)
                .WithValidation(x => x != Guid.Empty, "WorkstationId is required.");
            validationHandler
                .Set(LayoutId)
                .WithValidation(x => x != Guid.Empty, "LayoutId is required.");
        }

        public static HCTrainning CreateHCTrainning(Guid hcId, Guid workstationId, Guid layoutId)
        {
            return new()
            {
                 HCId = hcId,
                 WorkstationId = workstationId,
                 LayoutId = layoutId
            };
        }

        public void UpdateHCTrainning(Guid workstationId, Guid layoutId)
        {
            WorkstationId = workstationId;
            LayoutId = layoutId;
        }

        public bool Equals(HCTrainning obj)
        {
            return HCId == obj.HCId && WorkstationId == obj.WorkstationId && LayoutId == obj.LayoutId;
        }
    }

}
