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
        public void HeaderHasToken_InvalidAccessTokenAndExpiredToken_TokenInvalid()
        {
            SpotifyService.AccessToken = "";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(-1);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.False(isValid);
        }

        [Fact]
        public void HeaderHasToken_InvalidAccessTokenAndNotExpiredToken_TokenInvalid()
        {
            SpotifyService.AccessToken = "";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(30);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.False(isValid);
        }

        [Fact]
        public void HeaderHasToken_ValidAccessTokenAndExpiredToken_TokenInvalid()
        {
            SpotifyService.AccessToken = "An Access Token";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(-1);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.False(isValid);
        }

        [Fact]
        public void HeaderHasToken_ValidAccessTokenAndNotExpiredToken_TokenValid()
        {
            SpotifyService.AccessToken = "An Access Token";
            SpotifyService.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(30);

            var isValid = SpotifyService.HeaderHasToken();
            Assert.True(isValid);
        }
    }
}