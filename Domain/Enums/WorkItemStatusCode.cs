namespace Domain.Enums;

public enum WorkItemStatusCode
{
    Pending = 1,
    Skipped = 2,
    Processing = 3,
    Failed = 4,
    Cancelled = 5,
    Matched = 6,
    Discrepancy = 7,
    Approved = 8,
    Rejected = 9,
    Success = 10,
    NextRun = 11
}