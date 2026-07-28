using Microsoft.Extensions.Configuration;

namespace Tests.TestSupport;

public static class ConfigurationTestFactory
{
    public static IConfiguration Build(Dictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
    }

    public static IConfiguration WithEmailTemplate(string sectionKey, string subject, string url)
    {
        return Build(new Dictionary<string, string?>
        {
            [$"{sectionKey}:Subject"] = subject,
            [$"{sectionKey}:Url"] = url
        });
    }
}
