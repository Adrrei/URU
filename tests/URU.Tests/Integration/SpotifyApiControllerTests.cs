using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using URU.Models;
using URU.Tests.Utilities;
using Xunit;

namespace URU.Client.Tests.Integration
{
    public class SpotifyApiControllerTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient HttpClient;

        public SpotifyApiControllerTests(CustomWebApplicationFactory<Startup> factory)
        {
            HttpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Favorites_AtLeastFourElements()
        {
            var httpResponse = await HttpClient.GetAsync("/api/Spotify/Favorites", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            var stringReponse = await httpResponse.Content.ReadAsStringAsync();
            var favorites = JsonConvert.DeserializeObject<Favorites>(stringReponse);

            Assert.True(favorites.Ids.Length > 4);
        }

        [Fact]
        public async Task Genres_AllGenresAreEqual()
        {
            var httpResponse = await HttpClient.GetAsync("/api/Spotify/Genres", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            var stringReponse = await httpResponse.Content.ReadAsStringAsync();
            var genres = JsonConvert.DeserializeObject<Genres>(stringReponse).Counts;

            var listedGenres = new ListedGenres().Genres;

            Assert.True(genres.Count > 0);
            foreach (var genre in listedGenres)
            {
                Assert.True(genres.ContainsKey(genre));
                Assert.True(genres.GetValueOrDefault(genre).Item1 > 0);
                Assert.StartsWith("spotify:playlist:", genres.GetValueOrDefault(genre).Item2);
            }
        }

        [Fact]
        public async Task TracksByYear_AllItemsAreGreatherThanZero()
        {
            var httpResponse = await HttpClient.GetAsync("/api/Spotify/TracksByYear", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            var stringReponse = await httpResponse.Content.ReadAsStringAsync();
            var tracksByYear = JsonConvert.DeserializeObject<TracksByYear>(stringReponse).Counts;

            Assert.NotNull(tracksByYear);
            foreach (var pair in tracksByYear)
            {
                Assert.Equal(4, pair.Key.Length);
                Assert.True(pair.Value > 0);
            }
        }

        [Fact]
        public async Task IdDurationArtists_AllItemsAreGreatherThanZero()
        {
            var httpResponse = await HttpClient.GetAsync("/api/Spotify/IdDurationArtists", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            var stringReponse = await httpResponse.Content.ReadAsStringAsync();
            var artists = JsonConvert.DeserializeObject<Artists>(stringReponse);
            var artistDetails = artists.Counts;

            Assert.True(artistDetails.Count > 0);
            foreach (var artist in artistDetails)
            {
                Assert.True(artist.Value.Item1 > 0);
                Assert.StartsWith("spotify:artist:", artist.Value.Item2);
            }

            Assert.True(artists.Hours > 0);
            Assert.True(artists.Songs > 0);
        }
    }
}