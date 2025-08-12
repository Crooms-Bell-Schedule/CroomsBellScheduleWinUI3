using System;
using System.IO;
using System.Net.Http;
using System.Text;
using ABI.Windows.Media.Playback;
using Microsoft.UI.Xaml.Navigation;
using Windows.Media.Core;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class Livestream
{
    public Livestream()
    {
        InitializeComponent();
    }
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        player.Source = null;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        player.AutoPlay = true;
        player.Source = MediaSource.CreateFromUri(new Uri("https://mikhail.croomssched.tech/bell_live/data.m3u8"));
        player.MediaPlayer.RealTimePlayback = true;
    }
}