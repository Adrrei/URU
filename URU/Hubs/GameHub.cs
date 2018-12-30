using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace URU.Hubs
{
    public class GameHub : Hub
    {
        public static readonly ConcurrentDictionary<string, (string, int)> _players = new ConcurrentDictionary<string, (string, int)>();
        public static readonly ConcurrentDictionary<string, (string, string)> _gameSlots = new ConcurrentDictionary<string, (string, string)>();
        public static readonly ConcurrentDictionary<string, IList<string>> _connections = new ConcurrentDictionary<string, IList<string>>();
        public static readonly ConcurrentDictionary<string, int?> _gameTurns = new ConcurrentDictionary<string, int?>();
        public static readonly ConcurrentDictionary<string, string[,]> _gameStates = new ConcurrentDictionary<string, string[,]>();

        private const string MARK_X = "X";
        private const string MARK_O = "O";

        private static readonly int[,] winningMoves = new int[,] {
            { 1, 2, 3 },
            { 4, 5, 6 },
            { 7, 8, 9 },
            { 1, 4, 7 },
            { 2, 5, 8 },
            { 3, 6, 9 },
            { 1, 5, 9 },
            { 3, 5, 7 }
        };

        public void AddPlayer(string playerId)
        {
            _players[Context.ConnectionId] = (playerId, 0);
        }

        public async Task InitializeGroup(string prevGroupName, string groupName)
        {
            if (!string.IsNullOrEmpty(prevGroupName))
            {
                await RemoveFromGroup(prevGroupName);
                RemoveFromPlayerList(prevGroupName);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, prevGroupName);
            }

            if (string.IsNullOrEmpty(groupName))
                return;

            AddToGroup(groupName);
            AddToPlayerList(groupName);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await InitializeBoard(groupName);

            var playerIds = GetRealPlayerIds(groupName);
            await Clients.Group(groupName).SendAsync("Activity", playerIds);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _players.Remove(Context.ConnectionId, out (string, int) ignore);
            await base.OnDisconnectedAsync(exception);

            var groupName = _connections.Where(kvp => kvp.Value.Contains(Context.ConnectionId)).Select(kvp => kvp.Key).FirstOrDefault();

            if (string.IsNullOrEmpty(groupName))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            await RemoveFromGroup(groupName);
            RemoveFromPlayerList(groupName);

            var playerIds = GetRealPlayerIds(groupName);
            await Clients.Group(groupName).SendAsync("Activity", playerIds);
        }

        public async Task UpdateBoard(string groupName, int x, int y)
        {
            var grid = _gameStates.GetValueOrDefault(groupName);

            if (grid == null)
            {
                grid = CreateGrid(3, 3);
            }

            var slots = _gameSlots.GetValueOrDefault(groupName);
            if (slots.Item1 == null || slots.Item2 == null)
                return;

            var turn = _gameTurns.GetValueOrDefault(groupName);
            if (turn == null)
            {
                turn = 2;
            }

            var move = "";
            var opponent = "";
            var player = Context.ConnectionId;
            IList<(string, string)> playerMoves = new List<(string, string)>();

            if (player.Equals(slots.Item1) && turn == 2)
            {
                turn = 1;
                move = MARK_X;
                opponent = _players.GetValueOrDefault(slots.Item2).Item1;
                playerMoves.Add((opponent, MARK_O));
                playerMoves.Add((_players.GetValueOrDefault(slots.Item1).Item1, MARK_X));
            }
            else if (player.Equals(slots.Item2) && turn == 1)
            {
                turn = 2;
                move = MARK_O;
                opponent = _players.GetValueOrDefault(slots.Item1).Item1;
                playerMoves.Add((opponent, MARK_X));
                playerMoves.Add((_players.GetValueOrDefault(slots.Item2).Item1, MARK_O));
            }

            if (!string.IsNullOrEmpty(move))
            {
                if (!grid[x, y].Equals(MARK_X) && !grid[x, y].Equals(MARK_O))
                {
                    _gameTurns[groupName] = turn;
                    grid[x, y] = move;

                    await Clients.Group(groupName).SendAsync("ReceiveTurn", opponent);
                }
            }

            _gameStates[groupName] = grid;

            await Clients.Group(groupName).SendAsync("ReceiveBoard", grid, playerMoves);
        }

        public async Task InitializeBoard(string groupName)
        {
            var grid = _gameStates.GetValueOrDefault(groupName);

            if (grid == null)
            {
                grid = CreateGrid(3, 3);
            }

            var slots = _gameSlots.GetValueOrDefault(groupName);
            if (slots.Item1 == null || slots.Item2 == null)
                return;

            IList<(string, string)> playerMoves = new List<(string, string)>
            {
                (_players.GetValueOrDefault(slots.Item2).Item1, MARK_O),
                (_players.GetValueOrDefault(slots.Item1).Item1, MARK_X)
            };

            _gameStates[groupName] = grid;

            await Clients.Group(groupName).SendAsync("ReceiveBoard", grid, playerMoves);
        }

        public async Task CheckWinner(string groupName)
        {
            var grid = _gameStates.GetValueOrDefault(groupName);

            if (grid == null)
                return;

            string winner = "T"; // Tie

            // Matches the diagonals: [1, 5, 9] || [3, 5, 7]
            if (grid[0, 0].Equals(grid[1, 1]) && grid[1, 1].Equals(grid[2, 2]) || grid[0, 2].Equals(grid[1, 1]) && grid[1, 1].Equals(grid[2, 0]))
            {
                winner = grid[1, 1];
            }

            bool finished = true;
            for (int x = 0; x < 3; x++)
            {
                if (!winner.Equals("T"))
                    break;

                // Whether either position contains a number (in which case we keep going)
                if (int.TryParse(grid[x, 0], out int ignore1) || int.TryParse(grid[x, 1], out int ignore2) || int.TryParse(grid[x, 2], out int ignore3))
                {
                    finished = false;
                }

                // Matches the following rows: [1, 2, 3] || [4, 5, 6] || [7, 8, 9]
                if (grid[x, 0].Equals(grid[x, 1]) && grid[x, 1].Equals(grid[x, 2]))
                {
                    winner = grid[x, 0];
                }

                for (int y = 0; y < 3; y++)
                {
                    // Matches the following columns: [1, 4, 7] || [2, 5, 8] || [3, 6, 9]
                    if (grid[0, y].Equals(grid[1, y]) && grid[1, y].Equals(grid[2, y]))
                    {
                        winner = grid[0, y];
                        break;
                    }
                }
            }

            var winningPlayer = ("", 0);
            if (!winner.Equals("T"))
            {
                winningPlayer = _players[Context.ConnectionId];
                var updateWinningPlayer = (winningPlayer.Item1, winningPlayer.Item2++);
                _players.TryUpdate(Context.ConnectionId, winningPlayer, updateWinningPlayer);
            }

            if (finished || !winner.Equals("T"))
            {
                _gameStates[groupName] = null;
                await Clients.Group(groupName).SendAsync("ReceiveWinner", winningPlayer, winner);
            }
        }

        public async Task UpdateScores(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var connections = _connections[groupName];
            var playerScores = new List<(string, int)>();

            foreach (var player in connections)
            {
                var current = _players[player];
                playerScores.Add(current);
            }

            await Clients.Group(groupName).SendAsync("ReceiveScores", playerScores);
        }

        public IList<string> GetRealPlayerIds(string groupName)
        {
            IList<string> players = new List<string>();
            foreach (var connection in _connections[groupName])
            {
                players.Add(_players[connection].Item1);
            }

            return players;
        }

        public string[,] CreateGrid(int rows, int columns)
        {
            var grid = new string[rows, columns];

            int counter = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    counter++;
                    grid[i, j] = counter.ToString();
                }
            }

            return grid;
        }

        public void AddToPlayerList(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var slots = _gameSlots.GetValueOrDefault(groupName);

            if (string.IsNullOrEmpty(slots.Item1))
            {
                slots.Item1 = Context.ConnectionId;
            }
            else if (string.IsNullOrEmpty(slots.Item2))
            {
                slots.Item2 = Context.ConnectionId;
            }

            _gameSlots[groupName] = slots;
        }

        public void RemoveFromPlayerList(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var slots = _gameSlots.GetValueOrDefault(groupName);
            if (!string.IsNullOrEmpty(slots.Item1) && slots.Item1.Equals(Context.ConnectionId))
            {
                slots.Item1 = null;
            }
            else if (!string.IsNullOrEmpty(slots.Item2) && slots.Item2.Equals(Context.ConnectionId))
            {
                slots.Item2 = null;
            }

            var potentialPlayers = _connections.GetValueOrDefault(groupName);
            if (potentialPlayers.Count >= 2)
            {
                foreach (var player in potentialPlayers)
                {
                    if (slots.Item1 == null && slots.Item2 != player)
                    {
                        slots.Item1 = player;
                        break;
                    }
                    else if (slots.Item2 == null && slots.Item1 != player)
                    {
                        slots.Item2 = player;
                        break;
                    }
                }
            }

            _gameSlots[groupName] = slots;
        }

        public void AddToGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var players = _connections.GetValueOrDefault(groupName);
            if (players == null)
            {
                players = new List<string>();
            }

            if (!players.Contains(Context.ConnectionId))
            {
                players.Add(Context.ConnectionId);
            }

            _connections[groupName] = players;
        }

        public async Task RemoveFromGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var players = _connections.GetValueOrDefault(groupName);
            players.Remove(Context.ConnectionId);
            _connections[groupName] = players;

            await Clients.Group(groupName).SendAsync("Activity", GetRealPlayerIds(groupName));
        }
    }
}
