'use strict';

document.addEventListener('DOMContentLoaded', function () {
    getGenres();
    getFavorites();
    getIdDurationArtists();

    let favorites = document.getElementById('reset-favorites');
    favorites.addEventListener('click', function () {
        getFavorites();
    });

    let setArtists = document.getElementById('set-artists');
    setArtists.addEventListener('blur', function () {
        getTopArtists(this.value);
    });

    handleLocalStorage();
});

function getIdDurationArtists() {
    let latest = getItemFromStorage('Latest');
    let playtime = getItemFromStorage('Playtime');
    let artists = getItemFromStorage('Artists');

    let iframe = document.createElement('iframe');
    iframe.height = 80;
    iframe.width = 300;

    if (latest && playtime && artists) {
        iframe.src = 'https://open.spotify.com/embed/track/' + latest.id;
        document.getElementById('latest').appendChild(iframe);

        document.getElementsByClassName('loading')[0].classList.remove('loading');
        document.getElementById('playtime').textContent = playtime.hours;

        createTable(artists.artists, 'table-artists');
        return;
    }

    fetch('/Spotify/GetIdDurationArtists')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            document.getElementsByClassName('loading')[0].classList.remove('loading');

            let hours = responseJson.time;
            document.getElementById('playtime').textContent = hours;

            let latest = responseJson.latest;
            iframe.src = 'https://open.spotify.com/embed/track/' + latest;
            document.getElementById('latest').appendChild(iframe);

            let artists = responseJson.artists;
            createTable(artists, 'table-artists');

            setItemInStorage('Playtime', { hours: hours });
            setItemInStorage('Latest', { id: latest });
            setItemInStorage('Artists', { artists: artists });
        });
}

function getGenres() {
    let localGenres = getItemFromStorage('Genres');

    if (localGenres) {
        createTable(localGenres.genres, 'table-genres');
    }

    fetch('/Spotify/GetGenres')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let genres = responseJson.genres;
            createTable(genres, 'table-genres');
            setItemInStorage('Genres', { genres: genres });
        });
}

function getFavorites() {
    let width = window.innerWidth;

    let albumSource = '/content/images/Spotify-';
    switch (true) {
        case width > 580:
            albumSource += '245x245.png';
            break;
        case width > 344:
            albumSource += '320x80.png';
            break;
        case width > 320:
            albumSource += '300x80.png';
            break;
        default:
            albumSource += '260x80.png';
    }

    let cards = document.getElementById('cards');
    cards.innerHTML = '';

    for (let i = 0; i < 5; i++) {
        var albumPlaceholder = new Image;

        albumPlaceholder.onload = function () {
            cards.src = this.src;
        };
        albumPlaceholder.src = albumSource;
        albumPlaceholder.classList.add('card-margin');

        cards.appendChild(albumPlaceholder);
    }

    fetch('/Spotify/GetFavorites')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let favorites = responseJson.favorites;

            let numFavorites = document.getElementById('favorites-count');
            numFavorites.textContent = favorites.length;
            let placeholders = document.getElementById('cards').childNodes;

            let i = 0;
            favorites.forEach(function (track) {
                let iframe = document.createElement('iframe');
                iframe.src = 'https://open.spotify.com/embed/track/' + track;
                iframe.height = 245;
                iframe.width = 245;

                document.getElementById('cards').replaceChild(iframe, placeholders[i++]);
            });
        });
}

function getTopArtists(numArtists) {
    let artists = getItemFromStorage('Artists');

    if (artists && artists.artists.length === parseInt(numArtists)) {
        createTable(artists.artists, 'table-artists');
        return;
    }

    fetch('/Spotify/GetTopArtists?artists=' + numArtists)
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let artists = responseJson.artists;

            createTable(artists, 'table-artists');

            setItemInStorage('Artists', { artists: artists });
        });
}

function createTable(dictionary, tableId) {
    let table = document.getElementById(tableId).getElementsByTagName('tbody')[0];
    table.innerHTML = '';

    Object.keys(dictionary).forEach(key => {
        let value = dictionary[key];
        let row = table.insertRow();

        let leftColumn = row.insertCell(0);
        leftColumn.classList.add('left');
        leftColumn.textContent = key;

        let rightColumn = row.insertCell(1);
        rightColumn.classList.add('right');
        rightColumn.textContent = value;

        row.appendChild(leftColumn);
        row.appendChild(rightColumn);

        table.appendChild(row);
    });
}

function handleLocalStorage() {
    let reset = getItemFromStorage('Reset');
    if (reset) {
        var currentDate = new Date();
        var expiryDate = new Date(reset.date);

        if (currentDate > expiryDate) {
            localStorage.clear();
        } else {
            return;
        }
    }

    let date = new Date();
    date.setHours(date.getHours() + 3);
    setItemInStorage('Reset', { date: date });
}

function setItemInStorage(dataKey, data) {
    localStorage.setItem(dataKey, JSON.stringify(data));
}

function getItemFromStorage(dataKey) {
    var data = localStorage.getItem(dataKey);
    return data ? JSON.parse(data) : null;
}