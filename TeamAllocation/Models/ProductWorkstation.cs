using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFGAllocation.Models
{
    public class ProductWorkstation : Entity
    {
        public Guid Id { get; set; } = Guid.Empty;
        public Guid ProductId { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public bool AllowMultiHc { get; set; } = false;
        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set<Guid>(Id).WithValidation(v => v != Guid.Empty);
            validationHandler.Set<Guid>(ProductId).WithValidation(v => v != Guid.Empty);
            validationHandler.Set<string>(Name).WithValidation(v =>string.IsNullOrEmpty(v));
        }
    }
}
