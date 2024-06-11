using M2P.Results;

namespace MFGAllocation.Services
{
    public interface IAllocationPlanService
    {
		Task<Result<AllocationPlan>> CreateAsync(
            Guid siteId, Guid? companyId, Guid? buildingId, Guid? customerId, Guid? divisionId, Guid? areaId, 
            DateTime startDate, DateTime endDate,
            AllocationPlanType type, AllocationPlanEngine engine,
            IEnumerable<ProductionPlan> productionPlans);

		Task<Result> UpdateAsync(Guid id, Guid siteId, DateTime startDate, DateTime endDate,
            AllocationPlanType type, AllocationPlanEngine engine,
            IEnumerable<ProductionPlan> productionPlans, string reason, string user);

		Task<Result> DeleteAsync(Guid id, bool isActive);

        Task<Result> CloneAsync(Guid id);

        Task<Result> ValidateAsync(Guid id);

        Task<Result<ProductionPlan>> BuildAsync(string lineName, string productName, string layoutName, string shiftName, Guid siteId, int quantity, int velocity, Guid customerId);

		Task<Result<IEnumerable<ProductionPlan>>> AllocateEngineOneTeamAsync(DateTime startDate, IEnumerable<ProductionPlan> productionPlans);

        Task<Result<ProductionPlan>> AllocateProductionPlanAsync(DateTime startDate, ProductionPlan productionPlan);

        Task<GetRecommendedTeamResult?> GetRecommendedTeam(Guid productId, Guid shiftId, Guid layoutId, IEnumerable<Team> allocatedTeams);
        
        Task<Rise?> GetCandidateRise(TeamProduct teamProduct);

        Task<Rise?> GetCandidateRise(Guid TeamProductId);

        Task<Result<AllocationTeamResponse>> AllocateEngineTwoTeamAsync(IEnumerable<ProductionPlan> productionPlans);
    }

	public record AllocationTeamResponse(IEnumerable<ProductionPlan> ProductionPlans, IEnumerable<Error> Errors);

    public record GetRecommendedTeamResult(Team Team, TeamProduct TeamProduct, Rise? Rise);
}