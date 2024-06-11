

using WebUI.Services;
using WebUI.Shared;

namespace WebUI.Extensions
{
    static class TeamProductExtension
    {
        public static float GetTrainningLevel(this TeamProduct teamProduct)
        {
            float countWorkstation = teamProduct.TeamWorkstations.Count();
            float workstationPerc = countWorkstation > 0 ? 1.0f / countWorkstation : 0.0f;
            var result = teamProduct.TeamWorkstations.Sum(tw=>tw.GetTrainningLevel() * workstationPerc);
            return result;
        }

        public static IEnumerable<HC> GetHCs(this TeamProduct teamProduct)
        {
            return teamProduct.TeamWorkstations.SelectMany(tp => tp.GetHCs());
        }
    }

    static class TeamProductModelExtension
    {
        public static float GetTrainningLevel(this TeamProductModel teamProduct)
        {
            float countWorkstation = teamProduct.Workstations.Count();
            float workstationPerc = countWorkstation > 0 ? 1.0f / countWorkstation : 0.0f;
            return teamProduct.Workstations.Sum(tw => tw.GetTrainningLevel() * workstationPerc);
        }
    }
}
