
namespace MFGAllocation.Models
{
    public class HC : Entity
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public string Workday { get; set; } = string.Empty;
        public string SiteCode { get; set; } = string.Empty;
        public Guid ShiftId { get; set; } = Guid.Empty;
        public Guid SiteId { get; set; } = Guid.Empty;
        public Guid StatusesId { get; set; } = Guid.Empty;
        public Shift Shift { get; set; } = default!;
        public Site Site { get; set; } = default!;
        public Statuses Statuses { get; set; } = default!;

        public IEnumerable<HCTrainning> HCTrainnings { get; set; } = new List<HCTrainning>();

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(ShiftId).WithValidation(x => x != Guid.Empty, "Shift is required.");
            validationHandler.Set(SiteId).WithValidation(x => x != Guid.Empty, "Site is required.");
            validationHandler.Set(StatusesId).WithValidation(x => x != Guid.Empty, "Status is required.");
            validationHandler.Set(Workday).WithValidation(v => !string.Empty.Equals(v), "Workday is required");
            validationHandler.Set(Name).WithValidation(x => !string.IsNullOrEmpty(x), "Name is required.");
            validationHandler.Set(SiteCode).WithValidation(x => !string.IsNullOrEmpty(x), "SiteCode is required.");
        }

        public static HC CreateHC(
            Guid id,
            string name,
            string workday,
            string siteCode,
            Guid shiftId,
            Guid siteId,
            Guid statusId)
        {
            return new()
            {
                Id = id,
                Name = name,
                Workday = workday,
                SiteCode = siteCode,
                 ShiftId = shiftId,
                 SiteId = siteId,
                StatusesId = statusId
            };
        }

        public void UpdateHC(
            string name,
            string workday,
            string siteCode,
            Guid shiftId,
            Guid siteId,
            Guid statusId)
        {
            Name = name;
            Workday = workday;
            ShiftId = shiftId;
            SiteId = siteId;
            SiteCode = siteCode;
            StatusesId = statusId;
        }

    }

}
