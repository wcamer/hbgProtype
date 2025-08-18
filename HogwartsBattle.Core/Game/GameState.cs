using HogwartsBattle.Core.Cards;

namespace HogwartsBattle.Core.Game;

public sealed class GameState
{
    public string RoomCode { get; set; } = string.Empty;
    public GamePhase Phase { get; set; } = GamePhase.Lobby;
    public bool IsSoloMode { get; set; }
    public string? SoloControllerId { get; set; }

    public List<PlayerState> Players { get; set; } = new();
    public int ActivePlayerIndex { get; set; }

    public List<Card> Supply { get; set; } = new();
    public Stack<int> DarkArtsDeck { get; set; } = new();
    public Stack<int> VillainDeck { get; set; } = new();

    public List<VillainCard> ActiveVillains { get; set; } = new();
    public LocationCard? ActiveLocation { get; set; }
    public int LocationControl { get; set; }

    public Dictionary<int, Card> CardIndex { get; set; } = new();

    public List<string> Log { get; set; } = new();
}