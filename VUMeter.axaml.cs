using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HotwordDetectionApp;


/// <summary>
/// A log scale audio quality meter. for use in single channel audio applications , to avoid clipping. see the dyanmics
/// of the response of the audion stream.  takes a array of  float as a signal and generates a log scale meter.
/// the top 10 perfecnt is red and the display is in db or what is standa  for audio meters.
/// </summary>
public partial class VUMeter : UserControl
{
    public VUMeter()
    {
        InitializeComponent();
    }
}