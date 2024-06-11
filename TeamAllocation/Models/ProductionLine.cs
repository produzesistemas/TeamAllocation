
namespace MFGAllocation.Models
{
    public sealed class ProductionLine : Entity
    {
        public ProductionLine()
        {
        }

        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid SiteId { get; set; } = Guid.Empty;
        public bool IsActive { get; set; } = true;
        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler
               .Set(SiteId)
               .WithValidation(x => x != Guid.Empty, "Site is required.");

            validationHandler
               .Set(Name)
               .WithValidation(x => !string.IsNullOrEmpty(x), "Name is required.");
        }

        public ProductionLine Clone()
        {
            return new ProductionLine()
            {
                Id = this.Id,
                Name = this.Name,
                SiteId = this.SiteId,
                IsActive = this.IsActive,
            };
        }
        public static ProductionLine CreateProductionLine(
            Guid id,
            Guid siteId,
            string name,
            bool active)
        {
            return new()
            {
                Id = id,
                Name = name,
                SiteId = siteId,
                IsActive = active
            };
        }

        public void UpdateProductionLine(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }

        public void DeleteProductionLine(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
