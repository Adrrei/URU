window.onload = function () {
    getSpotifyLatestAddition();
    getSpotifyFavorites(1);
    getSpotifyPlaytime();
    getSpotifyTopArtists(10);

    handleLocalStorage();

    document.getElementById('spotify-set-artist-count').addEventListener('blur', resetTableSpotifyArtists);
};

function handleLocalStorage() {
    var localDuration = getItemFromStorage('StorageDuration');

    if (localDuration) {
        var currentDate = new Date(JSON.parse(JSON.stringify(new Date())));
        var expiryDate = new Date(JSON.parse(JSON.stringify(localDuration.StorageDuration)));

        if (currentDate > expiryDate) {
            localStorage.removeItem('StorageDuration');
            localStorage.removeItem('Genres');
            localStorage.removeItem('Artists');
            localStorage.removeItem('Hours');
        }

    } else {
        let date = new Date();
        date.setHours(date.getHours() + 3);
        setItemInStorage('StorageDuration', { StorageDuration: JSON.parse(JSON.stringify(date)) });
    }
}

function setItemInStorage(dataKey, data) {
    localStorage.setItem(dataKey, JSON.stringify(data));
}

function getItemFromStorage(dataKey) {
    var data = localStorage.getItem(dataKey);
    return data ? JSON.parse(data) : null;
}

function getSpotifyPlaytime() {

    var localHours = getItemFromStorage('Hours');
    if (localHours) {
        document.getElementsByClassName('loading')[0].classList.remove('loading');
        document.getElementById('spotify-playtime').textContent = localHours.Hours;
        return;
    }

    var request = new XMLHttpRequest();
    request.open('GET', uru.Urls.GetSpotifyPlaytime, true);
    request.responseType = 'json';
    request.send();

    request.onload = function () {
        if (request.status === 200) {
            document.getElementsByClassName('loading')[0].classList.remove('loading');

            var response = request.response;

            var hours = parseInt(response.hours);

            setItemInStorage('Hours', { Hours: hours });

            var hoursCounter = 0;
            setInterval(function () {
                if (hoursCounter++ >= hours) {
                    return;
                }
                document.getElementById('spotify-playtime').textContent = hoursCounter;
            }, 15);
        }
    };
}

function getSpotifyLatestAddition() {
    var localGenres = getItemFromStorage('Genres');
    if (localGenres) {
        createTableSpotifyGenres(localGenres.Genres);
    }

    var request = new XMLHttpRequest();
    request.open('GET', uru.Urls.GetSpotifyPlaylists, true);
    request.responseType = 'json';
    request.send();

    request.onload = function () {
        if (request.status === 200) {
            var response = request.response;

            var iframe = document.createElement('iframe');
            iframe.src = 'https://open.spotify.com/embed/track/' + response.exquisiteEdm.items[0].track.id;
            iframe.height = 80;
            iframe.width = 300;
            iframe.setVolume = 0.1;
            document.getElementById('spotify-latest-addition').appendChild(iframe);

            if (localGenres)
                return;

            createTableSpotifyGenres(response.genres);

            setItemInStorage('Genres', { Genres: response.genres });
        }
    };
}

function createTableSpotifyGenres(genres) {
    var table = document.getElementById('table-genres').getElementsByTagName('tbody')[0];
    for (var key in genres) {
        var tableRow = '<tr><th class="left">' + key + '</th><td class="right">' + genres[key] + '</td></tr>';
        table.innerHTML += tableRow;
    }
}

function resetGetSpotifyFavorites() {
    document.getElementById('cards').innerHTML = '';
    getSpotifyFavorites(0);
}

function getSpotifyFavorites(initial) {
    var cards = document.getElementById('cards');

    var width = window.innerWidth
        || document.documentElement.clientWidth
        || document.body.clientWidth;

    var albumSource = '/content/images/Spotify-';
    switch (true) {
        case width > 580:
            albumSource += '240x240.png';
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

    for (let i = 0; i < 5; i++) {
        var albumPlaceholder = new Image;

        albumPlaceholder.onload = function () {
            cards.src = this.src;
        };
        albumPlaceholder.src = albumSource;
        albumPlaceholder.classList.add('spotify-card-margin');

        cards.appendChild(albumPlaceholder);
    }

    var request = new XMLHttpRequest();
    request.open('GET', uru.Urls.GetSpotifyFavorites, true);
    request.responseType = 'json';
    request.send();

    request.onload = function () {
        if (request.status === 200) {
            var response = request.response;
            var favorites = response.favorites;

            var numberOfFavorites = document.getElementById('spotify-favorites-count');
            numberOfFavorites.textContent = favorites.length;
            var placeholders = document.getElementById('cards').childNodes;

            favorites.forEach(function (item) {
                var iframe = document.createElement('iframe');
                iframe.src = 'https://open.spotify.com/embed/track/' + item.track.id;
                iframe.height = 240;
                iframe.width = 240;
                iframe.setVolume = 0.1;

                document.getElementById('cards').replaceChild(iframe, placeholders[initial++]);
            });
        }
    };
}

function resetTableSpotifyArtists() {
    var numberOfArtists = document.getElementById('spotify-set-artist-count').value;
    getSpotifyTopArtists(numberOfArtists);
}

function getSpotifyTopArtists(numberOfArtists) {
    var localArtists = getItemFromStorage('Artists');
    if (numberOfArtists === '') {
        numberOfArtists = 10;
    }

    if (localArtists && localArtists.Artists.length === parseInt(numberOfArtists)) {
        createTableSpotifyArtists(localArtists.Artists);
        return;
    }

    var request = new XMLHttpRequest();
    request.open('POST', uru.Urls.GetSpotifyTopArtists, true);
    request.responseType = 'json';

    var payload = new FormData();
    payload.append('numberOfArtists', numberOfArtists);

    request.send(payload);

    request.onload = function () {
        if (request.status === 200) {
            var artists = request.response.artists;

            createTableSpotifyArtists(artists);

            setItemInStorage('Artists', { Artists: artists });
        }
    };
}

function createTableSpotifyArtists(artists) {
    var table = document.getElementById('table-top-artists').getElementsByTagName('tbody')[0];
    table.innerHTML = '';

    for (let i = 0; i < artists.length; i++) {
        var tableRow = '<tr><th class="left">' + artists[i].key + '</th><td class="right">' + artists[i].value + '</td></tr>';
        table.innerHTML += tableRow;
    }
}