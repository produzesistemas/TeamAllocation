using M2P.Results;

namespace MFGAllocation.Services.Impl
{
    internal class TeamService : ITeamService
    {
        private IMFGAllocationDbContext database;

        public TeamService(IMFGAllocationDbContext database)
        {
            this.database = database;
        }

        public async Task<Result<Team>> CreateAsync(
            Guid siteId, Guid? companyId, Guid? buildingId, Guid? customerId, Guid? divisionId, Guid? areaId, 
            string description, Guid responsibleId, Guid shiftId, IEnumerable<TeamProduct> products)
        {
            if (await database.Teams.AnyAsync(x => x.IsActive && x.Description.Trim().ToUpper() == description.Trim().ToUpper()))
            {
                return Results.TeamAlreadyExists("Team");
            }

            Team request = Team.CreateTeam(
                siteId, companyId, buildingId, customerId, divisionId, areaId, 
                description, responsibleId, shiftId, products);

            if (!request.TryValidate(out Result result))
            {
                return result;
            }

            await database.AddAsync(request);
            await database.SaveChangesAsync();

            return Result.Ok(request);
        }

        public async Task<Result> UpdateAsync(Guid id, string description, Guid responsibleId, Guid shiftId, IEnumerable<TeamProduct> products, string reason, string user)
        {
            Team? team = await database.Teams.FirstOrDefaultAsync(x => x.IsActive && x.Id == id);
            if (team is null)
            {
                return Results.TeamNotFound("Team");
            }

            if (await database.Teams.AnyAsync(x => x.IsActive && x.Description.ToUpper() == description.ToUpper() && x.Id != id))
            {
                return Results.TeamAlreadyExists("Team");
            }

            List<TeamProduct>? teamProducts = await database.TeamProducts
                .Where(x => x.TeamId == team.Id).Include(x => x.TeamWorkstations).ThenInclude(x => x.TeamWorkstationHCs)
                .ToListAsync();

            var teaworkstations = teamProducts.SelectMany(x => x.TeamWorkstations).ToList();
            var teaworkstationHcs = teaworkstations.SelectMany(c => c.TeamWorkstationHCs).ToList();

            teaworkstationHcs.ForEach(twh =>
            {
                database.Remove(twh);
            });

            teaworkstations.ForEach(tw =>
            {
                database.Remove(tw);
            });

            teamProducts.ForEach(tp =>
            {
                database.Remove(tp);
            });
            products.ToList().ForEach(async x =>
            {
                x.TeamId = id;
                await database.AddAsync(x);
            });
            team.UpdateTeam(description, responsibleId, shiftId, products);
            if (!team.TryValidate(out Result result))
            {
                return result;
            }
            await database.AddAsync(new HistoricChange() { Date = DateTime.UtcNow, Model = team.GetType().Name, ModelId = team.Id.ToString(), Reason = reason, User = user });
            await database.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(Guid id, bool isActive)
        {
            var team = await database.Teams
                .Include(x => x.TeamProducts)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (team is null)
            {
                return Results.TeamNotFound("Team");
            }

            team.DeleteTeam(isActive);
            if (!team.TryValidate(out Result result))
            {
                return result;
            }

            await database.SaveChangesAsync();
            return Result.Ok();
        }
    }
}