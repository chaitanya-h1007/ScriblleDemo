using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriblleDemo.Data;
using ScriblleDemo.Models;
using ScriblleDemo.Services;

namespace ScriblleDemo.Controllers
{
    public class LobbyController: Controller
    {
        private readonly AppDbContext _db;

        //constructor
        public LobbyController(AppDbContext db)
        {
            this._db = db;
        }

        // ─────────────────────────────────────────
        // GET: /Lobby/Create
        // Shows the "Create Room" form
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View();

        }


        // ─────────────────────────────────────────
        // POST: /Lobby/Create
        // Handles form submission — creates the room
        // ─────────────────────────────────────────


        [HttpPost]
        public async Task<IActionResult> Create(string nickname, int totalRounds = 3)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                ViewBag.Error = "Please enter a nickname.";
                return View();
            }

            // Generate a unique room code
            string roomCode;
            do
            {
                roomCode = RoomCodeGenerator.Generate();
            }
            // Keep generating until we get one that doesn't exist yet
            while (await _db.GameSessions.AnyAsync(g => g.RoomCode == roomCode));

            // Create the game session (the room)
            var session = new GameSession
            {
                RoomCode = roomCode,
                TotalRounds = totalRounds,
                Status = GameStatus.Waiting
            };

            _db.GameSessions.Add(session);
            await _db.SaveChangesAsync(); // Save to get the session.Id

            // Create the host player
            var player = new Player
            {
                Nickname = nickname,
                GameSessionId = session.Id,
                IsHost = true
            };

            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            // Store player info in Session (browser memory for this user)
            // So we know WHO this browser tab is across pages
            HttpContext.Session.SetInt32("PlayerId", player.Id);
            HttpContext.Session.SetString("Nickname", player.Nickname);
            HttpContext.Session.SetInt32("GameSessionId", session.Id);

            // Redirect to the waiting lobby
            return RedirectToAction("Waiting", new { roomCode = session.RoomCode });
        }

        // ─────────────────────────────────────────
        // GET: /Lobby/Join
        // Shows the "Join Room" form
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Join()
        {
            return View();
        }

        // ─────────────────────────────────────────
        // POST: /Lobby/Join
        // Handles form submission — joins existing room
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Join(string nickname, string roomCode)
        {
            if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(roomCode))
            {
                ViewBag.Error = "Please enter both nickname and room code.";
                return View();
            }

            roomCode = roomCode.Trim().ToUpper();

            // Find the game session
            var session = await _db.GameSessions
                .Include(g => g.Players) // Also load the Players list
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            // Does the room exist?
            if (session == null)
            {
                ViewBag.Error = "Room not found. Check your room code.";
                return View();
            }

            // Is the game already started?
            if (session.Status != GameStatus.Waiting)
            {
                ViewBag.Error = "This game has already started.";
                return View();
            }

            // Is the room full? (Max 10 players)
            if (session.Players.Count >= session.MaxPlayers)
            {
                ViewBag.Error = "This room is full (10/10 players).";
                return View();
            }

            // Add the new player
            var player = new Player
            {
                Nickname = nickname,
                GameSessionId = session.Id,
                IsHost = false
            };

            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            // Store in session
            HttpContext.Session.SetInt32("PlayerId", player.Id);
            HttpContext.Session.SetString("Nickname", player.Nickname);
            HttpContext.Session.SetInt32("GameSessionId", session.Id);

            return RedirectToAction("Waiting", new { roomCode = session.RoomCode });
        }

        // ─────────────────────────────────────────
        // GET: /Lobby/Waiting/AB12XY
        // The waiting room — shows who has joined
        // ─────────────────────────────────────────
        
        [HttpGet]
        public async Task<IActionResult> Waiting(string roomCode)
        {
            var session = await _db.GameSessions
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            if (session == null) return RedirectToAction("Join");

            var playerId = HttpContext.Session.GetInt32("PlayerId");
            var currentPlayer = session.Players.FirstOrDefault(p => p.Id == playerId);

            if (currentPlayer == null) return RedirectToAction("Join");

            // Route to different views based on who is viewing
            if (currentPlayer.IsHost)
            {
                return View("WaitingHost", session);
            }
            else
            {
                return View("WaitingPlayer", session);
            }
        }

        // ─────────────────────────────────────────
        // POST: /Lobby/StartGame
        // Host clicks "Start Game"
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> StartGame(int sessionId)
        {
            var session = await _db.GameSessions
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == sessionId);

            if (session == null) return RedirectToAction("Join");

            // Only allow start if at least 2 players
            if (session.Players.Count < 2)
            {
                return RedirectToAction("Waiting", new { roomCode = session.RoomCode });
            }

            session.Status = GameStatus.InProgress;
            session.CurrentRound = 1;
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Game", new { roomCode = session.RoomCode });
        }

        // ─────────────────────────────────────────
        // POST: /Lobby/LeaveRoom
        // Non-host player leaves the room
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> LeaveRoom(int sessionId)
        {
            var playerId = HttpContext.Session.GetInt32("PlayerId");

            if (playerId != null)
            {
                var player = await _db.Players.FindAsync(playerId);
                if (player != null)
                {
                    _db.Players.Remove(player);
                    await _db.SaveChangesAsync();
                }
            }

            // Clear session data
            HttpContext.Session.Clear();

            return RedirectToAction("Join");
        }

        // ─────────────────────────────────────────
        // POST: /Lobby/CancelRoom
        // Host cancels and deletes the room
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CancelRoom(int sessionId)
        {
            var session = await _db.GameSessions.FindAsync(sessionId);

            if (session != null)
            {
                // Cascade delete removes all players too
                _db.GameSessions.Remove(session);
                await _db.SaveChangesAsync();
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Create");
        }

    }



}
