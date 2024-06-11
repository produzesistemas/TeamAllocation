using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFGAllocation.Models
{
    public class HistoricChange : Entity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Model { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public string User { get; set; } = string.Empty;

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(Id).WithValidation((id) => id != Guid.Empty);
            validationHandler.Set(User).WithValidation((id) => !string.IsNullOrEmpty(id));
            validationHandler.Set(Model).WithValidation((model) => !string.IsNullOrEmpty(model));
            validationHandler.Set(ModelId).WithValidation((modelId) => !string.IsNullOrEmpty(modelId));
        }
    }
}
