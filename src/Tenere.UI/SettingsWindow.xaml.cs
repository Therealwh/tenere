using System.Windows;
using Tenere.UI.ViewModels;

namespace Tenere.UI;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close();
        InitializeComponent();
    }
}