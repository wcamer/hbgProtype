namespace HogwartsBattle.Server.Services;

public interface IImageAssetService
{
    string UrlFor(string categoryKey);
}

public sealed class ImageAssetService : IImageAssetService
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["location"] = "/images/buildings/building1.svg",
        ["location_alt"] = "/images/buildings/building2.svg",
        ["darkarts"] = "/images/food/food1.svg",
        ["character"] = "/images/animals/animal1.svg",
        ["villain"] = "/images/shapes/triangle.svg",
        ["influence"] = "/images/money/money.svg",
        ["attack"] = "/images/lightning.svg",
        ["control"] = "/images/skull.svg",
        ["potion"] = "/images/potion.svg",
        ["charm"] = "/images/charm.svg",
        ["creature"] = "/images/creature.svg",
        ["ally"] = "/images/ally.svg",
        ["item"] = "/images/item.svg",
        ["spell_generic"] = "/images/spell.svg",
        ["shape_triangle"] = "/images/shapes/triangle.svg",
        ["shape_square"] = "/images/shapes/square.svg",
        ["building_tower"] = "/images/buildings/building2.svg",
        ["food_chocolate"] = "/images/food/food1.svg",
        ["lightning"] = "/images/lightning.svg",
        ["skull"] = "/images/skull.svg"
    };

    public string UrlFor(string categoryKey)
    {
        if (Map.TryGetValue(categoryKey, out var url)) return url;
        return "/images/other/placeholder.svg";
    }
}