using HogwartsBattle.Core.Cards;
using HogwartsBattle.Core.Game;

namespace HogwartsBattle.Core.Services;

public static class CardFactory
{
    private static int _nextId = 1;

    public static Dictionary<int, Card> CreateCardIndex()
    {
        var index = new Dictionary<int, Card>();

        Card Add(Card c)
        {
            c.Id = _nextId++;
            index[c.Id] = c;
            return c;
        }

        // Market cards (Spells, Items, Allies, Potions, Charms, Creatures)
        Add(new Card { Name = "Alohomora", Type = CardType.Spell, Cost = 0, Text = "+1 influence.", ImageKey = "spell_generic", Effect = new CardEffect { Resources = new() { new ResourceDelta { Type = ResourceType.Influence, Amount = 1 } } } });
        Add(new Card { Name = "Stupefy", Type = CardType.Spell, Cost = 2, Text = "+1 attack.", ImageKey = "lightning", Effect = new CardEffect { Resources = new() { new ResourceDelta { Type = ResourceType.Attack, Amount = 1 } } } });
        Add(new Card { Name = "Potion Kit", Type = CardType.Potion, Cost = 3, Text = "Heal 2.", ImageKey = "potion", Effect = new CardEffect { Resources = new() { new ResourceDelta { Type = ResourceType.Heal, Amount = 2 } } } });
        Add(new Card { Name = "Charmed Focus", Type = CardType.Charm, Cost = 4, Text = "+1 influence and draw 1.", ImageKey = "charm", Effect = new CardEffect { Resources = new() { new ResourceDelta { Type = ResourceType.Influence, Amount = 1 } }, DrawCards = 1 } });
        Add(new Card { Name = "Friendly Creature", Type = CardType.Creature, Cost = 3, Text = "Heal 1 and draw 1.", ImageKey = "creature", Effect = new CardEffect { Resources = new() { new ResourceDelta { Type = ResourceType.Heal, Amount = 1 } }, DrawCards = 1 } });
        Add(new Card { Name = "Ally", Type = CardType.Ally, Cost = 2, Text = "+1 influence, +1 attack.", ImageKey = "ally", Effect = new CardEffect { Resources = new() { new ResourceDelta { Type = ResourceType.Influence, Amount = 1 }, new ResourceDelta { Type = ResourceType.Attack, Amount = 1 } } } });

        // Villains (shapes placeholders)
        Add(new VillainCard { Name = "Triangle Terror", Type = CardType.Villain, MaxHealth = 8, Text = "Villain effect placeholder.", ImageKey = "shape_triangle" });
        Add(new VillainCard { Name = "Square Scourge", Type = CardType.Villain, MaxHealth = 10, Text = "Villain effect placeholder.", ImageKey = "shape_square" });

        // Location (buildings)
        Add(new LocationCard { Name = "Ancient Tower", Type = CardType.Location, ControlTrackLength = 5, Text = "Location control.", ImageKey = "building_tower" });

        // Dark Arts (food placeholders)
        Add(new Card { Name = "Chocolate Hex", Type = CardType.DarkArts, Text = "All heroes take 1 damage.", ImageKey = "food_chocolate", Effect = new CardEffect { Resources = new(), DamageToAllVillains = 0, HealAllHeroes = 0 } });

        return index;
    }
}