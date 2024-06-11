
using WebUI.Services;
using WebUI.Shared;

namespace WebUI.Extensions
{
    static class TeamWorkstationExtension
    {
        public static float GetTrainningLevel(this TeamWorkstation teamWorkstation)
        {
            float hcCount = teamWorkstation.TeamWorkstationHCs.Count();
            float hcPerc = 1 / hcCount;
            return teamWorkstation.TeamWorkstationHCs.Sum(
                twhc=> {
                    float value = twhc.IsTrained(teamWorkstation.WorkstationId) ? 1.0f : 0.0f;
                    return value * hcPerc;
                 }
            );
        }

        public static IEnumerable<HC> GetHCs(this TeamWorkstation teamWorkstation)
        {
            return teamWorkstation.TeamWorkstationHCs.Select(tw => tw.HC);
        }
    }

    static class TeamProductWorkstationModelExtension
    {
        public static float GetTrainningLevel(this TeamProductWorkstationModel teamWorkstation)
        {
            float hcCount = teamWorkstation.HCs.Count();
            float hcPerc = 1 / hcCount;
            return teamWorkstation.HCs.Sum(
                twhc => {
                    float value = twhc.IsTrained(teamWorkstation.Workstation.Id) ? 1.0f : 0.0f;
                    return value * hcPerc;
                }
            );
        }
    }
}
