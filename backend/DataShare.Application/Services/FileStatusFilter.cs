namespace DataShare.Application.Services;

public enum FileStatusFilter
{
    All,
    Active,
    Expired
}

public static class FileStatusFilterParser
{
    public static bool TryParse(string? input, out FileStatusFilter result)
    {
        result = FileStatusFilter.All;
        if (string.IsNullOrEmpty(input))
            return true;

        return Enum.TryParse(input, ignoreCase: true, out result);
    }
}
