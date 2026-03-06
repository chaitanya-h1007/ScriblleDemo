using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScriblleDemo.Data;
using ScriblleDemo.Models;

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
        // Waiting room — player joins the SignalR group
        // ─────────────────────────────────────────
        public async Task JoinRoom(string roomCode, string nickname)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("PlayerJoined", nickname);
            await SendUpdatedPlayerList(roomCode);
        }

        public async Task LeaveRoom(string roomCode, string nickname)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("PlayerLeft", nickname);
            await SendUpdatedPlayerList(roomCode);
        }

        public async Task StartGame(string roomCode)
        {
            await Clients.Group(roomCode).SendAsync("GameStarted", roomCode);
        }

        // ─────────────────────────────────────────
        // Drawing — called by the drawer's browser
        // x, y = coordinates as % of canvas size
        // so it works on any screen size
        // ─────────────────────────────────────────
        public async Task SendStroke(
            string roomCode,
            float x,
            float y,
            float prevX,
            float prevY,
            string color,
            float size,
            bool isNewStroke)
        {
            // Broadcast to everyone EXCEPT the sender
            // (sender already drew it locally for smoothness)
            await Clients.OthersInGroup(roomCode).SendAsync(
                "ReceiveStroke", x, y, prevX, prevY, color, size, isNewStroke
            );
        }

        // ─────────────────────────────────────────
        // Clear canvas — host or drawer can clear
        // ─────────────────────────────────────────
        public async Task ClearCanvas(string roomCode)
        {
            await Clients.Group(roomCode).SendAsync("CanvasCleared");
        }

        // ─────────────────────────────────────────
        // Guess submitted by a player
        // ─────────────────────────────────────────
        public async Task SubmitGuess(string roomCode, string nickname, string guess)
        {
            var session = await _db.GameSessions
                .Include(g => g.Rounds)
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            if (session == null) return;

            var currentRound = session.Rounds
                .FirstOrDefault(r => r.RoundNumber == session.CurrentRound);

            if (currentRound == null || currentRound.IsCompleted) return;

            var isCorrect = string.Equals(
                guess.Trim(),
                currentRound.WordToDraw,
                StringComparison.OrdinalIgnoreCase
            );

            if (isCorrect)
            {
                // Award points to guesser
                var guesser = session.Players
                    .FirstOrDefault(p => p.Nickname == nickname);

                if (guesser != null)
                {
                    guesser.Score += 100;
                    await _db.SaveChangesAsync();
                }

                // Tell everyone the correct guess
                await Clients.Group(roomCode)
                    .SendAsync("CorrectGuess", nickname, currentRound.WordToDraw);

                // Check if game is over
                if (session.CurrentRound >= session.TotalRounds)
                {
                    session.Status = GameStatus.Finished;
                    await _db.SaveChangesAsync();
                    await Clients.Group(roomCode)
                        .SendAsync("GameOver", roomCode);
                }
                else
                {
                    // Move to next round
                    session.CurrentRound++;
                    currentRound.IsCompleted = true;
                    await _db.SaveChangesAsync();
                    await Clients.Group(roomCode)
                        .SendAsync("NextRound", roomCode);
                }
            }
            else
            {
                // Broadcast the wrong guess to everyone as a chat message
                await Clients.Group(roomCode)
                    .SendAsync("WrongGuess", nickname, guess);
            }
        }

        // ─────────────────────────────────────────
        // Private helper — sends player list to room
        // ─────────────────────────────────────────
        private async Task SendUpdatedPlayerList(string roomCode)
        {
            var players = await _db.Players
                .Include(p => p.GameSession)
                .Where(p => p.GameSession.RoomCode == roomCode)
                .Select(p => new { p.Id, p.Nickname, p.IsHost, p.Score })
                .ToListAsync();

            await Clients.Group(roomCode)
                .SendAsync("UpdatePlayerList", players);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}