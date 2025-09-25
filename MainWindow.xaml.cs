using System;
using System.Windows;
using System.Windows.Media;
using UVCCameraControl.Services;
using UVCCameraControl.ViewModels;

namespace UVCCameraControl
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var cameraService = new CameraService();
            _viewModel = new MainViewModel(cameraService);
            DataContext = _viewModel;

            // Wire up preview element to MediaCapture
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load available cameras when window loads
            if (_viewModel.LoadCamerasCommand.CanExecute(null))
            {
                _viewModel.LoadCamerasCommand.Execute(null);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsPreviewActive))
            {
                UpdatePreviewElement();
            }
        }

        private void UpdatePreviewElement()
        {
            // Preview is now handled by CameraPreviewControl in XAML
            // No additional setup needed
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up resources when window closes
            if (_viewModel?.StopPreviewCommand?.CanExecute(null) == true)
            {
                _viewModel.StopPreviewCommand.Execute(null);
            }
            base.OnClosed(e);
        }
    }
}