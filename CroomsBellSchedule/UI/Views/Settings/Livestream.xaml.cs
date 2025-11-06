using System;
using Microsoft.UI.Xaml.Navigation;
using Windows.Media.Core;

namespace CroomsBellSchedule.UI.Views.Settings;

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
        player.MediaPlayer.IsMuted = true;
    }
}