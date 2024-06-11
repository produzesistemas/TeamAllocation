using WebUI.Services;

namespace WebUI.Extensions
{
    public static class WorstationExtension
    {
        public static string CriticalityLevelColor(this Workstation workstation)
        {
            if (workstation != null)
            {
                switch (workstation.CriticalityLevel)
                {
                    case 0: return "transparent";
                    case 1: return "#FFCC8088";
                    case 2: return "#E5393588";
                }
            }
            return "transparent";
        }
    }
}
