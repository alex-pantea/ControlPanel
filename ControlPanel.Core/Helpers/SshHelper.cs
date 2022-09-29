using Chilkat;
using ControlPanel.Core.Entities;

namespace ControlPanel.Core.Helpers
{
    public class SshHelper
    {
        private readonly Ssh _client = new();
        private readonly string _username = string.Empty;
        private readonly string _password = string.Empty;
        private readonly string _host = string.Empty;
        private readonly int _port;

        public SshHelper() { }

        public SshHelper(string host, int port, string username, string password)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;


            _client.Connect(_host, _port);
            _client.AuthenticatePw(_username, _password);
        }

        public Phone GetPhone()
        {
            try
            {
                string result = SendCommand("vol");
                if (!string.IsNullOrWhiteSpace(result))
                {
                    string[] results = result.Split("\n");
                    return new Phone()
                    {
                        Category = results[0].Trim(),
                        Volume = (int)(float.Parse(results[1].Trim()) * 100)
                    };
                }
            }
            catch (Exception) { }
            return new();
        }

        public void PauseTrack()
        {
            SendCommand("media pause");
        }

        public void PlayTrack()
        {
            SendCommand("media play");
        }

        public void PreviousTrack()
        {
            SendCommand("media prev");
        }

        public void NextTrack()
        {
            SendCommand("media next");
        }

        public void SetVolume(double volume)
        {
            SendCommand($"vol {volume / 100f}");
        }

        private string SendCommand(string text)
        {
            try
            {
                return _client.QuickCommand($"/usr/local/bin/{text}", "ansi");
            }
            catch (Exception) { }
            return string.Empty;
        }
    }
}
