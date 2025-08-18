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
        PlayerState p;
        if (state.IsSoloMode)
        {
            p = state.Players.FirstOrDefault(x => x.PlayerId.StartsWith(playerId) && x.Character.SelectedTraitIds.Count == 0)
                ?? state.Players.First(x => x.PlayerId.StartsWith(playerId));
        }
        else
        {
            p = state.Players.First(x => x.PlayerId == playerId);
        }
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
        var p = GetPlayerForAction(state, playerId);
        if (!p.Hand.Remove(cardId)) return;
        p.InPlay.Add(cardId);
        var card = state.CardIndex[cardId];
        ApplyEffect(state, p, card.Effect);
        ApplyTraitOnCardPlayed(state, p, card);
    }

    public void BuyCard(GameState state, string playerId, int cardId)
    {
        var p = GetPlayerForAction(state, playerId);
        var card = state.CardIndex[cardId];
        var effectiveCost = card.Cost;
        // Collector: Items cost 1 less once per turn
        if (card.Type == CardType.Item && HasTrait(p, "Collector") && !p.TurnFlags.Contains("Collector_Used"))
        {
            effectiveCost = Math.Max(0, effectiveCost - 1);
        }
        if (p.Influence < effectiveCost) return;
        var supplyCard = state.Supply.FirstOrDefault(c => c.Id == cardId);
        if (supplyCard is null) return;
        p.Influence -= effectiveCost;
        p.Discard.Add(cardId);
        state.Supply.Remove(supplyCard);
        state.Log.Add($"{p.Name} bought {card.Name}.");
        if (card.Type == CardType.Item && HasTrait(p, "Collector") && !p.TurnFlags.Contains("Collector_Used"))
        {
            p.TurnFlags.Add("Collector_Used");
            state.Log.Add($"Collector reduced the cost by 1.");
        }
        // Tactician: +1 attack if you bought a card this turn (once per turn)
        if (HasTrait(p, "Tactician") && !p.TurnFlags.Contains("Tactician_Used"))
        {
            p.Attack += 1;
            p.TurnFlags.Add("Tactician_Used");
            state.Log.Add($"{p.Name}'s Tactician grants +1 attack.");
        }
        // Bold: +1 attack when you buy a card costing 4 or more
        if (HasTrait(p, "Bold") && card.Cost >= 4)
        {
            p.Attack += 1;
            state.Log.Add($"{p.Name}'s Bold grants +1 attack for a 4+ cost purchase.");
        }
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
        // Hopeful: At end of your turn, if you have 0 attack, gain 1 influence
        if (HasTrait(p, "Hopeful") && p.Attack == 0)
        {
            p.Influence += 1;
            state.Log.Add($"{p.Name}'s Hopeful grants +1 influence.");
        }
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

        // Next player (solo mode keeps cycling across the 4 heroes controlled by the same user)
        state.ActivePlayerIndex = (state.ActivePlayerIndex + 1) % state.Players.Count;

        // Start of next player's turn
        StartTurn(state);
    }

    public void AttackVillain(GameState state, string playerId, int villainId, int amount)
    {
        var player = GetPlayerForAction(state, playerId);
        if (amount <= 0 || player.Attack < amount) return;
        var villain = state.ActiveVillains.FirstOrDefault(v => v.Id == villainId);
        if (villain is null || villain.CurrentHealth <= 0) return;
        player.Attack -= amount;
        villain.CurrentHealth = Math.Max(0, villain.CurrentHealth - amount);
        state.Log.Add($"{player.Name} dealt {amount} to {villain.Name}.");
        if (villain.CurrentHealth == 0)
        {
            state.Log.Add($"{villain.Name} is defeated!");
            // Inspiring: When you defeat a Villain, all heroes gain 1 influence (if the defeating hero has Inspiring)
            if (HasTrait(player, "Inspiring"))
            {
                foreach (var hero in state.Players)
                {
                    hero.Influence += 1;
                }
                state.Log.Add($"{player.Name}'s Inspiring grants all heroes +1 influence.");
            }
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
            // Stubborn: If you take damage from a Dark Arts card, +1 attack
            if (HasTrait(hero, "Stubborn"))
            {
                hero.Attack += 1;
            }
            // Protector: Heal 1 when a Dark Arts card is resolved
            if (HasTrait(hero, "Protector"))
            {
                hero.Health = Math.Min(hero.MaxHealth, hero.Health + 1);
            }
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
        // Herbology: Healing effects you play heal +1
        if (HasTrait(player, "Herbology") && card.Effect.Resources.Any(r => r.Type == ResourceType.Heal && r.Amount > 0))
        {
            player.Health = Math.Min(player.MaxHealth, player.Health + 1);
            state.Log.Add($"{player.Name}'s Herbology heals +1.");
        }
        // Leader: Allies you play give another hero +1 health
        if (card.Type == CardType.Ally && HasTrait(player, "Leader"))
        {
            var other = state.Players.FirstOrDefault(h => !ReferenceEquals(h, player));
            if (other is not null)
            {
                other.Health = Math.Min(other.MaxHealth, other.Health + 1);
                state.Log.Add($"{player.Name}'s Leader heals {other.Name} for 1.");
            }
        }
    }

    // Mentor: Once per turn another hero draws 1
    public void UseMentor(GameState state, string playerId, string targetPlayerId)
    {
        var player = GetPlayerForAction(state, playerId);
        if (!HasTrait(player, "Mentor") || player.TurnFlags.Contains("Mentor_Used")) return;
        var target = state.Players.FirstOrDefault(x => x.PlayerId == targetPlayerId);
        if (target is null || ReferenceEquals(target, player)) return;
        DrawCards(state, target, 1);
        player.TurnFlags.Add("Mentor_Used");
        state.Log.Add($"{player.Name} mentors {target.Name}: they draw 1.");
    }

    // Swift: Once per turn you may discard a card to draw 1
    public void UseSwiftDiscard(GameState state, string playerId, int cardId)
    {
        var player = GetPlayerForAction(state, playerId);
        if (!HasTrait(player, "Swift") || player.TurnFlags.Contains("Swift_Used")) return;
        if (!player.Hand.Remove(cardId)) return;
        player.Discard.Add(cardId);
        DrawCards(state, player, 1);
        player.TurnFlags.Add("Swift_Used");
        state.Log.Add($"{player.Name} used Swift: discarded and drew 1.");
    }

    private static PlayerState GetPlayerForAction(GameState state, string playerId)
    {
        if (!state.IsSoloMode)
        {
            return state.Players.First(x => x.PlayerId == playerId);
        }
        // Solo mode: the acting player is always the active hero for the solo controller
        return state.Players[state.ActivePlayerIndex];
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