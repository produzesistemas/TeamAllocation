using System.Linq;
using WebUI.Services;

namespace WebUI.Extensions
{
    public static class TeamExtension
    {
        public static bool HasTeamProduct(this Team team, Guid productId)
        {
            return team.TeamProducts.Where(t => t.ProductId == productId).Any();
        }
        public static TeamProduct GetTeamProduct(this Team team,  Guid productId)
        {
            return team.TeamProducts.Where(t=>t.ProductId == productId).First();
        }

        public static float GetTrainningLevel(this Team team)
        {
            float teamProductCount = team.TeamProducts.Count();
            float productPerc = teamProductCount > 0 ? 1.0f / teamProductCount : 0.0f;
            return team.TeamProducts.Sum(tp => tp.GetTrainningLevel() * productPerc);
        }

        public static IEnumerable<HC> GetHCs(this IEnumerable<Team> teams)
        {
            return teams.SelectMany(t=>t.GetHCs()).ToHashSet();
        }

        public static IEnumerable<HC> GetHCs(this Team team)
        {
            return team.TeamProducts.SelectMany(tp => tp.GetHCs());
        }

        public static bool AnyHC(this IEnumerable<Team> teams, IEnumerable<HC> hcs)
        {
            var hcIds = hcs.Select(hc => hc.Id);
            foreach (var team in teams)
            {
                var queryResult = team.GetHCs().Where(hc => hcIds.Contains(hc.Id));
                if (queryResult.Any())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
