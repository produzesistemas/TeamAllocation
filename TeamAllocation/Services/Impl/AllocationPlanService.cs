using M2P.Results;
using MFGAllocation.Extensions;
using Newtonsoft.Json;

namespace MFGAllocation.Services.Impl
{
    internal class AllocationPlanService : IAllocationPlanService
    {
        private IMFGAllocationDbContext database;

        public AllocationPlanService(IMFGAllocationDbContext database)
        {
            this.database = database;
        }

        public async Task<Result<AllocationPlan>> CreateAsync(
            Guid siteId, Guid? companyId, Guid? buildingId, Guid? customerId, Guid? divisionId, Guid? areaId,
            DateTime startDate, DateTime endDate,
            AllocationPlanType type, AllocationPlanEngine engine,
            IEnumerable<ProductionPlan> productionPlans)
        {
            try
            {
                int number = await database.AllocationPlans.CountAsync() + 1;

                AllocationPlan request = AllocationPlan.Create(
                    siteId, companyId, buildingId, customerId, divisionId, areaId,
                    number, startDate,
                    endDate, type, engine, productionPlans);

                if (!request.TryValidate(out Result result))
                {
                    return result;
                }

                await database.AddAsync(request);
                await database.SaveChangesAsync();

                return Result.Ok(request);
            }
            catch (Exception ex)
            {
                return Result.Fail("AllocationPlan", ex.Message);
            }

        }

        public async Task<Result> UpdateAsync(Guid id, Guid siteId,
            DateTime startDate, DateTime endDate,
            AllocationPlanType type, AllocationPlanEngine engine,
            IEnumerable<ProductionPlan> productionPlans, string reason, string user)
        {
            AllocationPlan? ap = await database.AllocationPlans.FirstOrDefaultAsync(x => x.Id == id);
            if (ap is null)
            {
                return Results.AllocationPlanNotFound();
            }
            var previous = JsonConvert.SerializeObject(ap);
            List<ProductionDay>? pds = await database.ProductionDays
                .Include(x => x.ProductionPlan)
                .Where(x => x.ProductionPlan.AllocationPlanId == ap.Id).ToListAsync();

            List<ProductionPlan>? pps = await database.ProductionPlans
                .Where(x => x.AllocationPlanId == ap.Id).ToListAsync();


            pds.ForEach(x =>
            {
                database.Remove(x);
            });
            pps.ForEach(x =>
            {
                database.Remove(x);
            });

            productionPlans.ToList().ForEach(async x =>
            {
                x.AllocationPlanId = id;
                x.ProductionDays = ProductionDay.CloneList(x.ProductionDays);
                await database.AddAsync(x);
            });

            ap.Update(siteId, startDate, endDate, type, engine, productionPlans);
            if (!ap.TryValidate(out Result result))
            {
                return result;
            }
            var current = JsonConvert.SerializeObject(ap, Formatting.Indented,
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            });

            await this.database.AddAsync(new HistoricChange()
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                Reason = reason,
                ModelId = ap.Id.ToString(),
                Model = ap.GetType().Name,
                User = user
            });
            await database.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> CloneAsync(Guid id)
        {
            AllocationPlan? ap = await database.AllocationPlans
                .Include(x => x.Type)
                .Include(x => x.ProductionPlans).ThenInclude(x => x.ProductionDays)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ap is null)
            {
                return Results.AllocationPlanNotFound();
            }

            int number = await database.AllocationPlans.CountAsync() + 1;
            AllocationPlan clone = ap.Clone(number);
            if (!clone.TryValidate(out Result result))
            {
                return result;
            }

            await database.AddAsync(clone);
            await database.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(Guid id, bool isActive)
        {
            AllocationPlan? ap = await database.AllocationPlans
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ap is null)
            {
                return Results.AllocationPlanNotFound();
            }

            ap.Delete(isActive);
            if (!ap.TryValidate(out Result result))
            {
                return result;
            }

            await database.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> ValidateAsync(Guid id)
        {
            AllocationPlan? ap = await database.AllocationPlans
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ap is null)
            {
                return Results.AllocationPlanNotFound();
            }
            if (ap.Status.IsValid())
            {
                return Results.AllocationPlanAlreadyValid();
            }

            ap.UpdateStatusToValid();
            if (!ap.TryValidate(out Result result))
            {
                return result;
            }

            await database.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result<ProductionPlan>> BuildAsync(string lineName, string productName, string layoutName, string shiftName, Guid siteId, int quantity, int velocity, Guid customerId)
        {
            var errors = new Stack<Error>();
            var productionPlan = new ProductionPlan
            {
                Quantity = quantity,
                Velocity = velocity,
            };

            var productExists = await database.Products
                .FirstOrDefaultAsync(p => p.Name.ToUpper() == productName.ToUpper()
                && (customerId == Guid.Empty || p.CustomerId == customerId));
            if (productExists == null)
            {
                errors.Push(new Error("ProductNotFoundByName", productName));
                return Result.Fail(errors.ToList());
            }

            var productionLineExists = await database.ProductionLines
                .FirstOrDefaultAsync(x => x.Name.ToUpper() == lineName.ToUpper());
            if (productionLineExists == null)
            {
                errors.Push(new Error("ProductionLineNotFoundByName", lineName));
                return Result.Fail(errors.ToList());
            }

            var layout = await database
                .Layouts
                .Include(x => x.Product)
                .Include(x => x.ProductionLine)
                .FirstOrDefaultAsync(
                    x => x.Name.ToUpper() == layoutName.ToUpper()
                    && x.Product.Name.ToUpper() == productName.ToUpper()
                    && x.ProductionLine.Name.ToUpper() == lineName.ToUpper());
            if (layout == null)
            {
                errors.Push(new Error("LayoutNotFoundByProductAndLine", string.Concat(layoutName, " / ", productName, " / ", lineName)));
                return Result.Fail(errors.ToList());
            }

            var shift = await database.Shifts.FirstOrDefaultAsync(x => x.Name.ToUpper() == shiftName.ToUpper() && x.SiteId == siteId);
            if (shift == null)
            {
                errors.Push(new Error("ShiftNotFoundByName", shiftName));
                return Result.Fail(errors.ToList());
            }

            productionPlan.ProductId = layout.Product.Id;
            productionPlan.Product = layout.Product;
            productionPlan.Layout = layout;
            productionPlan.ProductionLine = layout.ProductionLine;
            productionPlan.ProductionLineId = layout.ProductionLine.Id;
            productionPlan.ShiftId = shift.Id;
            productionPlan.Shift = shift;

            if (errors.Any())
            {
                return Result.Fail(errors.ToList());
            }

            return Result.Ok(productionPlan);
        }

        public async Task<Result<IEnumerable<ProductionPlan>>> AllocateEngineOneTeamAsync(DateTime startDate, IEnumerable<ProductionPlan> productionPlans)
        {
            List<Team> allocatedTeams = new List<Team>();
            List<ProductionPlan> planned = new List<ProductionPlan>();
            foreach (var productionPlan in productionPlans)
            {
                GetRecommendedTeamResult? recommended = await GetRecommendedTeam(productionPlan.ProductId,
                    productionPlan.ShiftId!.Value, productionPlan.Layout.Id, allocatedTeams);

                if (recommended == null)
                {
                    planned.Add(productionPlan);
                }
                else
                {
                    productionPlan.TeamId = recommended.Team.Id;
                    productionPlan.Team = recommended.Team;
                    allocatedTeams.Add(recommended.Team);

                    productionPlan.CalculateDerate(startDate, recommended.Rise);

                    planned.Add(productionPlan);
                }
            }
            return Result.Ok((IEnumerable<ProductionPlan>)planned);
        }

        public async Task<Result<AllocationTeamResponse>> AllocateEngineTwoTeamAsync(IEnumerable<ProductionPlan> productionPlans)
        {
            var allocatedTeams = new List<DateTimeTeam>();
            var errors = new List<Error>();
            foreach (var productionPlan in productionPlans.OrderByDescending(p => p.Priority))
            {
                GetRecommendedTeamResult? recommended = null;
                foreach (var productionDay in productionPlan.ProductionDays)
                {
                    if (productionDay.Quantity == 0)
                    {
                        continue;
                    }

                    if (recommended is not null && recommended.Team.ShiftId == productionDay.ShiftId)
                    {
                        productionDay.TeamId = recommended.Team.Id;
                        productionDay.Team = recommended.Team;
                        productionDay.RiseId = recommended.Rise?.Id;
                        productionDay.Rise = recommended.Rise != null ? recommended.Rise : default!;
                        allocatedTeams.Add(new(productionDay.Date, recommended.Team));
                    }
                    else
                    {
                        var filteredAllocatedTeams = allocatedTeams
                            .Where(x => x.Team.ShiftId == productionDay.ShiftId && x.Date == productionDay.Date)
                            .Select(x => x.Team);

                        recommended = await GetRecommendedTeam(productionPlan.ProductId,
                            productionDay.ShiftId!.Value, productionPlan.LayoutId, filteredAllocatedTeams);

                        if (recommended is not null)
                        {
                            productionDay.TeamId = recommended.Team.Id;
                            productionDay.Team = recommended.Team;
                            productionDay.RiseId = recommended.Rise?.Id;
                            productionDay.Rise = recommended.Rise != null ? recommended.Rise : default!;
                            allocatedTeams.Add(new(productionDay.Date, recommended.Team));
                            continue;
                        }
                        else
                        {
                            errors.Add(new Error("TeamAllocationNotFound", string.Format("{0}/{1}/{2}", productionPlan.ProductionLine.Name, productionPlan.Layout.Name, productionPlan.Product.Name)));
                        }
                    }
                }
            }
            return Result.Ok(new AllocationTeamResponse(
                productionPlans
                .OrderByDescending(x => x.Priority)
                    .ToList(), errors)
                );
            //return Result.Ok(productionPlans, productionPlans.Count());
        }

        public async Task<Result<ProductionPlan>> AllocateProductionPlanAsync(DateTime startDate, ProductionPlan productionPlan)
        {
            var teamProduct = await database.TeamProducts
                .Include(x => x.TeamWorkstations)
                .ThenInclude(x => x.TeamWorkstationHCs)
                .ThenInclude(x => x.HC)
                .ThenInclude(x => x.HCTrainnings)
                .Where(x =>
                    x.TeamId == productionPlan.TeamId &&
                    x.ProductId == productionPlan.ProductId
                    && x.LayoutId == productionPlan.LayoutId
                )
                .FirstOrDefaultAsync();

            if (teamProduct == null)
            {
                return Results.TeamNotFound("TeamProduct");
            }

            Rise? rise = await GetCandidateRise(teamProduct);

            productionPlan.CalculateDerate(startDate, rise);

            return Result.Ok(productionPlan);
        }

        public async Task<Rise?> GetCandidateRise(Guid TeamProductId)
        {
            var teamProduct = await database.TeamProducts
                .Include(x => x.TeamWorkstations)
                .ThenInclude(x => x.TeamWorkstationHCs)
                .ThenInclude(x => x.HC)
                .ThenInclude(x => x.HCTrainnings)
                .Where(x => x.Id == TeamProductId)
                .FirstOrDefaultAsync();

            if (teamProduct == null)
            {
                return null;
            }

            return await GetCandidateRise(teamProduct);
        }

        public async Task<Rise?> GetCandidateRise(TeamProduct teamProduct)
        {
            Guid ProductId = teamProduct.ProductId;
            string Category = teamProduct.Category;
            int RequiredTraining = teamProduct.GetRequiredTrainning();

            //If the Team is 100% trained and it's not associated to a category,
            //it doesn't need a rise
            if (RequiredTraining == 0 && string.IsNullOrEmpty(Category))
            {
                return null;
            }

            var caseOneRises = await database.Rises
                .Include(x => x.RiseParams)
                .Where(x =>
                    x.IsActive == true
                    && x.ProductId == ProductId
                    && (
                        //Case I:
                        //Category isn't defined,
                        //checks the training range only
                        string.IsNullOrEmpty(Category)
                        && (
                            (x.TrainingMin != null && x.TrainingMax != null)
                            && x.TrainingMin <= RequiredTraining
                            && x.TrainingMax >= RequiredTraining
                        )
                    )
                ).ToListAsync();

            if (caseOneRises.Any())
            {
                //Gets the rise with fewer training days
                return caseOneRises
                   .OrderBy(x => x.RiseParams.Count())
                   .FirstOrDefault();
            }

            var caseTwoRises = await database.Rises
                .Include(x => x.RiseParams)
                .Where(x =>
                    x.IsActive == true
                    && x.ProductId == ProductId
                    && (
                        //Case II:
                        //Category is defined, but training range don't,
                        //checks the category only
                        !string.IsNullOrEmpty(Category)
                        && (
                            (x.TrainingMin == null || x.TrainingMax == null)
                            && x.Category == Category
                        )
                    )
                ).ToListAsync();

            if (caseTwoRises.Any())
            {
                //Gets the rise with fewer training days
                return caseTwoRises
                    .OrderBy(x => x.RiseParams.Count())
                    .FirstOrDefault();
            }

            var caseThreeRises = await database.Rises
               .Include(x => x.RiseParams)
               .Where(x =>
                   x.IsActive == true
                   && x.ProductId == ProductId
                   && (
                        //Case III:
                        //Category and training range are defined,
                        //checks both
                        !string.IsNullOrEmpty(Category)
                        && (
                            (x.TrainingMin != null && x.TrainingMax != null)
                            && x.TrainingMin <= RequiredTraining
                            && x.TrainingMax >= RequiredTraining
                            && x.Category == Category
                        )
                   )
               ).ToListAsync();

            //Gets the rise with fewer training days
            return caseThreeRises
                .OrderBy(x => x.RiseParams.Count())
                .FirstOrDefault();
        }

        public async Task<GetRecommendedTeamResult?> GetRecommendedTeam(Guid productId, Guid shiftId,
            Guid layoutId, IEnumerable<Team> allocatedTeams)
        {
            var allocatedTeamIds = allocatedTeams.Select(x => x.Id);
            var teams = await database.Teams
                .Where(team =>
                    team.TeamProducts.Any(tp => tp.ProductId == productId && tp.LayoutId == layoutId)
                    && team.IsActive
                    && team.ShiftId == shiftId
                    && !allocatedTeamIds.Contains(team.Id)
                )
                .Include(x => x.TeamProducts)
                .ThenInclude(x => x.TeamWorkstations)
                .ThenInclude(x => x.TeamWorkstationHCs)
                .ThenInclude(x => x.HC)
                .ThenInclude(x => x.HCTrainnings)
                .ToListAsync();

            teams = teams.Where(t => t.IsAllHCAvailable(allocatedTeams))
                .ToList();

            if (!teams.Any())
            {
                return null;
            }

            var list = new List<GetRecommendedTeamResult>();
            foreach (Team team in teams)
            {
                var teamProducts = team.TeamProducts
                    .Where(tp => tp.ProductId == productId && tp.LayoutId == layoutId);

                foreach (TeamProduct teamProduct in teamProducts)
                {
                    var rise = await GetCandidateRise(teamProduct);
                    list.Add(new GetRecommendedTeamResult(team, teamProduct, rise));
                }
            }

            //If there is any team that do not need a rise, filter only no rise teams
            if (list.Any(x => x.Rise is null))
            {
                list = list.Where(x => x.Rise is null).ToList();
            }

            //Sort by training level
            list = list.OrderByDescending(x => x.TeamProduct.GetTrainningLevel()).ToList();

            //If the first sorted team is Rise null, returns it and finishes here
            if (list.First().Rise is null)
            {
                return list.First();
            }

            //Filter the best training level
            var bestTrainingLevel = list.First().TeamProduct.GetTrainningLevel();

            //Get all teams with the best training level
            var optimizedTrainingLevelTeams = new List<GetRecommendedTeamResult>();
            foreach (var item in list)
            {
                if (item.TeamProduct.GetTrainningLevel() == bestTrainingLevel)
                {
                    optimizedTrainingLevelTeams.Add(item);
                }
            }

            // Get the team with the small rise
            var optimized = optimizedTrainingLevelTeams
                .OrderBy(x => x.Rise?.RiseParams.Count())
                .FirstOrDefault();

            return optimized;
        }

        private record DateTimeTeam(DateTime Date, Team Team);
    }
}
