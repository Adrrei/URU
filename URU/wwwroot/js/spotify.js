'use strict';

document.addEventListener('DOMContentLoaded', function () {
    getGenres();
    getFavorites();
    getTracksByYear();
    getDetailsArtists();

    let favorites = document.getElementById('reset-favorites');
    favorites.addEventListener('click', function () {
        getFavorites();
    });

    let tableGenres = document.getElementById('genres');
    let toggleGenres = document.getElementById('toggle-genres');
    let tableArtists = document.getElementById('artists');
    let toggleArtists = document.getElementById('toggle-artists');
    let tableTracksByYear = document.getElementById('tracks-by-year');
    let toggleTracksByYear = document.getElementById('toggle-tracks-by-year');

    toggleGenres.addEventListener('click', function () {
        prepareTable(tableGenres, tableArtists, tableTracksByYear, toggleGenres, toggleArtists, toggleTracksByYear, 1);
    });

    toggleArtists.addEventListener('click', function () {
        prepareTable(tableGenres, tableArtists, tableTracksByYear, toggleGenres, toggleArtists, toggleTracksByYear, 2);
    });

    toggleTracksByYear.addEventListener('click', function () {
        prepareTable(tableGenres, tableArtists, tableTracksByYear, toggleGenres, toggleArtists, toggleTracksByYear, 3);
    });

    handleLocalStorage();
});

function prepareTable(tableGenres, tableArtists, tableTracksByYear, toggleGenres, toggleArtists, toggleTracksByYear, id) {
    if (id === 1) {
        tableGenres.classList.remove('hidden');
        toggleGenres.classList.add('checked');
    } else {
        tableGenres.classList.add('hidden');
        toggleGenres.classList.remove('checked');
    }

    if (id === 2) {
        tableArtists.classList.remove('hidden');
        toggleArtists.classList.add('checked');
    } else {
        tableArtists.classList.add('hidden');
        toggleArtists.classList.remove('checked');
    }

    if (id === 3) {
        tableTracksByYear.classList.remove('hidden');
        toggleTracksByYear.classList.add('checked');
    } else {
        tableTracksByYear.classList.add('hidden');
        toggleTracksByYear.classList.remove('checked');
    }
}

function getDetailsArtists() {
    let localPlaytime = getItemFromStorage('Playtime');
    let localSongs = getItemFromStorage('Songs');
    let localArtists = getItemFromStorage('Artists');

    if (localPlaytime && localSongs && localArtists) {
        let displayHours = document.getElementById('hours');
        displayHours.textContent = localPlaytime.hours;
        displayHours.classList.remove('loading');

        let displaySongs = document.getElementById('songs');
        displaySongs.textContent = localSongs.songs;
        displaySongs.classList.remove('loading');

        createTable(localArtists.artists, 'artists');
        return;
    }

    fetch('/Spotify/GetIdDurationArtists')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let hours = responseJson.time;
            let songs = responseJson.songs;

            let displayHours = document.getElementById('hours');
            displayHours.textContent = hours;
            displayHours.classList.remove('loading');

            let displaySongs = document.getElementById('songs');
            displaySongs.textContent = songs;
            displaySongs.classList.remove('loading');

            let artists = responseJson.artists;
            createTable(artists, 'artists');

            setItemInStorage('Playtime', { hours: hours });
            setItemInStorage('Songs', { songs: songs });
            setItemInStorage('Artists', { artists: artists });
        });
}

function getTracksByYear() {
    let localTracksByYear = getItemFromStorage('TracksByYear');

    if (localTracksByYear) {
        createTable(localTracksByYear.tracks, 'tracks-by-year');
        return;
    }

    fetch('/Spotify/GetTracksByYear')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let tracks = responseJson.tracksByYear;
            createTable(tracks, 'tracks-by-year');

            setItemInStorage('TracksByYear', { tracks: tracks });
        });
}

function getGenres() {
    let localGenres = getItemFromStorage('Genres');

    if (localGenres) {
        createTable(localGenres.genres, 'genres');
        return;
    }

    fetch('/Spotify/GetGenres')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let genres = responseJson.genres;
            createTable(genres, 'genres');
            setItemInStorage('Genres', { genres: genres });
        });
}

function getFavorites() {
    let localFavorites = getItemFromStorage('Favorites');

    if (localFavorites) {
        displayFavorites(localFavorites.favorites);
        return;
    }

    fetch('/Spotify/GetFavorites')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            let songs = responseJson.favorites;
            displayFavorites(songs);

            setItemInStorage('Favorites', { favorites: songs });
        });
}

let next = 0;
function displayFavorites(songs) {
    let ids = [];
    for (let i = 0; i < 5; i++) {
        if (next++ > songs.length - 1) {
            next = 0;
        }
        ids.push(songs.splice(next, 1));
    }

    let width = window.innerWidth;

    let coverWidth;
    let coverHeight;

    switch (true) {
        case width > 880:
            coverWidth = 245;
            coverHeight = 245;
            break;
        case width > 360:
            coverWidth = 320;
            coverHeight = 80;
            break;
        default:
            coverWidth = 260;
            coverHeight = 80;
    }

    for (let i = 0; i < 4; i++) {
        let iframe = document.createElement('iframe');
        iframe.src = 'https://open.spotify.com/embed/track/' + ids[i];
        iframe.height = coverHeight;
        iframe.width = coverWidth;

        let placement = document.getElementById('favorites').getElementsByClassName('column')[i];
        placement.innerHTML = '';
        placement.appendChild(iframe);
    }
}

function randomNumber(max) {
    return Math.floor(Math.random() * max);
}

function createTable(dictionary, tableId) {
    let table = document.getElementById(tableId).getElementsByTagName('tbody')[0];
    table.innerHTML = '';

    Object.keys(dictionary).forEach(key => {
        let value = dictionary[key];
        let row = table.insertRow();

        let leftColumn = row.insertCell(0);
        let rightColumn = row.insertCell(1);

        if (value.item1) {
            leftColumn.textContent = key;
            rightColumn.textContent = value.item1;

            row.setAttribute('data-uri', value.item2);

            row.addEventListener('click', function () {
                window.location.href = row.dataset.uri;
            });
        } else {
            leftColumn.textContent = value.key;
            rightColumn.textContent = value.value;
        }

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
    let data = localStorage.getItem(dataKey);
    return data ? JSON.parse(data) : null;
}