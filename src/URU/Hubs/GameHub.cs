using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace URU.Hubs
{
    public class GameHub : Hub
    {
        public static readonly ConcurrentDictionary<string, (string playerId, int playerScore)> _players = new();
        public static readonly ConcurrentDictionary<string, (string playerOne, string playerTwo)> _gameSlots = new();
        public static readonly ConcurrentDictionary<string, IList<string>> _connections = new();
        public static readonly ConcurrentDictionary<string, int?> _gameTurns = new();
        public static readonly ConcurrentDictionary<string, string[,]> _gameStates = new();

        private const string TAG_X = "X";
        private const string TAG_O = "O";

        public void AddPlayer(string playerId)
        {
            _players[Context.ConnectionId] = (playerId, 0);
        }

        public async Task InitializeGroup(string prevGroupName, string groupName)
        {
            if (!string.IsNullOrWhiteSpace(prevGroupName))
            {
                await RemoveFromGroup(prevGroupName);
                RemoveFromPlayerList(prevGroupName);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, prevGroupName);
            }

            if (string.IsNullOrWhiteSpace(groupName))
                return;

            AddToGroup(groupName);
            AddToPlayerList(groupName);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await InitializeBoard(groupName);

            var playerIds = GetRealPlayerIds(groupName);
            await Clients.Group(groupName).SendAsync("Activity", playerIds);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _players.Remove(Context.ConnectionId, out (string, int) ignore);
            await base.OnDisconnectedAsync(exception);

            var groupName = _connections.Where(kvp => kvp.Value.Contains(Context.ConnectionId)).Select(kvp => kvp.Key).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(groupName))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            await RemoveFromGroup(groupName);
            RemoveFromPlayerList(groupName);

            var playerIds = GetRealPlayerIds(groupName);
            await Clients.Group(groupName).SendAsync("Activity", playerIds);
        }

        public async Task UpdateBoard(string groupName, int x, int y)
        {
            string[,] grid = _gameStates.GetValueOrDefault(groupName) ?? CreateGrid(3, 3);

            (string playerOne, string playerTwo) = _gameSlots.GetValueOrDefault(groupName);
            if (playerOne == null || playerTwo == null)
                return;

            var turn = _gameTurns.GetValueOrDefault(groupName);
            turn ??= 2;

            var move = "";
            var opponent = "";
            var player = Context.ConnectionId;
            var playerMoves = new List<(string, string)>();

            if (player.Equals(playerOne) && turn == 2)
            {
                turn = 1;
                move = TAG_X;
                opponent = _players.GetValueOrDefault(playerTwo).playerId;
                playerMoves.Add((opponent, TAG_O));
                playerMoves.Add((_players.GetValueOrDefault(playerOne).playerId, TAG_X));
            }
            else if (player.Equals(playerTwo) && turn == 1)
            {
                turn = 2;
                move = TAG_O;
                opponent = _players.GetValueOrDefault(playerOne).playerId;
                playerMoves.Add((opponent, TAG_X));
                playerMoves.Add((_players.GetValueOrDefault(playerTwo).playerId, TAG_O));
            }

            if (!string.IsNullOrWhiteSpace(move))
            {
                if (!grid[x, y].Equals(TAG_X) && !grid[x, y].Equals(TAG_O))
                {
                    _gameTurns[groupName] = turn;
                    grid[x, y] = move;

                    await Clients.Group(groupName).SendAsync("ReceiveTurn", opponent);
                }
            }

            _gameStates[groupName] = grid;

            await Clients.Group(groupName).SendAsync("ReceiveBoard", JsonConvert.SerializeObject(grid), JsonConvert.SerializeObject(playerMoves));
        }

        public async Task InitializeBoard(string groupName)
        {
            string[,] grid = _gameStates.GetValueOrDefault(groupName) ?? CreateGrid(3, 3);

            (string playerOne, string playerTwo) = _gameSlots.GetValueOrDefault(groupName);
            if (playerOne == null || playerTwo == null)
                return;

            var playerMoves = new List<(string, string)>
            {
                (_players.GetValueOrDefault(playerTwo).playerId, TAG_O),
                (_players.GetValueOrDefault(playerOne).playerId, TAG_X)
            };

            _gameStates[groupName] = grid;

            await Clients.Group(groupName).SendAsync("ReceiveBoard", JsonConvert.SerializeObject(grid), JsonConvert.SerializeObject(playerMoves));
        }

        public async Task CheckWinner(string groupName)
        {
            string[,] grid = _gameStates.GetValueOrDefault(groupName) ?? null!;

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
                if (int.TryParse(grid[x, 0], out _) || int.TryParse(grid[x, 1], out _) || int.TryParse(grid[x, 2], out _))
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

            (string player, int score) = ("", 0);
            if (!winner.Equals("T"))
            {
                (player, score) = _players[Context.ConnectionId];
                var updateVictor = (player, score++);
                _players.TryUpdate(Context.ConnectionId, (player, score), updateVictor);
            }

            if (finished || !winner.Equals("T"))
            {
                _gameStates[groupName] = null!;
                await Clients.Group(groupName).SendAsync("ReceiveWinner", JsonConvert.SerializeObject((player, score)), winner);
            }
        }

        public async Task UpdateScores(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            var connections = _connections[groupName];
            var playerScores = new List<(string playerId, int playerScore)>();

            foreach (var player in connections)
            {
                var current = _players[player];
                playerScores.Add(current);
            }

            await Clients.Group(groupName).SendAsync("ReceiveScores", playerScores);
        }

        public IList<string> GetRealPlayerIds(string groupName)
        {
            var players = new List<string>();
            foreach (var connection in _connections[groupName])
            {
                players.Add(_players[connection].playerId);
            }

            return players;
        }

        public string[,] CreateGrid(int rows, int columns)
        {
            string[,] grid = new string[rows, columns];

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
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            (string playerOne, string playerTwo) = _gameSlots.GetValueOrDefault(groupName);

            if (string.IsNullOrWhiteSpace(playerOne))
            {
                playerOne = Context.ConnectionId;
            }
            else if (string.IsNullOrWhiteSpace(playerTwo))
            {
                playerTwo = Context.ConnectionId;
            }

            _gameSlots[groupName] = (playerOne, playerTwo);
        }

        public void RemoveFromPlayerList(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            (string playerOne, string playerTwo) = _gameSlots.GetValueOrDefault(groupName);
            if (!string.IsNullOrWhiteSpace(playerOne) && playerOne.Equals(Context.ConnectionId))
            {
                playerOne = null!;
            }
            else if (!string.IsNullOrWhiteSpace(playerTwo) && playerTwo.Equals(Context.ConnectionId))
            {
                playerTwo = null!;
            }

            var potentialPlayers = _connections.GetValueOrDefault(groupName);
            if (potentialPlayers?.Count >= 2)
            {
                foreach (var player in potentialPlayers)
                {
                    if (playerOne == null && playerTwo != player)
                    {
                        playerOne = player;
                        break;
                    }
                    else if (playerTwo == null && playerOne != player)
                    {
                        playerTwo = player;
                        break;
                    }
                }
            }

            _gameSlots[groupName] = (playerOne!, playerTwo!);
        }

        public void AddToGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            var players = _connections.GetValueOrDefault(groupName) ?? new List<string>();
            if (!players.Contains(Context.ConnectionId))
            {
                players.Add(Context.ConnectionId);
            }

            _connections[groupName] = players;
        }

        public async Task RemoveFromGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            var players = _connections.GetValueOrDefault(groupName);

            if (players == null)
                return;

            players.Remove(Context.ConnectionId);
            _connections[groupName] = players;

            await Clients.Group(groupName).SendAsync("Activity", GetRealPlayerIds(groupName));
        }
    }
}