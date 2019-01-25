﻿'use strict';

window.onload = function () {
    var buttonReady = document.getElementById('ready-button');
    buttonReady.addEventListener('click', generatePlayzone);

    var inputPlayerId = document.getElementById('playerId');
    inputPlayerId.addEventListener('keyup', function (e) {
        if (e.key === 'Enter') {
            generatePlayzone();
        }

        if (inputPlayerId.value.length >= 2) {
            buttonReady.classList.add('btn-ready');
        } else {
            buttonReady.classList.remove('btn-ready');
        }
    });

    document.getElementById('name-container').classList.remove('invisible');
    document.getElementById('play-container').classList.add('invisible');
};

var previousRoom = '';

function joinRoom() {
    var room = document.getElementById('gameId').value;

    connection.invoke('InitializeGroup', previousRoom, room);
    connection.invoke('InitializeBoard', room);
    connection.invoke('UpdateScores', room);
    connection.invoke('UpdateScores', previousRoom);

    document.getElementById('gameRoom').textContent = room;
    previousRoom = room;
}

function generatePlayzone() {
    var playerName = document.getElementById('playerId').value;

    if (playerName.length < 2) {
        document.getElementById('nameError').classList.remove('invisible');
        return;
    }

    connection.invoke('AddPlayer', playerName);

    document.getElementById('player').textContent = playerName;
    document.getElementById('nameError').classList.add('invisible');
    document.getElementById('name-container').classList.toggle('invisible');
    document.getElementById('play-container').classList.toggle('invisible');

    drawBoard();
}

function drawBoard() {
    var board = document.getElementById('gameBoard');

    while (board.hasChildNodes()) {
        board.removeChild(board.firstChild);
    }

    var counter = 1;
    for (let x = 0; x < 3; x++) {
        var row = document.createElement('tr');
        row.classList.add('row');

        for (let y = 0; y < 3; y++) {
            var column = document.createElement('td');
            column.innerHTML = counter;
            column.id = counter;

            var handler = function () {
                var room = document.getElementById('gameRoom').textContent;
                connection.invoke('UpdateBoard', room, x, y);
                connection.invoke('CheckWinner', room);
            };

            column.addEventListener('click', handler);

            row.appendChild(column);
            counter++;
        }

        board.appendChild(row);
    }
}