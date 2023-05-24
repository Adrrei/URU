using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Globalization;

namespace URU.Models
{
    public enum Method
    {
        GetPlaylist,
        GetPlaylists,
        GetPlaylistTracks,
    }

    public partial class ListedGenres
    {
        public List<string> Genres = new()
        {
            "Bass House",
            "Big Room",
            "Breakbeat",
            "Dance",
            "Deep House",
            "Drum & Bass",
            "Dubstep",
            "Electro House",
            "Electronica / Downtempo",
            "Funky / Groove / Jackin' House",
            "Future Bass",
            "Future House",
            "Glitch Hop",
            "Hard Electronic",
            "House",
            "Indie Dance / Nu Disco",
            "Melodic House",
            "Progressive House",
            "Tech House",
            "Techno",
            "Trance",
            "Trap",
        };
    }

    public partial class Favorites
    {
        [JsonProperty("favorites")]
        public string[] Ids { get; set; }

        public Favorites(string[] ids)
        {
            Ids = ids;
        }
    }

    public partial class Genres
    {
        [JsonProperty("genres")]
        public Dictionary<string, (long, string)> Counts { get; set; }

        public Genres(Dictionary<string, (long, string)> counts)
        {
            Counts = counts;
        }
    }

    public partial class TracksByYear
    {
        [JsonProperty("tracksByYear")]
        public KeyValuePair<string, int>[]? Counts { get; set; }
    }

    public partial class Artists
    {
        [JsonProperty("artists")]
        public Dictionary<string, (int, string)>? Counts { get; set; }

        [JsonProperty("time")]
        public int Hours { get; set; }

        [JsonProperty("songs")]
        public int Songs { get; set; }
    }

    public partial class Album
    {
        [JsonProperty("release_date")]
        public string? ReleaseDate { get; set; }
    }

    public partial class Item
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("tracks")]
        public Tracks? Tracks { get; set; }

        [JsonProperty("uri")]
        public string? Uri { get; set; }

        [JsonProperty("track", NullValueHandling = NullValueHandling.Ignore)]
        public Track? Track { get; set; }
    }

    public partial class Artist
    {
        [JsonProperty("uri")]
        public string? Uri { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }
    }

    public partial class Playlist
    {
        [JsonProperty("items")]
        public Item[]? Items { get; set; }

        [JsonProperty("tracks")]
        public Tracks? Tracks { get; set; }
    }

    public partial class Track
    {
        [JsonProperty("album")]
        public Album? Album { get; set; }

        [JsonProperty("artists")]
        public Artist[]? Artists { get; set; }

        [JsonProperty("duration_ms")]
        public long DurationMs { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }
    }

    public partial class Tracks
    {
        [JsonProperty("tracks")]
        public Track[]? AllTracks { get; set; }

        [JsonProperty("items")]
        public Item[]? Items { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }

    public partial class User
    {
        public string PlaylistId { get; set; }

        public string UserId { get; set; }

        public long Offset { get; set; } = 0;

        public long Limit { get; set; } = 1;

        public User(string userId, string playlistId)
        {
            UserId = userId;
            PlaylistId = playlistId;
        }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter
                {
                    DateTimeStyles = DateTimeStyles.AssumeUniversal,
                },
            }
        };
    }

    public partial class Playlist
    {
        public static Playlist FromJson(string json) => JsonConvert.DeserializeObject<Playlist>(json, Converter.Settings)!;
    }
}