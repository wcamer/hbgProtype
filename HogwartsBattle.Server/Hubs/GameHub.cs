using HogwartsBattle.Core.Characters;
using HogwartsBattle.Core.Game;
using HogwartsBattle.Core.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace HogwartsBattle.Server.Hubs;

public sealed class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, GameState> Rooms = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionToRoom = new();
    private readonly GameEngine _engine;

    public GameHub(GameEngine engine)
    {
        _engine = engine;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        ConnectionToRoom.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task<string> CreateRoom(string playerName)
    {
        var code = GenerateRoomCode();
        var state = _engine.CreateNewGame(code);
        Rooms[code] = state;
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        ConnectionToRoom[Context.ConnectionId] = code;
        _engine.AddPlayer(state, Context.ConnectionId, playerName);
        await Clients.Group(code).SendAsync("GameUpdated", state);
        return code;
    }

    public async Task JoinRoom(string code, string playerName)
    {
        if (!Rooms.TryGetValue(code, out var state))
        {
            throw new HubException("Room not found");
        }
        if (state.Players.Count >= 6)
        {
            throw new HubException("Room is full (max 6 players).");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        ConnectionToRoom[Context.ConnectionId] = code;
        _engine.AddPlayer(state, Context.ConnectionId, playerName);
        await Clients.Group(code).SendAsync("GameUpdated", state);
    }

    public async Task Attach(string code)
    {
        if (!Rooms.TryGetValue(code, out var state))
        {
            throw new HubException("Room not found");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        ConnectionToRoom[Context.ConnectionId] = code;
        await Clients.Caller.SendAsync("GameUpdated", state);
    }

    public async Task StartGame()
    {
        var code = RequireRoom();
        var state = Rooms[code];
        _engine.StartGame(state);
        await Clients.Group(code).SendAsync("GameUpdated", state);
    }

    public async Task ConfirmCharacter(CharacterBuild build)
    {
        var code = RequireRoom();
        var state = Rooms[code];
        _engine.ConfirmCharacter(state, Context.ConnectionId, build);
        await Clients.Group(code).SendAsync("GameUpdated", state);
    }

    public async Task PlayCard(int cardId)
    {
        var code = RequireRoom();
        var state = Rooms[code];
        _engine.PlayCard(state, Context.ConnectionId, cardId);
        await Clients.Group(code).SendAsync("GameUpdated", state);
    }

    public async Task BuyCard(int cardId)
    {
        var code = RequireRoom();
        var state = Rooms[code];
        _engine.BuyCard(state, Context.ConnectionId, cardId);
        await Clients.Group(code).SendAsync("GameUpdated", state);
    }

    public async Task EndTurn()
    {
        var code = RequireRoom();
        var state = Rooms[code];
        _engine.EndTurn(state);
        await Clients.Group(code).SendAsync("GameUpdated", state);
    }

    private string RequireRoom()
    {
        if (!ConnectionToRoom.TryGetValue(Context.ConnectionId, out var code))
        {
            throw new HubException("Not in a room");
        }
        return code;
    }

    private static string GenerateRoomCode()
    {
        var rng = Random.Shared;
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string Next() => new string(Enumerable.Range(0, 5).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        string code;
        do { code = Next(); } while (Rooms.ContainsKey(code));
        return code;
    }
}