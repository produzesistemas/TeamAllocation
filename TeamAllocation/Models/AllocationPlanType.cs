
namespace MFGAllocation.Models
{
    public sealed class AllocationPlanType
    {
        private AllocationPlanType() 
        {
        }

        public string Name { get; set; } = string.Empty;

        #region CONSTS
        private const string TYPE_BOX = "BOX";
        private const string TYPE_PCB = "PCB";
        #endregion CONSTS

        #region CTORS
        public static AllocationPlanType FromName(string name)
        {
            return new AllocationPlanType()
            {
                Name = name,
            };
        }

        public static AllocationPlanType Box()
        {
            return new AllocationPlanType()
            {
                Name = TYPE_BOX,
            };
        }
        public static AllocationPlanType PCB()
        {
            return new AllocationPlanType()
            {
                Name = TYPE_PCB,
            };
        }
        public static IEnumerable<AllocationPlanType> All()
        {
            var all = new List<AllocationPlanType>
        {
            Box(),
            PCB(),
        };
            return all;
        }
        #endregion CTORS

        #region VALIDATORS
        public bool IsBox() => Name == TYPE_BOX;
        public bool IsPCB() => Name == TYPE_PCB;
        #endregion VALIDATORS
    }
}
