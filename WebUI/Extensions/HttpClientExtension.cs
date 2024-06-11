namespace Microsoft.Extensions.DependencyInjection;

public static class HttpClientExtension
{
    private const string MFG_API = "MFGALLOCATION_API";

    public static IHttpClientBuilder AddMFGHttpClient(this IServiceCollection services, Uri baseAddress)
    {
        return services.AddHttpClient(MFG_API, client =>
        {
            client.BaseAddress = baseAddress;
        });
    }

    public static HttpClient CreateMFGClient(this IHttpClientFactory httpClientFactory)
    {
        return httpClientFactory.CreateClient(MFG_API);
    }

    public static HttpClient WithTenant(this HttpClient httpClient, MFGTenant? tenant)
    {
        if (tenant is not null)
        {
            httpClient.DefaultRequestHeaders.Add("flex-site", tenant.Site.ToString());
            httpClient.DefaultRequestHeaders.Add("flex-company", tenant.Company.ToString());
            httpClient.DefaultRequestHeaders.Add("flex-building", tenant.Building.ToString());
            httpClient.DefaultRequestHeaders.Add("flex-customer", tenant.Customer.ToString());
            httpClient.DefaultRequestHeaders.Add("flex-division", tenant.Division.ToString());
            httpClient.DefaultRequestHeaders.Add("flex-area", tenant.Area.ToString());
        }
        return httpClient;
    }

    public static HttpClient WithUtf(this HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Add("utf", TimeZoneInfo.Local.BaseUtcOffset.Hours.ToString());
        return httpClient;
    }

    public static HttpClient IncludeInactives(this HttpClient httpClient, bool flag)
    {
        httpClient.DefaultRequestHeaders.Add("include-inactives", flag.ToString());
        return httpClient;
    }
}