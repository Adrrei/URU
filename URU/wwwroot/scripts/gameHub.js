﻿'use strict';

const connection = new signalR.HubConnectionBuilder().withUrl('/gameHub').configureLogging(signalR.LogLevel.None).build();

connection.start().catch(function (err) {
    return console.error(err.toString());
});

async function start() {
    try {
        await connection.start();
    } catch (err) {
        setTimeout(() => start(), 10000);
    }
}

connection.onclose(async () => {
    await start();
});

connection.on('ReceiveWinner', function (player, winner) {
    if (player.item1 !== '') {
        document.getElementById(player.item1).textContent = player.item2;
    }

    document.getElementById('win-screen').classList.toggle('invisible');
    document.getElementById('win-screen').textContent = winner;

    setTimeout(function () {
        document.getElementById('win-screen').classList.toggle('invisible');
    }, 3000);

    drawBoard();
});

connection.on('ReceiveScores', function (players) {
    for (let i = 0; i < players.length; i++) {
        var player = document.getElementById(players[i].item1);

        if (player) {
            player.textContent = players[i].item2;
        }
    }
});

connection.on('ReceiveBoard', function (board, playerMoves) {
    var gameBoard = document.getElementById('gameBoard').children;

    for (let i = 0; i < 3; i++) {
        var elements = gameBoard[i].getElementsByTagName('td');
        for (let j = 0; j < elements.length; j++) {
            elements[j].textContent = board[i][j];
        }
    }

    for (let i = 0; i < playerMoves.length; i++) {
        var icon = document.getElementById(playerMoves[i].item1 + 'Icon');
        if (icon && icon.id.includes(playerMoves[i].item1)) {
            icon.textContent = playerMoves[i].item2 + ' - ';
        }
    }
});

connection.on('ReceiveTurn', function (player) {
    var activePlayers = document.getElementById('gamePlayers').getElementsByTagName('span');

    var markedPlayer = false;
    for (let i = 0; i < activePlayers.length; i++) {
        activePlayers[i].classList.remove('player-active');

        if (!markedPlayer && activePlayers[i].textContent.includes(player)) {
            activePlayers[i].classList.add('player-active');
            markedPlayer = true;
        }
    }
});

connection.on('Activity', function (context) {
    var activePlayers = document.getElementById('gamePlayers');
    var spectators = document.getElementById('gameSpectators');

    activePlayers.innerHTML = '';
    spectators.innerHTML = '';

    if (document.getElementById('gameId').value === '')
        return;

    var playerIndex = 0;
    for (var key in context) {
        var playerHtml = '<span class="newline"><span id="' + context[key] + 'Icon"></span>' + context[key] + ' (<span id="' + context[key] + '">0</span>)</span>';

        if (playerIndex++ < 2) {
            activePlayers.innerHTML += playerHtml;
        } else {
            spectators.innerHTML += playerHtml;
        }
    }
});

document.getElementById('gameId').addEventListener('input', function () {
    joinRoom();
});