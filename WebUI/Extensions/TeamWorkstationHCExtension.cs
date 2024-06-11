
using WebUI.Services;

namespace WebUI.Extensions
{
    static class TeamWorkstationHCExtension
    {
        public static bool IsTrained(this TeamWorkstationHC teamWorkstationHc, Guid id)
        {
            return teamWorkstationHc.HC.IsTrained(id);
        }
    }
}
