﻿
using Image = System.Windows.Controls.Image;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for ManualExplorer
/// </summary>
public partial class ManualExplorer : UserControl
{
    private bool _autoScroll = true;
        
    public ManualExplorer()
    {
        InitializeComponent();
    }
        
    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange == 0)
        {
                
            _autoScroll = ScrollViewer.VerticalOffset == ScrollViewer.ScrollableHeight;
        }


        if (_autoScroll && e.ExtentHeightChange != 0)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
        }
    }

    private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
    {
        var image = (Image)sender;
        image.Source = null;
    }
}