using WebUI.Services;

namespace WebUI.Extensions
{
    public static class ProductionPlanExtension
    {
        public static bool IsFullTrained(this ProductionPlan productionPlan, AllocationPlanEngine engine)
        {
            if (engine.IsTwo())
            {
                var anyNotAllocated = productionPlan.ProductionDays
                    .Where(x => x.TeamId is null && x.Quantity > 0)
                    .Any();
                if (anyNotAllocated)
                {
                    return false;
                }

                var tps = productionPlan.GetTeamProducts(engine);
                var anyNotTrained = tps.Any(x => x.GetTrainningLevel() < 1);
                return !anyNotTrained;
            }

            return productionPlan.Team?.GetTrainningLevel() == 1;
        }

        public static IEnumerable<TeamProduct> GetTeamProducts(this ProductionPlan productionPlan, AllocationPlanEngine engine)
        {
            if (engine.IsTwo())
            {
                var teamProductList = productionPlan.ProductionDays.Where(x => x.Team is not null).Select(pd => pd.Team.TeamProducts)
                    .SelectMany(s => s)
                    .Where(x => x.ProductId == productionPlan.ProductId && x.LayoutId == productionPlan.LayoutId)
                    .Distinct();
                return teamProductList;
            }

            return productionPlan.Team?.TeamProducts ?? Enumerable.Empty<TeamProduct>();
        }

        public static int GetQuantityAt(this ProductionPlan plan, Guid shift, DateTime date)
        {
            return plan.GetProductionDay(shift, date).Quantity;
        }
        public static ProductionDay GetProductionDay(this ProductionPlan plan, Guid shift, DateTime date)
        {
            return plan.ProductionDays
                .Where(p=>p.ShiftId == shift && p.Date.ToShortDateString() == date.ToShortDateString())
                .First();
        }
        public static HashSet<ProductionDay> GetProductionDay(this IEnumerable<ProductionPlan> plans, DateTime date)
        {
            return plans.SelectMany(p => p.ProductionDays).Where(p=>p.Date == date).ToHashSet();

            // return new HashSet<DateTime> { };
        }

        public static HashSet<ProductionDay> GetPlanDay(this ProductionPlan plan, DateTime date, Guid shift)
        {
            return plan.ProductionDays.Where(p => p.Date == date && p.ShiftId == shift).ToHashSet();
        }

        public static HashSet<DateTime> GetDates(this IEnumerable<ProductionPlan> plans)
        {
            return plans.SelectMany(p=>p.ProductionDays).Select(pd=>pd.Date).ToHashSet();
        }

        public static HashSet<Team> GetTeams(this IEnumerable<ProductionPlan> plans)
        {
            HashSet<Team> teams = new HashSet<Team>();
            var planTeams  = plans.Where(p => p.Team != null).Select(p=>p.Team).ToHashSet();
            foreach (var team in planTeams)
            {
                if(team!=null)
                    teams.Add(team);
            }
            var dayTeams = plans
                .SelectMany(p => p.ProductionDays
                .Where(pd => pd.Team != null)
                .Select(pd=>pd.Team)
                );

            foreach (var team in dayTeams)
            {
                if (team != null)
                    teams.Add(team);
            }
            return teams;
        }

        public static IEnumerable<Shift> GetShifts(this ProductionPlan plan) {
            return plan.ProductionDays
                .Select(pd => pd.Shift)
                .DistinctBy(s => s.Id)
                .OrderBy(s => s.Name);
        }

        public static string GetPlanCell(this ProductionPlan plan, Guid shift, DateTime date)
        {
            var team = plan.GetTeamName(shift, date);
            if (string.IsNullOrEmpty(team))
            {
                return "-";
            }
            var rise = plan.GetRiseDayPercentage(shift, date);
            if (string.IsNullOrEmpty(rise))
            {
                return team;
            }

            return $"{rise} • {team}";
        }

        public static string GetTeamName(this ProductionPlan plan, Guid shift, DateTime dateTime)
        {
            var shortDate = dateTime.ToShortDateString();
            var productionDay = plan.ProductionDays
                .Where(pd => pd.ShiftId == shift && shortDate == pd.Date.ToShortDateString())
                .First();
            return productionDay.Team == null ? String.Empty : productionDay.Team.Description;
        }

        public static DateTime GetStartDate(this ProductionPlan productionPlan)
        {
            return productionPlan.ProductionDays.OrderBy(p => p.Date).First().Date;
        }

        public static DateTime GetStartDate(this IEnumerable<ProductionPlan> productionPlans)
        {
            return productionPlans.OrderBy(p => p.GetStartDate()).Select(p => p.GetStartDate()).First();
        }

        public static DateTime GetEndDate(this IEnumerable<ProductionPlan> productionPlans)
        {
            return productionPlans.OrderByDescending(p => p.GetEndDate()).Select(p => p.GetEndDate()).First();
        }

        public static DateTime GetEndDate(this ProductionPlan productionPlan)
        {
            return productionPlan.ProductionDays.OrderByDescending(p => p.Date).First().Date;
        }

        public static string GetRiseDayPercentage(this ProductionPlan plan, Guid shift, DateTime dateTime)
        {
            var shortDate = dateTime.ToShortDateString();
            var productionDays = plan.ProductionDays.Where(p => p.ShiftId == shift);
            var matrixList = new List<RiseMatrix>();

            foreach (var productionDay in productionDays)
            {
                Team? team = productionDay.Team;
                Rise? rise = productionDay.Rise;
                string percentage = string.Empty;

                if (team is not null && rise is not null)
                {
                    int teamCount = matrixList.Where(x => x.Team?.Id == team?.Id).Count();
                    int paramsCount = rise.RiseParams.Count();

                    if (teamCount < paramsCount)
                    {
                        var param = rise.RiseParams.Where(r => r.Day == teamCount + 1).FirstOrDefault();
                        percentage = param?.Percentage.ToString() + "%";
                    }
                    else
                    {
                        percentage = "100%";
                    }
                }

                if (shortDate == productionDay.Date.ToShortDateString())
                {
                    return percentage;
                }

                matrixList.Add(new RiseMatrix(team, rise, percentage));
            }

            return string.Empty;
        }

        public static void SetTeamOnDay(this ProductionPlan plan, Guid shift, DateTime dateTime, Team team, Rise? rise = null)
        {
            var shortDate = dateTime.ToShortDateString();
            var productionDay = plan.ProductionDays
                .Where(p => p.ShiftId == shift && shortDate == p.Date.ToShortDateString())
                .FirstOrDefault();

            if (productionDay is null) 
            {
                return;
            }

            productionDay.Team = team;
            productionDay.TeamId = team.Id;

            productionDay.RiseId = rise?.Id;
            productionDay.Rise = rise ?? default!;
        }

        public static void RemoveTeamOnDay(this ProductionPlan plan, Guid shift, DateTime dateTime)
        {
            var shortDate = dateTime.ToShortDateString();
            var productionDay = plan.ProductionDays
                .Where(pd => pd.ShiftId == shift && shortDate == pd.Date.ToShortDateString())
                .First();
            productionDay.Team = default!;
            productionDay.TeamId = null;
        }

        public static void RemoveAllTeams(this ProductionPlan plan)
        {
            foreach(var productionDay in plan.ProductionDays)
            {
                productionDay.Team = default!;
                productionDay.TeamId = null;
            }
        }

        private class RiseMatrix
        {
            public Team? Team;
            public Rise? Rise;
            public string Percentage = string.Empty;

            public RiseMatrix(Team? team, Rise? rise, string percentage)
            {
                Team = team;
                Rise = rise;
                Percentage = percentage;
            }
        }
    }
}
