namespace Application.Common.Utils;

public static class CalculationUtils
{
    /// <summary>
    /// Calculates average execution time in seconds.
    /// </summary>
    /// <param name="executionTimeInSeconds">Total execution time in seconds</param>
    /// <param name="success">Number of successful executions</param>
    /// <param name="failed">Number of failed executions</param>
    /// <param name="nextRun">Number of nextRun executions</param>
    /// <returns>Average execution time in seconds, rounded to 2 decimal places</returns>
    public static decimal CalculateAgentAvgExecutionTime(long executionTimeInSeconds, long success, long failed, int nextRun)
    {
        var totalExecutions = success + failed + nextRun;
        return totalExecutions > 0
            ? Math.Round((decimal)executionTimeInSeconds / totalExecutions, 2)
            : 0;
    }

    /// <summary>
    /// Calculates average execution time in seconds.
    /// </summary>
    /// <param name="executionTimeInSeconds">Total execution time in seconds</param>
    /// <param name="success">Number of successful executions</param>
    /// <param name="failed">Number of failed executions</param>
    /// <returns>Average execution time in seconds, rounded to 2 decimal places</returns>
    public static decimal CalculateAgentAvgExecutionTime(long executionTimeInSeconds, long success, long failed)
    {
        var totalExecutions = success + failed;
        return totalExecutions > 0
            ? Math.Round((decimal)executionTimeInSeconds / totalExecutions, 2)
            : 0;
    }
}
