using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using URU.Models;
using Xunit;

namespace URU.Client.Tests.Integration
{
    public class SpotifyApiControllerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient HttpClient;

        public SpotifyApiControllerTests(WebApplicationFactory<Startup> factory)
        {
            HttpClient = factory.CreateClient();
        }

        [Theory]
        [InlineData("/api/Spotify/Favorites")]
        public async Task Favorites_ReturnsAtLeastFourElementsTest(string url)
        {
            var httpResponse = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            var stringReponse = await httpResponse.Content.ReadAsStringAsync();
            var favorites = JsonConvert.DeserializeObject<Favorites>(stringReponse);

            Assert.True(favorites.Ids.Length > 4);
        }

        [Theory]
        [InlineData("/api/Spotify/Genres")]
        public async Task Genres_NotEmpty_AllGenresAreEqualTest(string url)
        {
            var httpResponse = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
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

        [Theory]
        [InlineData("/api/Spotify/TracksByYear")]
        public async Task TracksByYear_NotEmpty_AllItemsAreGreatherThanZeroTest(string url)
        {
            var httpResponse = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
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

        [Theory]
        [InlineData("/api/Spotify/IdDurationArtists")]
        public async Task IdDurationArtists_NotEmpty_AllItemsAreGreatherThanZeroTest(string url)
        {
            var httpResponse = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
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