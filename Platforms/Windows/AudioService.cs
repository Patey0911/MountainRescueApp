using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;


namespace MountainRescueApp.Platforms.Windows;

public class AudioService : IAudioService
{
    public async void PlayAlert()
    {
        var player = new MediaPlayerElement();
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///alarm.mp3"));
        player.Source = MediaSource.CreateFromStorageFile(file);
        player.MediaPlayer.Play();
    }
}
