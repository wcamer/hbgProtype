using HogwartsBattle.Core.Cards;
using HogwartsBattle.Core.Game;
using HogwartsBattle.Core.Characters;

namespace HogwartsBattle.Core.Services;

public sealed class GameEngine
{
    public GameState CreateNewGame(string roomCode)
    {
        var cardIndex = CardFactory.CreateCardIndex();
        var state = new GameState
        {
            RoomCode = roomCode,
            Phase = GamePhase.Lobby,
            CardIndex = cardIndex
        };

        // Seed supply with a few copies
        state.Supply = cardIndex.Values
            .Where(c => c.Type is CardType.Spell or CardType.Item or CardType.Ally or CardType.Potion or CardType.Charm or CardType.Creature)
            .Take(10)
            .ToList();

        // Seed villains and location
        state.ActiveLocation = cardIndex.Values.OfType<LocationCard>().FirstOrDefault();
        state.ActiveVillains = cardIndex.Values.OfType<VillainCard>().Take(2).Select(v => { v.CurrentHealth = v.MaxHealth; return v; }).ToList();

        // Seed a simple Dark Arts deck (repeat the available Dark Arts card)
        var darkArtsIds = cardIndex.Values.Where(c => c.Type == CardType.DarkArts).Select(c => c.Id).ToList();
        for (int i = 0; i < 10; i++)
        {
            state.DarkArtsDeck.Push(darkArtsIds[i % Math.Max(1, darkArtsIds.Count)]);
        }

        return state;
    }

    public PlayerState AddPlayer(GameState state, string playerId, string name)
    {
        var player = new PlayerState { PlayerId = playerId, Name = name };
        // Starter deck: 7 Alohomora (influence), 3 attack spells
        var starters = state.CardIndex.Values.Where(c => c.Name is "Alohomora" or "Stupefy").ToList();
        for (int i = 0; i < 7; i++) player.Library.Add(starters.First(c => c.Name == "Alohomora").Id);
        for (int i = 0; i < 3; i++) player.Library.Add(starters.First(c => c.Name == "Stupefy").Id);
        Shuffle(player.Library);
        DrawCards(state, player, 5);
        state.Players.Add(player);
        return player;
    }

    public void StartGame(GameState state)
    {
        state.Phase = GamePhase.CharacterCreation;
        state.ActivePlayerIndex = 0;
        state.Log.Add("Game started. Proceed to character creation.");
    }

    public void ConfirmCharacter(GameState state, string playerId, HogwartsBattle.Core.Characters.CharacterBuild build)
    {
        var p = state.Players.First(x => x.PlayerId == playerId);
        p.Character = build;
        if (state.Players.All(x => x.Character.SelectedTraitIds.Count > 0))
        {
            state.Phase = GamePhase.InProgress;
            state.Log.Add("All heroes ready. Let the battle begin!");
            StartTurn(state);
        }
    }

    public void PlayCard(GameState state, string playerId, int cardId)
    {
        var p = state.Players.First(x => x.PlayerId == playerId);
        if (!p.Hand.Remove(cardId)) return;
        p.InPlay.Add(cardId);
        var card = state.CardIndex[cardId];
        ApplyEffect(state, p, card.Effect);
        ApplyTraitOnCardPlayed(state, p, card);
    }

    public void BuyCard(GameState state, string playerId, int cardId)
    {
        var p = state.Players.First(x => x.PlayerId == playerId);
        var card = state.CardIndex[cardId];
        if (p.Influence < card.Cost) return;
        var supplyCard = state.Supply.FirstOrDefault(c => c.Id == cardId);
        if (supplyCard is null) return;
        p.Influence -= card.Cost;
        p.Discard.Add(cardId);
        state.Supply.Remove(supplyCard);
        state.Log.Add($"{p.Name} bought {card.Name}.");
        // Restock market with a random marketable card
        var marketPool = state.CardIndex.Values.Where(c => c.Type is CardType.Spell or CardType.Item or CardType.Ally or CardType.Potion or CardType.Charm or CardType.Creature).ToList();
        if (marketPool.Count > 0)
        {
            var next = marketPool[Random.Shared.Next(marketPool.Count)];
            state.Supply.Add(next);
            state.Log.Add($"Market restocked with {next.Name}.");
        }
    }

    public void EndTurn(GameState state)
    {
        var p = state.Players[state.ActivePlayerIndex];
        // Discard hand and in play
        p.Discard.AddRange(p.Hand);
        p.Discard.AddRange(p.InPlay);
        p.Hand.Clear();
        p.InPlay.Clear();
        p.Influence = 0;
        p.Attack = 0;
        p.TurnFlags.Clear();
        p.TurnCounters.Clear();

        // Draw 5 new cards for the player ending their turn
        DrawCards(state, p, 5);

        // Next player
        state.ActivePlayerIndex = (state.ActivePlayerIndex + 1) % state.Players.Count;

        // Start of next player's turn
        StartTurn(state);
    }

    public void AttackVillain(GameState state, string playerId, int villainId, int amount)
    {
        var player = state.Players.First(x => x.PlayerId == playerId);
        if (amount <= 0 || player.Attack < amount) return;
        var villain = state.ActiveVillains.FirstOrDefault(v => v.Id == villainId);
        if (villain is null || villain.CurrentHealth <= 0) return;
        player.Attack -= amount;
        villain.CurrentHealth = Math.Max(0, villain.CurrentHealth - amount);
        state.Log.Add($"{player.Name} dealt {amount} to {villain.Name}.");
        if (villain.CurrentHealth == 0)
        {
            state.Log.Add($"{villain.Name} is defeated!");
            CheckVictoryDefeat(state);
        }
    }

    private static void ApplyEffect(GameState state, PlayerState player, CardEffect effect)
    {
        foreach (var delta in effect.Resources)
        {
            switch (delta.Type)
            {
                case ResourceType.Influence:
                    player.Influence += delta.Amount; break;
                case ResourceType.Attack:
                    player.Attack += delta.Amount; break;
                case ResourceType.Heal:
                    player.Health = Math.Min(player.MaxHealth, player.Health + delta.Amount); break;
                case ResourceType.CardDraw:
                    DrawCards(state, player, delta.Amount); break;
                case ResourceType.ControlProgress:
                    if (state.ActiveLocation != null)
                    {
                        state.LocationControl = Math.Min(state.ActiveLocation.ControlTrackLength, state.LocationControl + delta.Amount);
                    }
                    break;
            }
        }
        if (effect.DrawCards > 0) DrawCards(state, player, effect.DrawCards);
    }

    private static void DrawCards(GameState state, PlayerState player, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (player.Library.Count == 0)
            {
                if (player.Discard.Count == 0) break;
                Shuffle(player.Discard);
                player.Library.AddRange(player.Discard);
                player.Discard.Clear();
            }
            var top = player.Library[^1];
            player.Library.RemoveAt(player.Library.Count - 1);
            player.Hand.Add(top);
        }
    }

    private static void Shuffle(List<int> list)
    {
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void StartTurn(GameState state)
    {
        if (state.Phase != GamePhase.InProgress) return;
        var active = state.Players[state.ActivePlayerIndex];
        state.Log.Add($"It is now {active.Name}'s turn.");
        ResolveDarkArts(state);
        ResolveVillains(state);
        ApplyPreparedTrait(state, active);
        CheckVictoryDefeat(state);
    }

    private void ResolveDarkArts(GameState state)
    {
        // Simple effect: all heroes take 1 damage and location control increases by 1
        foreach (var hero in state.Players)
        {
            hero.Health = Math.Max(0, hero.Health - 1);
        }
        if (state.ActiveLocation != null)
        {
            state.LocationControl = Math.Min(state.ActiveLocation.ControlTrackLength, state.LocationControl + 1);
        }
        state.Log.Add("Dark Arts: All heroes take 1 damage. Location control increases by 1.");
    }

    private void ResolveVillains(GameState state)
    {
        // Each villain deals 1 damage to the active hero
        if (state.ActiveVillains.Count == 0) return;
        var active = state.Players[state.ActivePlayerIndex];
        int totalDamage = state.ActiveVillains.Count;
        active.Health = Math.Max(0, active.Health - totalDamage);
        state.Log.Add($"Villains strike {active.Name} for {totalDamage}.");
    }

    private void ApplyPreparedTrait(GameState state, PlayerState player)
    {
        if (HasTrait(player, "Prepared"))
        {
            DrawCards(state, player, 1);
            state.Log.Add($"{player.Name} draws 1 from Prepared.");
        }
    }

    private void ApplyTraitOnCardPlayed(GameState state, PlayerState player, Card card)
    {
        if (HasTrait(player, "Courage") && !player.TurnFlags.Contains("Courage_Used"))
        {
            player.Attack += 1;
            player.TurnFlags.Add("Courage_Used");
            state.Log.Add($"{player.Name}'s Courage grants +1 attack.");
        }
        if (card.Type == CardType.Spell)
        {
            if (HasTrait(player, "Brilliant") && !player.TurnFlags.Contains("Brilliant_Used"))
            {
                player.Attack += 1;
                player.TurnFlags.Add("Brilliant_Used");
                state.Log.Add($"{player.Name}'s Brilliant adds +1 attack to first Spell.");
            }
            if (HasTrait(player, "Scholar"))
            {
                player.TurnCounters.TryGetValue("Scholar_Spells", out var count);
                if (count < 2)
                {
                    player.Influence += 1;
                    player.TurnCounters["Scholar_Spells"] = count + 1;
                    state.Log.Add($"{player.Name}'s Scholar grants +1 influence (spell {count + 1}/2).");
                }
            }
        }
    }

    private static bool HasTrait(PlayerState player, string traitName)
    {
        var traits = CharacterTrees.Trees[player.Character.BaseHero];
        var selected = new HashSet<string>(player.Character.SelectedTraitIds);
        return traits.Any(t => selected.Contains(t.Id) && string.Equals(t.Name, traitName, StringComparison.Ordinal));
    }

    private void CheckVictoryDefeat(GameState state)
    {
        if (state.ActiveVillains.Count > 0 && state.ActiveVillains.All(v => v.CurrentHealth <= 0))
        {
            state.Phase = GamePhase.Completed;
            state.Log.Add("Heroes are victorious! All villains defeated.");
            return;
        }
        if (state.ActiveLocation is not null && state.LocationControl >= state.ActiveLocation.ControlTrackLength)
        {
            state.Phase = GamePhase.Completed;
            state.Log.Add("Hogwarts falls under control. Heroes are defeated.");
        }
    }
}