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
        public static readonly ConcurrentDictionary<string, IList<string>> _connections = new ConcurrentDictionary<string, IList<string>>();
        public static readonly ConcurrentDictionary<string, string[,]> _gameStates = new ConcurrentDictionary<string, string[,]>();
        public static readonly ConcurrentDictionary<string, int?> _gameTurns = new ConcurrentDictionary<string, int?>();
        public static readonly ConcurrentDictionary<string, (string, string)> _gameSlots = new ConcurrentDictionary<string, (string, string)>();

        private readonly int[,] winningMoves = new int[,] {
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
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            UpdateSlots(prevGroupName, groupName, true);

            await Clients.Group(groupName).SendAsync("Activity", _connections[groupName]);
        }

        public async Task RemoveFromGroup(string prevGroupName, string groupName)
        {
            await UpdateGroupValues(prevGroupName, groupName);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            UpdateSlots(prevGroupName, groupName);

            await Clients.Group(groupName).SendAsync("Activity", _connections[groupName]);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var groupName = _connections.Where(kvp => kvp.Value.Contains(Context.ConnectionId)).Select(kvp => kvp.Key).FirstOrDefault();
            if (groupName != null)
            {
                await UpdateGroupValues("", groupName);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                await Clients.Group(groupName).SendAsync("Activity", _connections[groupName]);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public void UpdateSlots(string prevGroupName, string groupName, bool add = false)
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

            if (add)
            {
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
            if (x != -1 && y != -1)
            {
                var turn = _gameTurns.GetValueOrDefault(groupName);

                if (turn == null)
                {
                    turn = 2;
                }

                var move = "";
                var player = Context.ConnectionId;
                var slots = _gameSlots.GetValueOrDefault(groupName);

                if (player.Equals(slots.Item1) && turn == 2)
                {
                    turn = 1;
                    move = "X";
                }
                else if (player.Equals(slots.Item2) && turn == 1)
                {
                    turn = 2;
                    move = "O";
                }

                if (!string.IsNullOrEmpty(move))
                {
                    if (!grid[x, y].Equals("X") && !grid[x, y].Equals("O"))
                    {
                        _gameTurns[groupName] = turn;
                        grid[x, y] = move;
                    }
                }
            }

            _gameStates[groupName] = grid;

            await Clients.Group(groupName).SendAsync("ReceiveBoard", grid);
        }

        public async Task DoMove(string groupName)
        {
            var turn = _gameTurns.GetValueOrDefault(groupName);

            if (turn == null)
            {
                turn = 2;
            }

            var move = "";
            var player = Context.ConnectionId;
            var slots = _gameSlots.GetValueOrDefault(groupName);

            if (player.Equals(slots.Item1) && turn == 2)
            {
                turn = 1;
                move = "X";
            }
            else if (player.Equals(slots.Item2) && turn == 1)
            {
                turn = 2;
                move = "O";
            }

            if (!string.IsNullOrEmpty(move))
            {
                _gameTurns[groupName] = turn;
                await Clients.Group(groupName).SendAsync("ReceiveMove", move);
            }
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
                await Clients.Group(groupName).SendAsync("ReceiveWinner", winner);
            }
        }

        public async Task UpdateGroupValues(string prevGroupName, string groupName, bool add = false)
        {
            if (!string.IsNullOrEmpty(prevGroupName))
            {
                var prevPlayers = _connections.GetValueOrDefault(prevGroupName);

                prevPlayers.Remove(Context.ConnectionId);
                _connections[prevGroupName] = prevPlayers;
                await Clients.Group(prevGroupName).SendAsync("Activity", _connections[prevGroupName]);
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
