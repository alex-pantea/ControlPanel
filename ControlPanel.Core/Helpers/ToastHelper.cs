using Microsoft.Toolkit.Uwp.Notifications;
using ControlPanel.Core.Entities;

namespace ControlPanel.Core.Helpers
{
    public class ToastHelper
    {
        private readonly CancellationToken _stoppingToken;
        private readonly HttpClient _imageClient = new();
        private readonly ClientHelper _clientHelper = new();

        public ToastHelper() { }

        public ToastHelper(ClientHelper clientHelper, CancellationToken stoppingToken)
        {
            _imageClient = new HttpClient();
            _stoppingToken = stoppingToken;
            _clientHelper = clientHelper;

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // These actions (aka buttons) aren't working for some reason
            ToastArguments args = ToastArguments.Parse(e.Argument);
            if (args.Contains("action"))
            {
                switch (args["action"])
                {
                    case "like":
                        _clientHelper.ThumbsUpTrack();
                        break;
                    case "dislike":
                        _clientHelper.ThumbsDownTrack();
                        break;
                    default:
                        break;
                }
            }
        }

        public void ShowToast(Player player, Track track, string titlePrefix = "", int duration = -1)
        {
            ToastNotificationManagerCompat.History.Clear();

            LoadImage(track, out string path);

            titlePrefix += titlePrefix.Length == 0 ? string.Empty : " - ";

            var toastBuilder = new ToastContentBuilder()
                .AddAppLogoOverride(new(path))
                .AddText($"{titlePrefix}{track.Title}")
                .AddText($"{track.Author} • {track?.Album}")
                .AddAudio(null, false, true)
                // Like/Dislike buttons not working atm from toast
                //.AddButton(new ToastButton()
                //    .SetContent("")
                //    .AddArgument("action", "like")
                //    .SetImageUri(new Uri("assets/img/thumb-up-outline.png", UriKind.Relative)))
                //.AddButton(new ToastButton()
                //    .SetContent("")
                //    .AddArgument("action", "dislike")
                //    .SetImageUri(new Uri("assets/img/thumb-down-outline.png", UriKind.Relative)))
                ;
            toastBuilder.Show(toast =>
            {
                if (duration >= 0)
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(duration == 0 ? track!.Duration - player.SeekbarCurrentPosition : duration);
                }
            });

            // Sleep the thread to let Windows display the image in the notification
            Thread.Sleep(1000);
            if (!string.IsNullOrWhiteSpace(path))
            {
                File.Delete(path);
            }
        }

        private void LoadImage(Track track, out string path)
        {
            path = Path.GetTempFileName();

            try
            {
                var image = _imageClient.GetAsync(track!.Cover, _stoppingToken).Result.Content.ReadAsByteArrayAsync(_stoppingToken).Result;
                File.WriteAllBytes(path, image);
            }
            catch { };
        }
    }
}
