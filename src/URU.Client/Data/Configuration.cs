namespace URU.Client.Data
{
    public class ClientConfiguration
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public ClientConfiguration(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }

    public class SpotifyConfiguration
    {
        public string UserId { get; set; } = "11157411586";

        public string FavoritesId { get; set; } = "48HcflR8QplI2zgAutNDnT";

        public string ExquisiteEdmId { get; set; } = "7ssZYYankNsiAfeyPATtXe";
    }
}