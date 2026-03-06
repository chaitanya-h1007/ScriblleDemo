using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ScriblleDemo.Data;

namespace ScriblleDemo.Hubs
{
    public class GameHub : Hub
    {
        private readonly AppDbContext _db;

        public GameHub(AppDbContext db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────
        // Called when a player opens the waiting room
        // Groups let us broadcast only to players
        // in the same room — not everyone on the server
        // ─────────────────────────────────────────
        public async Task JoinRoom(string roomCode, string nickname)
        {
            // Add this connection to a SignalR Group named by roomCode
            // Think of Groups as "channels" — one per game room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            // Tell everyone in the room that someone joined
            // "PlayerJoined" is the function name we'll call on the browser
            await Clients.Group(roomCode).SendAsync("PlayerJoined", nickname);

            // Send the updated player list to everyone in the room
            await SendUpdatedPlayerList(roomCode);
        }

        // ─────────────────────────────────────────
        // Called when a player leaves or disconnects
        // ─────────────────────────────────────────
        public async Task LeaveRoom(string roomCode, string nickname)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("PlayerLeft", nickname);
            await SendUpdatedPlayerList(roomCode);
        }

        // ─────────────────────────────────────────
        // Called by host to start the game
        // Tells ALL players in the room to redirect
        // ─────────────────────────────────────────
        public async Task StartGame(string roomCode)
        {
            // "GameStarted" triggers redirect on all connected browsers
            await Clients.Group(roomCode).SendAsync("GameStarted", roomCode);
        }

        // ─────────────────────────────────────────
        // Helper — fetches fresh player list from DB
        // and broadcasts it to everyone in the room
        // ─────────────────────────────────────────
        private async Task SendUpdatedPlayerList(string roomCode)
        {
            var players = await _db.Players
                .Include(p => p.GameSession)
                .Where(p => p.GameSession.RoomCode == roomCode)
                .Select(p => new
                {
                    p.Id,
                    p.Nickname,
                    p.IsHost,
                    p.Score
                })
                .ToListAsync();

            // Send the player list as JSON to everyone in the room
            await Clients.Group(roomCode)
                .SendAsync("UpdatePlayerList", players);
        }

        // ─────────────────────────────────────────
        // Fires automatically when a browser
        // disconnects (tab closed, page navigated)
        // ─────────────────────────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
