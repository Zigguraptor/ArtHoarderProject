using System.Windows;
using System.Windows.Controls;

namespace ArtHoarderClient.Infrastructure;

public static class ScrollViewerHelper
{
    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(ScrollViewerHelper), new PropertyMetadata(false, OnAutoScrollToEndChanged));

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer && (bool)e.NewValue)
        {
            scrollViewer.ScrollToEnd();
        }
    }
}
