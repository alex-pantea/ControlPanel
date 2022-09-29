using ControlPanel.Core.Entities;
using Newtonsoft.Json;

namespace ControlPanel.Core.Helpers
{
    public class ClientHelper
    {
        private readonly HttpClient _client = new();
        private readonly CancellationToken _stoppingToken;

        public ClientHelper() { }

        public ClientHelper(Uri baseAddress, string bearerToken, CancellationToken stoppingToken)
        {
            _client = new() { BaseAddress = baseAddress };
            _client.DefaultRequestHeaders.Authorization = new("Bearer", bearerToken);
            _stoppingToken = stoppingToken;
        }

        public void GetStatus(out Player player, out Track track)
        {
            player = null!;
            track = null!;

            HttpResponseMessage response = _client.GetAsync("query", _stoppingToken).Result;
            if (!response.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                string result = response.Content.ReadAsStringAsync(_stoppingToken).Result;
                if (result != null && !string.IsNullOrEmpty(result))
                {
                    string p = result.Substring(result.IndexOf("{", 1), result.IndexOf("}") - result.IndexOf("{", 1) + 1);
                    string t = result[(result.IndexOf("\"track\":") + 8)..^1];
                    player = JsonConvert.DeserializeObject<Player>(p)!;
                    track = JsonConvert.DeserializeObject<Track>(t)!;
                }
                return;
            }
        }

        public Track GetTrack()
        {
            HttpResponseMessage response = _client.GetAsync("query/track", _stoppingToken).Result;
            if (response == null)
            {
                return null!;
            }
            return JsonConvert.DeserializeObject<Track>(response.Content.ReadAsStringAsync().Result)!;
        }

        public Player GetPlayer()
        {
            HttpResponseMessage response = _client.GetAsync("query/player", _stoppingToken).Result;
            if (response == null)
            {
                return null!;
            }
            return JsonConvert.DeserializeObject<Player>(response.Content.ReadAsStringAsync().Result)!;
        }

        public async void PauseTrack()
        {
            await _client.PostAsync("query", new StringContent("{\"command\":\"track-pause\"}"));
        }

        public async void PlayTrack()
        {
            await _client.PostAsync("query", new StringContent("{\"command\":\"track-play\"}"));
        }

        public async void PreviousTrack()
        {
            await _client.PostAsync("query", new StringContent("{\"command\":\"track-previous\"}"));
        }

        public async void NextTrack()
        {
            await _client.PostAsync("query", new StringContent("{\"command\":\"track-next\"}"));
        }

        public async void ThumbsUpTrack()
        {
            await _client.PostAsync("query", new StringContent("{\"command\":\"track-thumbs-up\"}"));
        }

        public async void ThumbsDownTrack()
        {
            await _client.PostAsync("query", new StringContent("{\"command\":\"track-thumbs-down\"}"));
        }

        public async void SetVolume(double volume)
        {
            await _client.PostAsync("query", new StringContent($"{{\"command\":\"player-set-volume\",\"value\":\"{volume}\"}}"));
        }
    }
}
