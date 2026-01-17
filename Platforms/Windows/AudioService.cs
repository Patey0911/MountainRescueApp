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

public partial class AudioService : IAudioService
{
    public void PlayAlertLoop() => PlayAlertPlatform();
    public void StopAlert() => StopAlertPlatform();

    partial void PlayAlertPlatform();
    partial void StopAlertPlatform();
}

