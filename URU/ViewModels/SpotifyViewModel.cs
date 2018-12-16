using System.Collections.Generic;
using URU.Models;

namespace URU.ViewModels
{
    public class SpotifyViewModel
    {
        public Dictionary<string, long> EdmPlaylists { get; set; }

        public User User { get; set; }
    }
}