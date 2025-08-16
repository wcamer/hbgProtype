using HogwartsBattle.Core.Game;

namespace HogwartsBattle.Core.Cards;

public class Card
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public int Cost { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public CardEffect Effect { get; set; } = new();
}

public sealed class VillainCard : Card
{
    public int MaxHealth { get; set; }
}

public sealed class LocationCard : Card
{
    public int ControlTrackLength { get; set; }
}