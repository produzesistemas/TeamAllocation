using MudBlazor;
using WebUI.Shared;

namespace Microsoft.Extensions.DependencyInjection;

public static class SnackbarExtension
{
    public static Snackbar Add(this ISnackbar services, string title, IEnumerable<string> list, Severity severity = Severity.Normal)
    {
        string message = $"<div><h3>{title}</h3><ul>";
        foreach (string item in list)
        {
            message += $"<li>{item}</li>";
        }
        message += $"</ul></div>";
        return services.Add(message, severity);
    }
}