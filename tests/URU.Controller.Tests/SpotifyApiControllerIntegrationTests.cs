using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using URU.Models;
using Xunit;

namespace URU.Client.Tests
{
    public class SpotifyApiControllerIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient Client;

        public SpotifyApiControllerIntegrationTests(WebApplicationFactory<Startup> factory)
        {
            Client = factory.CreateClient();
        }

        [Theory]
        [InlineData("/api/spotify/favorites")]
        public async Task Favorites_ReturnsAtLeastFourElementsTest(string url)
        {
            var httpResponse = await Client.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();

            var stringReponse = await httpResponse.Content.ReadAsStringAsync();
            var favorites = JsonConvert.DeserializeObject<Favorites>(stringReponse);

            Assert.True(favorites.Ids.Length > 4);
        }

        [Theory]
        [InlineData("/api/spotify/genres")]
        public async Task Genres_NotEmpty_AllGenresAreEqualTest(string url)
        {
            var httpResponse = await Client.GetAsync(url);
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
        [InlineData("/api/spotify/tracksbyyear")]
        public async Task TracksByYear_NotEmpty_AllItemsAreGreatherThanZeroTest(string url)
        {
            var httpResponse = await Client.GetAsync(url);
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
        [InlineData("/api/spotify/iddurationartists")]
        public async Task IdDurationArtists_NotEmpty_AllItemsAreGreatherThanZeroTest(string url)
        {
            var httpResponse = await Client.GetAsync(url);
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