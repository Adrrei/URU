'use strict';

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('player-id').value = '';
    document.getElementById('game-id').value = '';

    let buttonReady = document.getElementById('ready-button');
    buttonReady.addEventListener('click', generatePlayzone);

    let inputPlayerId = document.getElementById('player-id');
    inputPlayerId.addEventListener('keyup', function (e) {
        if (inputPlayerId.value.length < 2) {
            buttonReady.classList.remove('btn-ready');
            return;
        }

        buttonReady.classList.add('btn-ready');

        if (e.key === 'Enter') {
            generatePlayzone();
        }
    });
});

function generatePlayzone() {
    let playerName = document.getElementById('player-id').value;

    if (playerName.length < 2) {
        document.getElementById('name-error').classList.remove('invisible');
        return;
    }

    connection.invoke('AddPlayer', playerName);

    document.getElementById('player').textContent = playerName;
    document.getElementById('name-container').outerHTML = '';
    document.getElementById('play-container').classList.remove('invisible');
    document.getElementById('game-id').focus();

    drawBoard();
}

let previousRoom = '';

function joinRoom() {
    let room = document.getElementById('game-id').value;

    connection.invoke('InitializeGroup', previousRoom, room);
    connection.invoke('InitializeBoard', room);
    connection.invoke('UpdateScores', room);
    connection.invoke('UpdateScores', previousRoom);

    document.getElementById('room').textContent = room;
    previousRoom = room;
}

function drawBoard() {
    let board = document.getElementById('board');

    while (board.hasChildNodes()) {
        board.removeChild(board.firstChild);
    }

    let squares = 1;
    for (let x = 0; x < 3; x++) {
        var row = document.createElement('tr');
        row.classList.add('row');

        for (let y = 0; y < 3; y++) {
            var column = document.createElement('td');
            column.innerHTML = squares;
            column.id = squares++;

            column.addEventListener('click', function () {
                let room = document.getElementById('room').textContent;
                connection.invoke('UpdateBoard', room, x, y);
                connection.invoke('CheckWinner', room);
            });

            row.appendChild(column);
        }

        board.appendChild(row);
    }
}