
namespace MFGAllocation.Models
{
    public sealed class AllocationPlanEngine
    {
        private AllocationPlanEngine() 
        {
        }

        public string Name { get; set; } = string.Empty;

        #region CONSTS
        private const string ENGINE_ONE = "ONE";
        private const string ENGINE_TWO = "TWO";
        #endregion CONSTS

        #region CTORS
        public static AllocationPlanEngine FromName(string name)
        {
            return new AllocationPlanEngine()
            {
                Name = name,
            };
        }

        public static AllocationPlanEngine One()
        {
            return new AllocationPlanEngine()
            {
                Name = ENGINE_ONE,
            };
        }
        public static AllocationPlanEngine Two()
        {
            return new AllocationPlanEngine()
            {
                Name = ENGINE_TWO,
            };
        }
        #endregion CTORS

        #region VALIDATORS
        public bool IsEngineOne() => Name == ENGINE_ONE;
        public bool IsEngineTwo() => Name == ENGINE_TWO;
        #endregion VALIDATORS
    }
}
