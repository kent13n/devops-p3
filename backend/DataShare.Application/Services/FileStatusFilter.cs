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

        // Rejet explicite des entrées numériques (?status=0, ?status=42...)
        // qu'Enum.TryParse accepterait sinon, même pour des valeurs in-range.
        // Seuls les noms textuels (all, active, expired) sont valides.
        if (int.TryParse(input, out _))
            return false;

        return Enum.TryParse(input, ignoreCase: true, out result)
            && Enum.IsDefined(result);
    }
}
