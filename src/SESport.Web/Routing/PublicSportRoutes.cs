namespace SESport.Web.Routing;

public sealed record PublicSportRoute(
    string SportId,
    string Slug,
    string DisplayName
)
{
    public string Path => "/" + Slug;
}

public static class PublicSportRoutes
{
    public static IReadOnlyList<PublicSportRoute> All { get; } =
    [
        new("alpine-skiing", "alpint", "Alpint"),
        new("athletics", "friidrott", "Friidrott"),
        new("basketball", "basket", "Basket"),
        new("beach-volleyball", "beachvolleyboll", "Beachvolleyboll"),
        new("biathlon", "skidskytte", "Skidskytte"),
        new("billiards", "biljard", "Biljard"),
        new("boat-racing", "batracing", "Båtracing"),
        new("climbing", "klattring", "Klättring"),
        new("cross-country-skiing", "langdskidakning", "Längdskidåkning"),
        new(
            "cross-country-skiing-endurance",
            "langdskidakning-langlopp",
            "Längdskidåkning - långlopp"
        ),
        new("cycling", "cykel", "Cykel"),
        new("darts", "dart", "Dart"),
        new("equestrian", "hastsport", "Hästsport"),
        new("fencing", "faktning", "Fäktning"),
        new("fighting", "kampsport", "Kampsport"),
        new("figure-skating", "konstakning", "Konståkning"),
        new("fishing", "sportfiske", "Sportfiske"),
        new("floorball", "innebandy", "Innebandy"),
        new("football", "fotboll", "Fotboll"),
        new("freeski", "freeski", "Freeski"),
        new("golf", "golf", "Golf"),
        new("handball", "handboll", "Handboll"),
        new("ice-hockey", "ishockey", "Ishockey"),
        new("luge", "rodel", "Rodel"),
        new("moguls", "puckelpist", "Puckelpist"),
        new("motocross", "motocross", "Motocross"),
        new("motorsport", "motorsport", "Motorsport"),
        new("multi-sport", "multisport", "Multisport"),
        new("orienteering", "orientering", "Orientering"),
        new("poker", "poker", "Poker"),
        new("power-lifting", "styrkelyft", "Styrkelyft"),
        new("rally", "rally", "Rally"),
        new("athletics-road-running", "lopning", "Löpning"),
        new("sailing", "segling", "Segling"),
        new("skeleton", "skeleton", "Skeleton"),
        new("skicross", "skicross", "Skicross"),
        new("skiing", "skidakning", "Skidåkning"),
        new("ski-jumping", "backhoppning", "Backhoppning"),
        new("ski-mountaineering", "skidalpinism", "Skidalpinism"),
        new("snowboard", "snowboard", "Snowboard"),
        new("speed-skating", "hastighetsakning", "Hastighetsåkning"),
        new("speedway", "speedway", "Speedway"),
        new("swimming", "simning", "Simning"),
        new("table-tennis", "bordtennis", "Bordtennis"),
        new("tennis", "tennis", "Tennis"),
        new("thai-boxing", "thaiboxning", "Thaiboxning"),
        new("ultra-endurance", "extremsport", "Extremsport"),
        new("volleyball", "volleyboll", "Volleyboll")
    ];

    public static PublicSportRoute? FindByPath(string? path)
    {
        var normalizedPath = path?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        return All.FirstOrDefault(route => string.Equals(
            route.Path,
            normalizedPath,
            StringComparison.OrdinalIgnoreCase
        ));
    }

    public static string? GetPath(string? sportId)
    {
        return FindBySportId(sportId)?.Path;
    }

    public static PublicSportRoute? FindBySportId(string? sportId)
    {
        return All.FirstOrDefault(route => string.Equals(
            route.SportId,
            sportId,
            StringComparison.OrdinalIgnoreCase
        ));
    }
}
