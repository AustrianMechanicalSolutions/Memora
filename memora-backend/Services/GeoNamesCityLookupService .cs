using System.Globalization;

public record CityLookupResult(string City, string CountryCode);

public interface ICityLookupService
{
    CityLookupResult? FindNearest(double latitude, double longitude);
}

public class GeoNamesCityLookupService : ICityLookupService
{
    private readonly List<GeoCity> _cities = new();

    public GeoNamesCityLookupService(IWebHostEnvironment env)
    {
        var path = Path.Combine(
            env.ContentRootPath,
            "dataset",
            "cities500.txt"
        );

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split('\t');

            if (parts.Length < 15)
                continue;

            var name = parts[1];
            var lat = double.Parse(parts[4], CultureInfo.InvariantCulture);
            var lon = double.Parse(parts[5], CultureInfo.InvariantCulture);
            var countryCode = parts[8];
            var population = long.TryParse(parts[14], out var p) ? p : 0;

            _cities.Add(new GeoCity(name, lat, lon, countryCode, population));
        }
    }

    public CityLookupResult? FindNearest(double latitude, double longitude)
    {
        if (_cities.Count == 0)
            return null;

        GeoCity? best = null;
        double bestDistance = double.MaxValue;

        foreach (var city in _cities)
        {
            var distance = DistanceKm(latitude, longitude, city.Latitude, city.Longitude);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = city;
            }
        }

        return best == null
            ? null
            : new CityLookupResult(best.Name, best.CountryCode);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) *
            Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double ToRad(double degrees)
        => degrees * Math.PI / 180;

    private record GeoCity(
        string Name,
        double Latitude,
        double Longitude,
        string CountryCode,
        long Population
    );
}

