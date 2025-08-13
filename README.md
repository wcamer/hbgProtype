# Hogwarts Battle (Blazor Server)

A multiplayer Hogwarts Battle-inspired deckbuilding game implemented as a .NET 8 Blazor Server web app. Includes extensions for Creatures, Potions, and Charms, a 2–6 player online lobby, SignalR real-time sync, and a character creation mode with trait trees inspired by Harry, Ron, Hermione, Neville, Ginny, and Luna.

Note: All visual assets are placeholder SVGs per your mapping request (buildings for locations, food for Dark Arts, animals for characters, shapes for villains, $ for influence, lightning bolt for attack, skull-and-crossbones for location control, and other simple placeholders elsewhere).

## Requirements
- Linux/macOS/Windows
- .NET 8 SDK (installed locally in this workspace under `dotnet/` if not already present)

## Getting Started

1) Build
```bash
cd /workspace
./dotnet/dotnet build
```

2) Run
```bash
cd /workspace/HogwartsBattle.Server
../dotnet/dotnet run --no-launch-profile --urls http://0.0.0.0:5199
```
Then open your browser to http://localhost:5199

3) Multiplayer
- Open the app in multiple browser windows.
- Go to Lobby (default) and either Create Room (gets a code) or Join Room with an existing code.
- Up to 6 concurrent players per room are supported (practical limit; server is in-memory).

## Project Structure
- `HogwartsBattle.Server/` Blazor Server app
  - SignalR hub: `Hubs/GameHub.cs`
  - Services: `Services/ImageAssetService.cs`
  - UI: `Components/Pages/Lobby.razor`, `Room.razor`, `CharacterCreate.razor`
  - Static assets: `wwwroot/images/...` placeholder SVGs
- `HogwartsBattle.Core/` core game engine
  - Game models: `Game/*.cs`
  - Cards: `Cards/Card.cs`
  - Characters: `Characters/CharacterModels.cs`
  - Engine and factory: `Services/GameEngine.cs`, `CardFactory.cs`

## Gameplay Flow
- Lobby: Create or Join a room. The creator can start the game.
- Character Creation: Choose a base hero and select traits. When all players have at least one trait, the game moves to InProgress.
- In-Game:
  - Each player begins with a starter deck.
  - Play cards from your hand to gain influence/attack/heal/draw.
  - Buy cards from the Market to strengthen your deck.
  - End your turn to pass play to the next player.

This implementation provides a playable skeleton with the requested extensions and online sync. You can expand rules, dark arts effects, villain activations, damage resolution, defeat/victory checks, and persistence as desired.

## Image Mapping (Placeholders)
- Locations: buildings (`wwwroot/images/buildings/*.svg`)
- Dark Arts: food (`wwwroot/images/food/*.svg`)
- Characters: animals (`wwwroot/images/animals/*.svg`)
- Villains: shapes (`wwwroot/images/shapes/*.svg`)
- Influence: money `$` (`wwwroot/images/money/money.svg`)
- Attack: lightning (`wwwroot/images/lightning.svg`)
- Location Control: skull and crossbones (`wwwroot/images/skull.svg`)
- Other: `wwwroot/images/other/placeholder.svg`

## Notes
- In-memory rooms and state: restarting the server clears rooms.
- HTTPS is enabled by default in dev; the README run command uses HTTP for simplicity.
- To deploy, host the server publicly and expose the configured URL/port. SignalR uses WebSockets with fallback. 
