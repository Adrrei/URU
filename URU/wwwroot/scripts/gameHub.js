var connection = new signalR.HubConnectionBuilder().withUrl('/gameHub').configureLogging(signalR.LogLevel.None).build();

connection.start().catch(function (err) {
    return console.error(err.toString());
});

async function start() {
    try {
        await connection.start();
        console.log('Reconnected');
    } catch (err) {
        setTimeout(() => start(), 5000);
    }
}

connection.onclose(async () => {
    await start();
});

connection.on('ReceiveWinner', function (winner) {
    if (winner === 'X') {
        let score = document.getElementById('player1').textContent;
        document.getElementById('player1').textContent = parseInt(score) + Number(1);
    } else if (winner === 'O') {
        let score = document.getElementById('player2').textContent;
        document.getElementById('player2').textContent = parseInt(score) + Number(1);
    }

    drawBoard();
});

connection.on('ReceiveBoard', function (board) {
    var gameBoard = document.getElementById('gameBoard').children;

    for (let i = 0; i < 3; i++) {
        var elements = gameBoard[i].getElementsByTagName('td');
        for (let j = 0; j < elements.length; j++) {
            elements[j].textContent = board[i][j];
        }
    }
});

connection.on('Activity', function (context) {
    var activePlayers = document.getElementById('gameLog');
    activePlayers.innerHTML = '';

    for (var key in context) {
        activePlayers.innerHTML += '<br/>' + context[key];
    }

});

document.getElementById('gameId').addEventListener('input', function () {
    joinRoom();
});
