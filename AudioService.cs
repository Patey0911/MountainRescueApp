using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp;

public partial class AudioService : IAudioService
{
    public void PlayAlert() => PlayAlertPlatform();

    partial void PlayAlertPlatform();
}
