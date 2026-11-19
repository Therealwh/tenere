using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Tenere.Core.Models;
using Tenere.UI.ViewModels;

namespace Tenere.UI;

/// <summary>
/// Interaction logic for MainWindow.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ClipCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (sender is FrameworkElement element &&
            element.DataContext is ClipItem item &&
            DataContext is MainViewModel viewModel)
        {
            viewModel.CopyCommand.Execute(item);
        }
    }
}