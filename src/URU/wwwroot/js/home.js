'use strict';

document.addEventListener('DOMContentLoaded', function () {
    let info = document.getElementById('info');
    let infoLocalize = document.getElementById('info-localize');
    let people = '.no';
    let love = '@';
    let spamming = 'uru';
    let bots = 'ar';

    info.title = bots + love + spamming + people;
    info.href = 'mailto:' + info.title;
    infoLocalize.textContent = info.title;

    preloadSpotify();

    let toggleAbout = document.getElementById('toggle-about');
    toggleAbout.addEventListener('click', function () {
        let wrapper = document.getElementById('wrapper');
        let projects = document.getElementById('projects');
        let about = document.getElementById('about');

        setItemInStorage('sidebar', 'about');

        // About sidebar is open
        if (wrapper.classList.contains('use-sidebar') && !about.classList.contains('hidden')) {
            wrapper.classList.remove('use-sidebar');
            about.classList.add('hidden');
            localStorage.removeItem('sidebar');
        } else {
            wrapper.classList.add('use-sidebar');
            about.classList.remove('hidden');
            projects.classList.add('hidden');
        }
    });

    let toggleProjects = document.getElementById('toggle-projects');
    toggleProjects.addEventListener('click', function () {
        let wrapper = document.getElementById('wrapper');
        let projects = document.getElementById('projects');
        let about = document.getElementById('about');

        setItemInStorage('sidebar', 'projects');

        // Projects sidebar is open
        if (wrapper.classList.contains('use-sidebar') && !projects.classList.contains('hidden')) {
            wrapper.classList.remove('use-sidebar');
            projects.classList.add('hidden');
            localStorage.removeItem('sidebar');
        } else {
            wrapper.classList.add('use-sidebar');
            projects.classList.remove('hidden');
            about.classList.add('hidden');
        }
    });

    let localSidebar = getItemFromStorage('sidebar');
    if (localSidebar) {
        if (localSidebar === 'about') {
            toggleAbout.click();
        } else if (localSidebar === 'projects') {
            toggleProjects.click();
        }
    }
});

function preloadSpotify() {
    handleLocalStorage();

    new Image().src = '/content/images/Sunrise.jpg';

    if (getItemFromStorage('Playtime'))
        return;

    try {
        fetch('/api/Spotify/IdDurationArtists')
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
        fetch('/api/Spotify/TracksByYear')
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
        fetch('/api/Spotify/Genres')
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
        fetch('/api/Spotify/Favorites')
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