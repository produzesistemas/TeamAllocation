using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFGAllocation.Models
{
    public class TeamWorkstationHC : Entity
    {
        public Guid TeamWorkstationId { get; set; }
        public Guid HCId { get; set; }
        public HC HC { get; set; } = default!;

        protected override void OnValidate(ValidationHandlerSet validationHandler)
        {
            validationHandler.Set(TeamWorkstationId).WithValidation(id => !Guid.Empty.Equals(id));
            validationHandler.Set(HCId).WithValidation(id => !Guid.Empty.Equals(id));
        }
    }
}
