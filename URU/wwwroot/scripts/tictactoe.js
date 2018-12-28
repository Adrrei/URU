var previousRoom = '';

function joinRoom() {
    var room = document.getElementById('gameId').value;

    connection.invoke('AddToGroup', previousRoom, room);
    connection.invoke('UpdateBoard', room, -1, -1);

    document.getElementById('gameRoom').textContent = room;
    previousRoom = room;
}

window.onload = function () {
    drawBoard();
};

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
            var col = document.createElement('td');
            col.id = counter;
            col.innerHTML = counter;

            var handler = function () {
                var room = document.getElementById('gameRoom').textContent;
                connection.invoke('UpdateBoard', room, x, y);
                connection.invoke('CheckWinner', room);
            };

            col.addEventListener('click', handler);

            row.appendChild(col);
            counter++;
        }

        board.appendChild(row);
    }
}
