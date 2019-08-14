using System;
using URU.Services;
using Xunit;

namespace URU.Controller.Tests.Unit
{
    public class SpotifyApiTests
    {
        private readonly SpotifyService SpotifyService;

        public SpotifyApiTests()
        {
            SpotifyService = new SpotifyService();
        }

        [Fact]
        public void HeaderHasToken_InvalidAccessTokenInvalidTokenExpiresTest()
        {
            SpotifyService.AccessToken = "";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(-1);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.False(isValid);
        }

        [Fact]
        public void HeaderHasToken_InvalidAccessTokenValidTokenExpiresTest()
        {
            SpotifyService.AccessToken = "";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(30);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.False(isValid);
        }

        [Fact]
        public void HeaderHasToken_ValidAccessTokenInvalidTokenExpiresTest()
        {
            SpotifyService.AccessToken = "An Access Token";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(-1);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.False(isValid);
        }

        [Fact]
        public void HeaderHasToken_ValidAccessTokenValidTokenExpiresTest()
        {
            SpotifyService.AccessToken = "An Access Token";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(30);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.True(isValid);
        }
    }
}