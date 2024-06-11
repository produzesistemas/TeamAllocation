
namespace MFGAllocation.Models
{
    public sealed class Workstation : Entity
    {
        public Workstation()
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; } = Guid.Empty;
        public Guid LayoutId { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;

        public Guid SiteId { get; set; } = Guid.Empty;
        public bool AllowMultiHC { get; set; } = false;
        public int CriticalityLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public IEnumerable<Guid> Trainings { get; set; } = Enumerable.Empty<Guid>();

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler
               .Set(ProductId)
               .WithValidation(x => x != Guid.Empty, "ProductId is required.");

            validationHandler
                .Set(LayoutId)
                .WithValidation(x => x != Guid.Empty, "LayoutId is required.");

            validationHandler
               .Set(CriticalityLevel)
               .WithValidation(x => x > -1, "CriticalityLevel is required.");

            validationHandler
               .Set(Name)
               .WithValidation(x => !string.IsNullOrEmpty(x), "Name is required.");
        }

        public static Workstation Create(
            string name,
            Guid productId,
            Guid layoutId,
            Guid siteId,
            bool allowMultiHC,
            int criticalityLevel,
            IEnumerable<Guid> trainings)
        {
            return new()
            {
                Name = name,
                AllowMultiHC = allowMultiHC,
                CriticalityLevel = criticalityLevel,
                ProductId = productId,
                LayoutId = layoutId,
                SiteId = siteId,
                Trainings = trainings
            };
        }

        public void Update(
            bool allowMultiHC,
            int criticalityLevel,
            IEnumerable<Guid> trainings)
        {
            AllowMultiHC = allowMultiHC;
            CriticalityLevel = criticalityLevel;
            Trainings = trainings;
        }

        public void Update(Workstation value)
        {
            AllowMultiHC = value.AllowMultiHC;
            CriticalityLevel = value.CriticalityLevel;
            Trainings = value.Trainings;
            SiteId = value.SiteId;
            IsActive = value.IsActive;
        }

        public void DeleteWorkstation(bool isActive)
        {
            IsActive = isActive;
        }

        public bool IdentityEquals(Workstation value)
        {
            return (LayoutId == value.LayoutId && ProductId == value.ProductId && Name == value.Name);
        }

        public bool ContainsIdentity(IEnumerable<Workstation> list)
        {
            foreach (var workstation in list)
            {
                if (IdentityEquals(workstation))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
