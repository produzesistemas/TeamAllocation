
namespace MFGAllocation.Models
{
    public sealed class AllocationPlanStatus
    {
        private AllocationPlanStatus() 
        {
        }  

        public string Name { get; set; } = string.Empty;

        #region CONSTS
        private const string STATUS_SIMULATION = "SIMULATION";
        private const string STATUS_VALIDATED = "VALIDATED";
        #endregion CONSTS

        #region CTORS
        public static AllocationPlanStatus Simulation()
        {
            return new AllocationPlanStatus()
            {
                Name = STATUS_SIMULATION,
            };
        }
        public static AllocationPlanStatus Validated()
        {
            return new AllocationPlanStatus()
            {
                Name = STATUS_VALIDATED,
            };
        }
        public static IEnumerable<AllocationPlanStatus> All()
        {
            var all = new List<AllocationPlanStatus>
        {
            Simulation(),
            Validated(),
        };
            return all;
        }
        #endregion CTORS

        #region VALIDATORS
        public bool IsSimulation() => Name == STATUS_SIMULATION;
        public bool IsValid() => Name == STATUS_VALIDATED;
        #endregion VALIDATORS
    }
}
