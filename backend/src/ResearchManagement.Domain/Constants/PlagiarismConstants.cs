namespace ResearchManagement.Domain.Constants;

public static class PlagiarismProviders
{
    public const string Copyleaks = "COPYLEAKS";
}

public static class PlagiarismCheckStatuses
{
    public const string Pending = "CHO_XU_LY";
    public const string Sending = "DANG_GUI";
    public const string Processing = "DANG_KIEM_TRA";
    public const string Completed = "HOAN_THANH";
    public const string Failed = "THAT_BAI";
    public const string Cancelled = "DA_HUY";
}
