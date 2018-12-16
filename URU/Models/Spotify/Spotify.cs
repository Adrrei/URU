using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace URU.Models
{
    public class Spotify
    {
        private readonly IConfiguration _configuration;

        public Spotify(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetAccessToken()
        {
            var sectionSpotify = _configuration.GetSection("Spotify");
            var clientId = sectionSpotify["ClientId"];
            var clientSecret = sectionSpotify["ClientSecret"];
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", clientId, clientSecret)));

            try
            {
                WebRequest webRequest = WebRequest.Create("https://accounts.spotify.com/api/token");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Headers.Add("Authorization: Basic " + base64Credentials);

                var request = ("grant_type=client_credentials");
                byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                webRequest.ContentLength = requestBytes.Length;

                Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (Stream streamResponse = webResponse.GetResponseStream())
                    {
                        using (StreamReader streamReader = new StreamReader(streamResponse))
                        {
                            string serverResponse = streamReader.ReadToEnd();
                            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(serverResponse);
                            streamReader.Close();

                            return (string)jsonResponse["access_token"];
                        }
                    }
                }
            }
            catch (WebException)
            {
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public enum Method
        {
            GetPlaylist,
            GetPlaylists,
            GetPlaylistTracks
        }

        public string GetPlaylist(User user, string parameters)
        {
            return $"https://api.spotify.com/v1/users/{user.UserId}/playlists/{user.PlaylistId}" + parameters;
        }

        public string GetPlaylists(User user)
        {
            return $"https://api.spotify.com/v1/users/{user.UserId}/playlists?limit=50";
        }

        public string GetPlaylistTracks(User user, string parameters)
        {
            return $"https://api.spotify.com/v1/playlists/{user.PlaylistId}/tracks" + parameters;
        }

        public T GetSpotify<T>(User user, Method method, string parameters = "")
        {
            if (user == null)
            {
                return default;
            }

            try
            {
                string accessToken = GetAccessToken();
                user.Token = accessToken;

                var sectionSpotify = _configuration.GetSection("Spotify");
                var redirectUri = sectionSpotify["RedirectUri"];

                string url = "";
                switch (method)
                {
                    case Method.GetPlaylist:
                        url = GetPlaylist(user, parameters);
                        break;

                    case Method.GetPlaylists:
                        url = GetPlaylists(user);
                        break;

                    case Method.GetPlaylistTracks:
                        url = GetPlaylistTracks(user, parameters);
                        break;

                    default:
                        break;
                }

                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Method = "GET";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Headers.Add("Authorization: Bearer " + accessToken);

                T type = default;

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (Stream streamResponse = webResponse.GetResponseStream())
                    {
                        using (StreamReader streamReader = new StreamReader(streamResponse))
                        {
                            string serverResponse = streamReader.ReadToEnd();
                            type = JsonConvert.DeserializeObject<T>(serverResponse);
                            streamReader.Close();
                        }
                    }
                }
                return type;
            }
            catch (WebException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Playlist> GetPlaylists<Playlist>(User user)
        {
            if (user == null)
            {
                return default;
            }

            try
            {
                string accessToken = GetAccessToken();
                user.Token = accessToken;

                var sectionSpotify = _configuration.GetSection("Spotify");
                var redirectUri = sectionSpotify["RedirectUri"];

                Playlist playlist = default;

                var method = GetPlaylistTracks(user, "?offset=" + user.Offset + "&limit=" + user.Limit);
                WebRequest webRequest = WebRequest.Create(method);
                webRequest.Method = "GET";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Headers.Add("Authorization: Bearer " + accessToken);

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (Stream streamResponse = webResponse.GetResponseStream())
                    {
                        using (StreamReader streamReader = new StreamReader(streamResponse))
                        {
                            string serverResponse = await streamReader.ReadToEndAsync();
                            playlist = JsonConvert.DeserializeObject<Playlist>(serverResponse);
                            streamReader.Close();
                        }
                    }
                }
                return playlist;
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<long> GetSpotifyPlaytime<T>(User user)
        {
            if (user == null)
            {
                return default;
            }

            long milliseconds = 0;

            try
            {
                string accessToken = GetAccessToken();
                user.Token = accessToken;

                var sectionSpotify = _configuration.GetSection("Spotify");
                var redirectUri = sectionSpotify["RedirectUri"];

                Playlist playlist = default;
                long numTracks = 1;

                while (user.Offset < numTracks)
                {
                    string url = GetPlaylistTracks(user, "?offset=" + user.Offset);
                    WebRequest webRequest = WebRequest.Create(url);
                    webRequest.Method = "GET";
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Headers.Add("Authorization: Bearer " + accessToken);

                    using (WebResponse webResponse = webRequest.GetResponse())
                    {
                        using (Stream streamResponse = webResponse.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(streamResponse))
                            {
                                string serverResponse = await streamReader.ReadToEndAsync();
                                playlist = JsonConvert.DeserializeObject<Playlist>(serverResponse);

                                foreach (var track in playlist.Items)
                                {
                                    milliseconds += track.Track.DurationMs;
                                }
                                if (numTracks == 1)
                                {
                                    numTracks = playlist.Total;
                                }
                                user.Offset += 100;
                                streamReader.Close();
                            }
                        }
                    }
                }

                return milliseconds;
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}