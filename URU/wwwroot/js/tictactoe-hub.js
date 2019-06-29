'use strict';

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
    player = JSON.parse(player);

    if (player.Item1 !== '') {
        document.getElementById(player.Item1 + '-score').textContent = ' (' + player.Item2 + ')';
    }

    document.getElementById('results').classList.toggle('invisible');
    document.getElementById('results').textContent = winner;

    setTimeout(function () {
        document.getElementById('results').classList.toggle('invisible');
    }, 3000);

    drawBoard();
});

connection.on('ReceiveScores', function (players) {
    for (let i = 0; i < players.length; i++) {
        var playerScore = document.getElementById(players[i].Item1 + '-score');

        if (playerScore) {
            playerScore.textContent = ' (' + players[i].Item2 + ')';
        }
    }
});

connection.on('ReceiveBoard', function (board, playerMoves) {
    playerMoves = JSON.parse(playerMoves);
    board = JSON.parse(board);

    let gameBoard = document.getElementById('board').children;

    for (let i = 0; i < 3; i++) {
        let elements = gameBoard[i].getElementsByTagName('td');
        for (let j = 0; j < elements.length; j++) {
            elements[j].textContent = board[i][j];
        }
    }

    for (let i = 0; i < playerMoves.length; i++) {
        let icon = document.getElementById(playerMoves[i].Item1 + '-tag');
        if (icon && icon.id.includes(playerMoves[i].Item1)) {
            icon.textContent = playerMoves[i].Item2 + ' - ';
        }
    }
});

connection.on('ReceiveTurn', function (player) {
    var activePlayers = document.getElementById('players').getElementsByTagName('span');

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
    let information = document.getElementsByClassName('information');
    let activePlayers = document.getElementById('players');
    let spectators = document.getElementById('spectators');

    activePlayers.innerHTML = '';
    spectators.innerHTML = '';

    if (document.getElementById('game-id').value === '') {
        for (let i = 0; i < information.length; i++) {
            information[i].classList.add('invisible');
        }
        return;
    }

    for (let i = 0; i < information.length; i++) {
        information[i].classList.remove('invisible');
    }

    var playerIndex = 0;
    for (var key in context) {
        let spanTag = document.createElement('span');
        spanTag.id = context[key] + '-tag';
        spanTag.textContent = '? - ';

        let spanId = document.createElement('span');
        spanId.id = context[key] + '-id';
        spanId.textContent = context[key];

        let spanScore = document.createElement('span');
        spanScore.id = context[key] + '-score';
        spanScore.textContent = ' (0)';

        let spanNewLine = document.createElement('span');
        spanNewLine.classList.add('newline');

        if (playerIndex++ < 2) {
            activePlayers.appendChild(spanTag);
            activePlayers.appendChild(spanId);
            activePlayers.appendChild(spanScore);
            activePlayers.appendChild(spanNewLine);
        } else {
            spectators.appendChild(spanTag);
            spectators.appendChild(spanId);
            spectators.appendChild(spanScore);
            spectators.appendChild(spanNewLine);
        }
    }
});

document.getElementById('game-id').addEventListener('input', function () {
    joinRoom();
});