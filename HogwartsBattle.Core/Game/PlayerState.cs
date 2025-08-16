using HogwartsBattle.Core.Cards;
using HogwartsBattle.Core.Characters;

namespace HogwartsBattle.Core.Game;

public sealed class PlayerState
{
    public required string PlayerId { get; init; }
    public string Name { get; set; } = string.Empty;
    public int MaxHealth { get; set; } = 10;
    public int Health { get; set; } = 10;
    public int Influence { get; set; }
    public int Attack { get; set; }

    public List<int> Library { get; set; } = new();
    public List<int> Hand { get; set; } = new();
    public List<int> Discard { get; set; } = new();
    public List<int> InPlay { get; set; } = new();

    public CharacterBuild Character { get; set; } = new();

    public HashSet<string> TurnFlags { get; set; } = new();
    public Dictionary<string, int> TurnCounters { get; set; } = new();
}