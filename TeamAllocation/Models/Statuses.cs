namespace MFGAllocation.Models
{
    public class Statuses : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SiteCode { get; set; } = string.Empty;
        public bool Active { get; set; }

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation(x => x != Guid.Empty, "Id is required.");
            validationHandler.Set(Name).WithValidation(x => !string.IsNullOrEmpty(x), "Name is required.");
            validationHandler.Set(SiteCode).WithValidation(x => !string.IsNullOrEmpty(x), "SiteCode is required.");
        }

        public static Statuses CreateStatuses(Guid id, string name, string siteCode, bool active)
        {
            return new()
            {
                Id = id,
                Name = name,
                SiteCode = siteCode,
                Active = active
            };
        }

        public void UpdateStatuses(string name, string siteCode)
        {
            Name = name;
            SiteCode = siteCode;
        }

        public void DeleteStatuses(bool active)
        {
            Active = active;
        }
    }
}
