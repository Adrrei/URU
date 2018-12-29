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

        public async Task AddToGroup(string prevGroupName, string groupName)
        {
            await UpdateGroupValues(prevGroupName, groupName, true);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, prevGroupName);

            if (!string.IsNullOrEmpty(groupName))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }

            UpdateSlots("", prevGroupName);
            UpdateSlots(prevGroupName, groupName, true);
            await UpdateBoard(prevGroupName, -1, -1);
            await UpdateBoard(groupName, -1, -1);

            if (!string.IsNullOrEmpty(groupName))
            {
                var playerIds = GetRealPlayerIds(groupName);
                await Clients.Group(groupName).SendAsync("Activity", playerIds);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var groupName = _connections.Where(kvp => kvp.Value.Contains(Context.ConnectionId)).Select(kvp => kvp.Key).FirstOrDefault();
            if (groupName != null)
            {
                await UpdateGroupValues("", groupName);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                UpdateSlots("", groupName);
                await UpdateBoard(groupName, -1, -1);

                var playerIds = GetRealPlayerIds(groupName);
                await Clients.Group(groupName).SendAsync("Activity", playerIds);
            }

            _players.Remove(Context.ConnectionId, out (string, int) ignore);

            await base.OnDisconnectedAsync(exception);
        }

        public void AddPlayer(string playerId)
        {
            _players[Context.ConnectionId] = (playerId, 0);
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

        public void UpdateSlots(string prevGroupName, string groupName, bool add = false)
        {
            if (!string.IsNullOrEmpty(prevGroupName))
            {
                var prevSlots = _gameSlots.GetValueOrDefault(prevGroupName);
                if (!string.IsNullOrEmpty(prevSlots.Item1) && prevSlots.Item1.Equals(Context.ConnectionId))
                {
                    prevSlots.Item1 = null;
                }
                else if (!string.IsNullOrEmpty(prevSlots.Item2) && prevSlots.Item2.Equals(Context.ConnectionId))
                {
                    prevSlots.Item2 = null;
                }

                _gameSlots[prevGroupName] = prevSlots;
            }

            if (!string.IsNullOrEmpty(groupName))
            {
                var slots = _gameSlots.GetValueOrDefault(groupName);

                if (add)
                {
                    if (string.IsNullOrEmpty(slots.Item1))
                    {
                        slots.Item1 = Context.ConnectionId;
                    }
                    else if (string.IsNullOrEmpty(slots.Item2))
                    {
                        slots.Item2 = Context.ConnectionId;
                    }
                }
                else
                {
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

                }

                _gameSlots[groupName] = slots;
            }

        }


        public async Task UpdateBoard(string groupName, int x, int y)
        {
            var grid = _gameStates.GetValueOrDefault(groupName);

            if (grid == null)
            {
                const int MAX_X = 3;
                const int MAX_Y = 3;
                grid = new string[MAX_X, MAX_Y];

                int counter = 0;
                for (int i = 0; i < MAX_X; i++)
                {
                    for (int j = 0; j < MAX_Y; j++)
                    {
                        counter++;
                        grid[i, j] = counter.ToString();
                    }
                }
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

            if (x == -1 && y == -1)
            {
                playerMoves.Add((_players.GetValueOrDefault(slots.Item2).Item1, "O - "));
                playerMoves.Add((_players.GetValueOrDefault(slots.Item1).Item1, "X - "));
            }
            else
            {
                if (player.Equals(slots.Item1) && turn == 2)
                {
                    turn = 1;
                    move = "X";
                    opponent = _players.GetValueOrDefault(slots.Item2).Item1;
                    playerMoves.Add((opponent, "O - "));
                    playerMoves.Add((_players.GetValueOrDefault(slots.Item1).Item1, "X - "));
                }
                else if (player.Equals(slots.Item2) && turn == 1)
                {
                    turn = 2;
                    move = "O";
                    opponent = _players.GetValueOrDefault(slots.Item1).Item1;
                    playerMoves.Add((opponent, "X - "));
                    playerMoves.Add((_players.GetValueOrDefault(slots.Item2).Item1, "O - "));
                }

                if (!string.IsNullOrEmpty(move))
                {
                    if (!grid[x, y].Equals("X") && !grid[x, y].Equals("O"))
                    {
                        _gameTurns[groupName] = turn;
                        grid[x, y] = move;

                        await Clients.Group(groupName).SendAsync("ReceiveTurn", opponent);
                    }
                }

            }

            _gameStates[groupName] = grid;

            await Clients.Group(groupName).SendAsync("ReceiveBoard", grid, playerMoves);
        }

        public async Task CheckWinner(string groupName)
        {
            var grid = _gameStates.GetValueOrDefault(groupName);

            if (grid == null)
                return;

            string winner = "";
            if (grid[0, 0].Equals(grid[1, 1]) && grid[1, 1].Equals(grid[2, 2]) || grid[0, 2].Equals(grid[1, 1]) && grid[1, 1].Equals(grid[2, 0]))
            { // 1, 5, 9 || 3, 5, 7
                winner = grid[0, 0];
            }

            if (string.IsNullOrEmpty(winner))
            {
                for (int i = 0; i < 3; i++)
                {
                    if (grid[i, 0].Equals(grid[i, 1]) && grid[i, 1].Equals(grid[i, 2]))
                    { // 1, 2, 3 || 4, 5, 6 || 7, 8, 9
                        winner = grid[i, 0];
                        break;
                    }
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[0, j].Equals(grid[1, j]) && grid[1, j].Equals(grid[2, j]))
                        { // 1, 4, 7 || 2, 5, 8 || 3, 6, 9
                            winner = grid[0, j];
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(winner))
            {
                bool finished = true;
                int counter = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        counter++;
                        if (grid[i, j].Equals(counter.ToString()))
                        {
                            finished = false;
                            break;
                        }
                    }
                }

                if (finished)
                {
                    winner = "TIE";
                }
            }

            if (!string.IsNullOrEmpty(winner))
            {
                _gameStates[groupName] = null;

                var winningPlayer = _players[Context.ConnectionId];
                var updateWinningPlayer = (winningPlayer.Item1, winningPlayer.Item2++);
                _players.TryUpdate(Context.ConnectionId, winningPlayer, updateWinningPlayer);

                await Clients.Group(groupName).SendAsync("ReceiveWinner", winningPlayer);
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

        public async Task UpdateGroupValues(string prevGroupName, string groupName, bool add = false)
        {
            if (!string.IsNullOrEmpty(prevGroupName))
            {
                var prevPlayers = _connections.GetValueOrDefault(prevGroupName);

                prevPlayers.Remove(Context.ConnectionId);
                _connections[prevGroupName] = prevPlayers;

                var playerIds = GetRealPlayerIds(prevGroupName);
                await Clients.Group(prevGroupName).SendAsync("Activity", playerIds);
            }

            var players = _connections.GetValueOrDefault(groupName);

            if (players == null)
            {
                players = new List<string>();
            }

            if (add)
            {
                if (!players.Contains(Context.ConnectionId))
                {
                    players.Add(Context.ConnectionId);
                }
            }
            else
            {
                players.Remove(Context.ConnectionId);
            }

            if (!string.IsNullOrEmpty(groupName))
            {
                _connections[groupName] = players;
            }
        }
    }
}
