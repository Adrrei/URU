namespace URU.Client.Data
{
    public class ClientConfiguration(string clientId, string clientSecret)
    {
        public string ClientId { get; set; } = clientId;

        public string ClientSecret { get; set; } = clientSecret;
    }

    public class SpotifyConfiguration
    {
        public SpotifyConfiguration()
        {
            UserId = "11157411586";
            FavoritesId = "48HcflR8QplI2zgAutNDnT";
            ExquisiteEdmId = "7ssZYYankNsiAfeyPATtXe";
        }

        public string UserId { get; private set; }

        public string FavoritesId { get; private set; }

        public string ExquisiteEdmId { get; private set; }
    }
}