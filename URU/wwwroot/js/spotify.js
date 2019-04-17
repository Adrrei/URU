document.addEventListener('DOMContentLoaded', function () {
    getGenres();
    getFavorites();
    getTracksByYear();
    getIdDurationArtists();

    let favorites = document.getElementById('reset-favorites');
    favorites.addEventListener('click', function () {
        getFavorites();
    });

    let toggleArtists = document.getElementById('toggle-artists');
    let toggleGenres = document.getElementById('toggle-genres');
    let toggleTracksByYear = document.getElementById('toggle-tracks-by-year');
    let tableArtists = document.getElementById('artists');
    let tableGenres = document.getElementById('genres');
    let tableTracksByYear = document.getElementById('tracks-by-year');

    toggleArtists.addEventListener('click', function () {
        tableArtists.classList.remove('hidden');
        tableGenres.classList.add('hidden');
        tableTracksByYear.classList.add('hidden');

        toggleArtists.classList.add('checked');
        toggleGenres.classList.remove('checked');
        toggleTracksByYear.classList.remove('checked');
    });

    toggleGenres.addEventListener('click', function () {
        tableGenres.classList.remove('hidden');
        tableArtists.classList.add('hidden');
        tableTracksByYear.classList.add('hidden');

        toggleGenres.classList.add('checked');
        toggleArtists.classList.remove('checked');
        toggleTracksByYear.classList.remove('checked');
    });

    toggleTracksByYear.addEventListener('click', function () {
        tableTracksByYear.classList.remove('hidden');
        tableArtists.classList.add('hidden');
        tableGenres.classList.add('hidden');

        toggleTracksByYear.classList.add('checked');
        toggleArtists.classList.remove('checked');
        toggleGenres.classList.remove('checked');
    });

    handleLocalStorage();
});

function getIdDurationArtists() {
    let playtime = getItemFromStorage('Playtime');
    let artists = getItemFromStorage('Artists');

    if (playtime && artists) {
        document.getElementsByClassName('loading')[0].classList.remove('loading');
        document.getElementById('hours').textContent = playtime.hours;

        createTable(artists.artists, 'artists');
        return;
    }

    fetch('/Spotify/GetIdDurationArtists')
        .then(function (response) {
            return response.json();
        })
        .then(function (responseJson) {
            document.getElementsByClassName('loading')[0].classList.remove('loading');

            let hours = responseJson.time;
            document.getElementById('hours').textContent = hours;

            let artists = responseJson.artists;
            createTable(artists, 'artists');

            setItemInStorage('Playtime', { hours: hours });
            setItemInStorage('Artists', { artists: artists });
        });
}

function getTracksByYear() {
    let tracksByYear = getItemFromStorage('TracksByYear');

    if (tracksByYear) {
        createTable(tracksByYear.tracks, 'tracks-by-year');
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

function displayFavorites(songs) {
    let ids = [];
    for (let i = 0; i < 4; i++) {
        let position = randomNumber(songs.length);

        while (ids.includes(position)) {
            position = randomNumber(songs.length);
        }

        ids.push(position);
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
        iframe.src = 'https://open.spotify.com/embed/track/' + songs[ids[i]];
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
    var data = localStorage.getItem(dataKey);
    return data ? JSON.parse(data) : null;
}