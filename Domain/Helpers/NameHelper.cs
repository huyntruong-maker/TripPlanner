namespace Domain.Helpers;

using System.Collections.Generic;
using System.Linq;

public static class NameHelper
{
    public static string GeneratePipelineRunName(string pipelineName, DateTime date, int runNumber)
    {
        const string template = "{pipelineName}_{date}.{rev}";
        var values = new Dictionary<string, string>
        {
            ["pipelineName"] = pipelineName,
            ["date"] = date.ToString("yyyyMMdd"),
            ["rev"] = runNumber.ToString()
        };

        return ReplaceTemplate(template, values);
    }

    public static string ReplaceTemplate(string template, Dictionary<string, string> values)
    {
        return values.Aggregate(template, (current, kvp) => current.Replace($"{{{kvp.Key}}}", kvp.Value));
    }

    public static string GenerateJobName(string pipelineRunName, int version)
    {
        return $"{pipelineRunName}_Job_{version}";
    }

    public static string GenerateJobLogKey(Guid jobId, int start)
    {
        return $"JobLog:{jobId}:{start}";
    }
}