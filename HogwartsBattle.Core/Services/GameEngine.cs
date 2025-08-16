using HogwartsBattle.Core.Cards;
using HogwartsBattle.Core.Game;

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
        state.ActiveVillains = cardIndex.Values.OfType<VillainCard>().Take(2).ToList();

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
        }
    }

    public void PlayCard(GameState state, string playerId, int cardId)
    {
        var p = state.Players.First(x => x.PlayerId == playerId);
        if (!p.Hand.Remove(cardId)) return;
        p.InPlay.Add(cardId);
        var card = state.CardIndex[cardId];
        ApplyEffect(state, p, card.Effect);
    }

    public void BuyCard(GameState state, string playerId, int cardId)
    {
        var p = state.Players.First(x => x.PlayerId == playerId);
        var card = state.CardIndex[cardId];
        if (p.Influence < card.Cost) return;
        if (!state.Supply.Any(c => c.Id == cardId)) return;
        p.Influence -= card.Cost;
        p.Discard.Add(cardId);
        state.Log.Add($"{p.Name} bought {card.Name}.");
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

        // Draw 5 new cards
        DrawCards(state, p, 5);

        // Next player
        state.ActivePlayerIndex = (state.ActivePlayerIndex + 1) % state.Players.Count;
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
}