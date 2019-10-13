using URU.Models;

namespace URU.ViewModels
{
    public class SpotifyViewModel
    {
        public User User { get; set; }

        public SpotifyViewModel(User user)
        {
            User = user;
        }
    }
}