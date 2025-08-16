namespace HogwartsBattle.Core.Game;

public enum GamePhase
{
    Lobby,
    CharacterCreation,
    InProgress,
    Completed
}

public enum CardType
{
    Spell,
    Item,
    Ally,
    DarkArts,
    Villain,
    Location,
    Creature,
    Potion,
    Charm
}

public enum ResourceType
{
    Influence,
    Attack,
    Heal,
    CardDraw,
    ControlProgress
}