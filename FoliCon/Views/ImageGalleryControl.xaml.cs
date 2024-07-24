﻿using System.Collections;
using Image = System.Windows.Controls.Image;

namespace FoliCon.Views;

public partial class ImageGalleryControl
{
    private const double Tolerance = 0.001;
    private const double Epsilon = 1E-10;
    
    public ImageGalleryControl()
    {
        InitializeComponent();
    }

    #region Variables

    private bool _autoScroll = true;
    
    public static readonly DependencyProperty CustomBusyContentTemplateProperty =
        DependencyProperty.Register(nameof(CustomBusyContentTemplate), typeof(DataTemplate), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.Register(nameof(DoubleClickCommand), typeof(ICommand), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty BindingPathProperty = 
        DependencyProperty.Register(
            nameof(BindingPath), 
            typeof(string), 
            typeof(ImageGalleryControl), 
            new PropertyMetadata("Url")  // Default binding path
        );
        
    public static readonly DependencyProperty UseCacheConverterProperty =
        DependencyProperty.Register(nameof(UseCacheConverter), typeof(bool), 
            typeof(ImageGalleryControl), 
            new PropertyMetadata(false));
    
    #endregion

    #region Properties

    public DataTemplate CustomBusyContentTemplate
    {
        get => GetValue(CustomBusyContentTemplateProperty) as DataTemplate;
        set => SetValue(CustomBusyContentTemplateProperty, value);
    }
    
    public IEnumerable ItemsSource
    {
        get => GetValue(ItemsSourceProperty) as IEnumerable;
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand DoubleClickCommand
    {
        get => GetValue(DoubleClickCommandProperty) as ICommand;
        set => SetValue(DoubleClickCommandProperty, value);
    }

    public ICommand ClickCommand
    {
        get => GetValue(ClickCommandProperty) as ICommand;
        set => SetValue(ClickCommandProperty, value);
    }

    public string BindingPath
    {
        get => GetValue(BindingPathProperty) as string;
        set => SetValue(BindingPathProperty, value);
    }
    
    public bool UseCacheConverter
    {
        get => GetValue(UseCacheConverterProperty) as bool? ?? false;
        set => SetValue(UseCacheConverterProperty, value);
    }
    #endregion

    #region Methods

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {

        if (Math.Abs(e.ExtentHeightChange) > Epsilon)
        {
            _autoScroll = Math.Abs(ScrollViewer.VerticalOffset - ScrollViewer.ScrollableHeight) < Tolerance;
        }


        if (_autoScroll && Math.Abs(e.ExtentHeightChange) > Epsilon)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
        }
    }
    
    private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
    {
        var image = (Image)sender;
        image.Source = null;
    }

    #endregion
    
}