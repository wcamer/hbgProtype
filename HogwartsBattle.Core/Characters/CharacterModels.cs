namespace HogwartsBattle.Core.Characters;

public enum HeroArchetype
{
    Harry,
    Ron,
    Hermione,
    Neville,
    Ginny,
    Luna
}

public sealed class CharacterTrait
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Tier { get; set; }
}

public sealed class CharacterBuild
{
    public HeroArchetype BaseHero { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<string> SelectedTraitIds { get; set; } = new();
}

public static class CharacterTrees
{
    public static readonly Dictionary<HeroArchetype, List<CharacterTrait>> Trees = new()
    {
        [HeroArchetype.Harry] = new()
        {
            new CharacterTrait { Name = "Courage", Description = "Gain +1 attack on first card played each turn.", Tier = 1 },
            new CharacterTrait { Name = "Mentor", Description = "Once per turn another hero draws 1.", Tier = 2 },
            new CharacterTrait { Name = "Protector", Description = "Heal 1 when a Dark Arts card is resolved.", Tier = 3 }
        },
        [HeroArchetype.Ron] = new()
        {
            new CharacterTrait { Name = "Loyalty", Description = "+1 influence when another hero plays an Ally.", Tier = 1 },
            new CharacterTrait { Name = "Collector", Description = "Items cost 1 less once per turn.", Tier = 2 },
            new CharacterTrait { Name = "Tactician", Description = "+1 attack if you bought a card this turn.", Tier = 3 }
        },
        [HeroArchetype.Hermione] = new()
        {
            new CharacterTrait { Name = "Scholar", Description = "+1 influence for each Spell played (first 2).", Tier = 1 },
            new CharacterTrait { Name = "Prepared", Description = "Draw 1 at the start of your turn.", Tier = 2 },
            new CharacterTrait { Name = "Brilliant", Description = "The first Spell you play each turn deals +1 attack.", Tier = 3 }
        },
        [HeroArchetype.Neville] = new()
        {
            new CharacterTrait { Name = "Herbology", Description = "Healing effects you play heal +1.", Tier = 1 },
            new CharacterTrait { Name = "Stubborn", Description = "If you take damage from a Dark Arts card, +1 attack.", Tier = 2 },
            new CharacterTrait { Name = "Leader", Description = "Allies you play give another hero +1 health.", Tier = 3 }
        },
        [HeroArchetype.Ginny] = new()
        {
            new CharacterTrait { Name = "Bold", Description = "+1 attack when you buy a card costing 4 or more.", Tier = 1 },
            new CharacterTrait { Name = "Swift", Description = "Once per turn you may discard a card to draw 1.", Tier = 2 },
            new CharacterTrait { Name = "Inspiring", Description = "When you defeat a Villain, all heroes gain 1 influence.", Tier = 3 }
        },
        [HeroArchetype.Luna] = new()
        {
            new CharacterTrait { Name = "Quirky", Description = "The first time you discard each turn, draw 1.", Tier = 1 },
            new CharacterTrait { Name = "Insightful", Description = "Reveal the top of your deck, you may discard it.", Tier = 2 },
            new CharacterTrait { Name = "Hopeful", Description = "At end of your turn, if you have 0 attack, gain 1 influence.", Tier = 3 }
        },
    };
}