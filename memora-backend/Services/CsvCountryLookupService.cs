public interface ICountryLookupService
{
    string GetCountryName(string countryCode);
}

public class CsvCountryLookupService : ICountryLookupService
{
    private readonly Dictionary<string, string> _countries = new();

    public CsvCountryLookupService(IWebHostEnvironment env)
    {
        var path = Path.Combine(
            env.ContentRootPath,
            "dataset",
            "country_codes.csv"
        );

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');

            if (parts.Length < 2)
                continue;

            var countryName = parts[0].Trim();
            var alpha2 = parts[1].Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(alpha2))
                _countries[alpha2] = countryName;
        }
    }

    public string GetCountryName(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return countryCode;

        return _countries.TryGetValue(countryCode.ToUpperInvariant(), out var name)
            ? name
            : countryCode;
    }
}