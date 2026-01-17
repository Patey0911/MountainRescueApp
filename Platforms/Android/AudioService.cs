using Android.Media;

namespace MountainRescueApp;

public partial class AudioService
{
    private MediaPlayer _player;

    partial void PlayAlertPlatform()
    {
        StopAlertPlatform();

        _player = new MediaPlayer();
        var afd = Android.App.Application.Context.Assets.OpenFd("Resources/Raw/alarm.mp3");
        _player.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
        _player.Looping = true;
        _player.Prepare();
        _player.Start();
    }

    partial void StopAlertPlatform()
    {
        if (_player != null)
        {
            _player.Stop();
            _player.Release();
            _player.Dispose();
            _player = null;
        }
    }
}
