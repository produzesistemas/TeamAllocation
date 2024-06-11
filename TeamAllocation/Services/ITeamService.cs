using M2P.Results;

namespace MFGAllocation.Services
{
    public interface ITeamService
    {
        Task<Result<Team>> CreateAsync(
            Guid siteId, Guid? companyId, Guid? buildingId, Guid? customerId, Guid? divisionId, Guid? areaId, 
            string description, Guid responsibleId, Guid ShiftId, IEnumerable<TeamProduct> products);

        Task<Result> UpdateAsync(Guid id, string description,  Guid responsibleId, Guid ShiftId, IEnumerable<TeamProduct> products, string reason, string user);

        Task<Result> DeleteAsync(Guid id, bool isActive);
    }
}