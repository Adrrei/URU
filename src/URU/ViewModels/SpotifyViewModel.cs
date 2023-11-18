using URU.Models;

namespace URU.ViewModels
{
    public class SpotifyViewModel(User user)
    {
        public User User { get; set; } = user;
    }
}