using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp;

public partial class AudioService : IAudioService
{
    public void PlayAlertLoop() => PlayAlertPlatform();
    public void StopAlert() => StopAlertPlatform();

    partial void PlayAlertPlatform();
    partial void StopAlertPlatform();
}

