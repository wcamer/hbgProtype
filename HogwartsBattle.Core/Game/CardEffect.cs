using System.Text.Json.Serialization;

namespace HogwartsBattle.Core.Game;

public sealed class ResourceDelta
{
    public ResourceType Type { get; set; }
    public int Amount { get; set; }
}

public sealed class CardEffect
{
    public List<ResourceDelta> Resources { get; set; } = new();
    public int DamageToAllVillains { get; set; }
    public int HealAllHeroes { get; set; }
    public int DrawCards { get; set; }
    public string? ConditionalNote { get; set; }
}