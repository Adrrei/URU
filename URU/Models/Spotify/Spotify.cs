using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public async Task<long> GetSpotifyPlaytime<T>(User user, long numberOfSongs)
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


                var client = new HttpClient();

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                IList <HttpRequestMessage> urls = new List<HttpRequestMessage>();

                while (user.Offset < numberOfSongs)
                {
                    string spotifyUrl = GetPlaylistTracks(user, "?offset=" + user.Offset + "&fields=items(track(duration_ms))");
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri(spotifyUrl),
                        Method = HttpMethod.Get,
                        Headers = {
                            { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
                        }
                    };

                    urls.Add(httpRequestMessage);
                    user.Offset += 100;
                }

                var requests = urls.Select(url => client.GetAsync(url.RequestUri));

                var responses = requests.Select
                    (
                        task => task.Result
                    );

                foreach (var response in responses)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(content);

                    foreach (var track in playlist.Items)
                    {
                        milliseconds += track.Track.DurationMs;
                    }
                }

                return milliseconds;
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}