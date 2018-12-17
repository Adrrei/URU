window.onload = function () {
    getSpotifyLatestAddition();
    getSpotifyFavorites(1);
    getSpotifyPlaytime();
};

function getSpotifyLatestAddition() {
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
            document.getElementById('spotify-latest-addition').appendChild(iframe);

            var table = document.getElementById('table-wrapper').getElementsByTagName('tbody')[0];
            for (let [key, value] of Object.entries(response.playlists)) {
                var tableRow = '<tr><th class="left">' + key + '</th><td class="right">' + value + '</td></tr>';
                table.innerHTML += tableRow;
            }
        }
    };
}

function resetGetSpotifyFavorites() {
    document.getElementById('cards').innerHTML = '';
    getSpotifyFavorites(0);
}

function getSpotifyFavorites(initial) {
    var request = new XMLHttpRequest();
    request.open('GET', uru.Urls.GetSpotifyFavorites, true);
    request.responseType = 'json';
    request.send();

    var cards = document.getElementById('cards');
    for (let i = 0; i < 5; i++) {
        var spotifyPlaceholder = new Image;
        spotifyPlaceholder.onload = function () {
            cards.src = this.src;
        };

        var width = window.innerWidth
            || document.documentElement.clientWidth
            || document.body.clientWidth;

        if (width > 580) {
            spotifyPlaceholder.src = '/content/images/Spotify-240x240.png';
        } else if (width > 344) {
            spotifyPlaceholder.src = '/content/images/Spotify-320x80.png';
        } else if (width > 320) {
            spotifyPlaceholder.src = '/content/images/Spotify-300x80.png';
        } else {
            spotifyPlaceholder.src = '/content/images/Spotify-260x80.png';
        }

        spotifyPlaceholder.classList.add('spotify-card-margin');

        cards.appendChild(spotifyPlaceholder);
    }

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

                document.getElementById('cards').replaceChild(iframe, placeholders[initial++]);
            });

        }
    };
}

function getSpotifyPlaytime() {
    var request = new XMLHttpRequest();
    request.open('GET', uru.Urls.GetSpotifyPlaytime, true);
    request.responseType = 'json';
    request.send();

    request.onload = function () {
        if (request.status === 200) {
            document.getElementsByClassName('loading')[0].classList.remove('loading');

            var response = request.response;

            var hours = parseInt(response.hours);
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