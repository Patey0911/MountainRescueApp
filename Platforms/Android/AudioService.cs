namespace MountainRescueApp;

public partial class AudioService
{
    partial void PlayAlertPlatform()
    {
        var player = new Android.Media.MediaPlayer();
        var afd = Android.App.Application.Context.Assets.OpenFd("alarm.mp3");

        player.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
        player.Prepare();
        player.Start();
    }
}

