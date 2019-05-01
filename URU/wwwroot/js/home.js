'use strict';

document.addEventListener('DOMContentLoaded', function () {
    let reveal = document.getElementById('info');
    let people = '.no';
    let love = '@';
    let spamming = 'uru';
    let bots = 'ar';

    reveal.innerHTML = bots + love + spamming + people;

    handleLocalStorage();
    setTimeout(function () {
        preloadSpotify();
    }, 2000);
});

function preloadSpotify() {
    new Image().src = '/content/images/Sunrise.jpg';

    if (getItemFromStorage('Playtime'))
        return;

    try {
        fetch('/Spotify/GetIdDurationArtists')
            .then(function (response) {
                return response.json();
            })
            .then(function (responseJson) {
                setItemInStorage('Playtime', { hours: responseJson.time });
                setItemInStorage('Artists', { artists: responseJson.artists });
            });
    } catch (e) {
        // Ignore, as preload is not crucial
    }

    try {
        fetch('/Spotify/GetTracksByYear')
            .then(function (response) {
                return response.json();
            })
            .then(function (responseJson) {
                setItemInStorage('TracksByYear', { tracks: responseJson.tracksByYear });
            });
    } catch (e) {
        // Ignore, as preload is not crucial
    }

    try {
        fetch('/Spotify/GetGenres')
            .then(function (response) {
                return response.json();
            })
            .then(function (responseJson) {
                setItemInStorage('Genres', { genres: responseJson.genres });
            });
    } catch (e) {
        // Ignore, as preload is not crucial
    }

    try {
        fetch('/Spotify/GetFavorites')
            .then(function (response) {
                return response.json();
            })
            .then(function (responseJson) {
                setItemInStorage('Favorites', { favorites: responseJson.favorites });
            });
    } catch (e) {
        // Ignore, as preload is not crucial
    }
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