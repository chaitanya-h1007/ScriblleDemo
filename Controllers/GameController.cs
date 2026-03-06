using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriblleDemo.Data;
using ScriblleDemo.Models;

namespace ScriblleDemo.Controllers
{
    public class GameController : Controller
    {
        private readonly AppDbContext _db;

        public GameController(AppDbContext db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────
        // GET: /Game/Index?roomCode=XXXXXX
        // Main game page — drawing + guessing
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string roomCode)
        {
            var session = await _db.GameSessions
                .Include(g => g.Players)
                .Include(g => g.Rounds)
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            if (session == null) return RedirectToAction("Join", "Lobby");

            // Get current player from session
            var playerId = HttpContext.Session.GetInt32("PlayerId");
            var currentPlayer = session.Players
                .FirstOrDefault(p => p.Id == playerId);

            if (currentPlayer == null) return RedirectToAction("Join", "Lobby");

            // Get or create the current round
            var currentRound = session.Rounds
                .FirstOrDefault(r => r.RoundNumber == session.CurrentRound);

            if (currentRound == null)
            {
                // Pick a random word from the word list
                var wordList = await _db.WordList.ToListAsync();
                var random = new Random();
                var word = wordList[random.Next(wordList.Count)].Word;

                // Pick the drawer — rotate through players each round
                var players = session.Players.ToList();
                var drawerIndex = (session.CurrentRound - 1) % players.Count;
                var drawer = players[drawerIndex];

                currentRound = new Round
                {
                    GameSessionId = session.Id,
                    RoundNumber = session.CurrentRound,
                    WordToDraw = word,
                    DrawerPlayerId = drawer.Id,
                    TimeLimit = 80
                };

                _db.Rounds.Add(currentRound);
                await _db.SaveChangesAsync();
            }

            // Pass data to view using ViewBag
            ViewBag.RoomCode = roomCode;
            ViewBag.CurrentPlayer = currentPlayer;
            ViewBag.CurrentRound = currentRound;
            ViewBag.IsDrawer = currentRound.DrawerPlayerId == playerId;
            ViewBag.TotalRounds = session.TotalRounds;

            // Only show the word to the drawer
            ViewBag.WordToDraw = currentRound.DrawerPlayerId == playerId
                ? currentRound.WordToDraw
                : "???";

            return View(session);
        }

        // ─────────────────────────────────────────
        // GET: /Game/Scoreboard?roomCode=XXXXXX
        // Shows scores after game ends
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Scoreboard(string roomCode)
        {
            var session = await _db.GameSessions
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            if (session == null) return RedirectToAction("Join", "Lobby");

            // Sort players by score descending
            var rankedPlayers = session.Players
                .OrderByDescending(p => p.Score)
                .ToList();

            ViewBag.RoomCode = roomCode;
            ViewBag.RankedPlayers = rankedPlayers;

            return View(session);
        }
    }
}
