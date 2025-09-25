using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UVCCameraControl.Services;

namespace UVCCameraControl.Controls
{
    public class CameraPreviewControl : Border
    {
        private ICameraService? _cameraService;
        private Image? _previewImage;
        private bool _isPreviewActive = false;

        public static readonly DependencyProperty MediaCaptureProperty =
            DependencyProperty.Register(nameof(MediaCaptureSource), typeof(object), typeof(CameraPreviewControl),
                new PropertyMetadata(null, OnMediaCaptureChanged));

        public object? MediaCaptureSource
        {
            get => GetValue(MediaCaptureProperty);
            set => SetValue(MediaCaptureProperty, value);
        }

        private static void OnMediaCaptureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraPreviewControl control)
            {
                control.UpdateMediaCapture(e.NewValue);
            }
        }

        public CameraPreviewControl()
        {
            Background = Brushes.Black;
            Loaded += CameraPreviewControl_Loaded;
            Unloaded += CameraPreviewControl_Unloaded;

            // Create image control for displaying frames
            _previewImage = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void CameraPreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_cameraService != null)
            {
                StartPreview();
            }
        }

        private void CameraPreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopPreview();
        }

        private void UpdateMediaCapture(object? mediaCapture)
        {
            // Stop previous preview if any
            StopPreview();

            // Get camera service from MainWindow through the visual tree
            if (mediaCapture != null)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow?.DataContext is ViewModels.MainViewModel viewModel)
                {
                    // Access the camera service through reflection (temporary solution)
                    var cameraServiceField = typeof(ViewModels.MainViewModel).GetField("_cameraService",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    _cameraService = cameraServiceField?.GetValue(viewModel) as ICameraService;
                }

                if (IsLoaded && _cameraService != null)
                {
                    StartPreview();
                }
            }
        }

        private void StartPreview()
        {
            if (_cameraService == null || _isPreviewActive)
                return;

            try
            {
                // Subscribe to frame captured event
                _cameraService.FrameCaptured += OnFrameCaptured;

                // Set the image as child content
                Child = _previewImage;
                _isPreviewActive = true;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to start preview: {ex.Message}");
            }
        }

        private void OnFrameCaptured(object? sender, BitmapSource bitmapSource)
        {
            // This is already called on UI thread by the SampleGrabberCallback
            if (_previewImage != null)
            {
                _previewImage.Source = bitmapSource;
            }
        }

        private void StopPreview()
        {
            if (!_isPreviewActive)
                return;

            try
            {
                // Unsubscribe from frame captured event
                if (_cameraService != null)
                {
                    _cameraService.FrameCaptured -= OnFrameCaptured;
                }

                // Clear the image
                if (_previewImage != null)
                {
                    _previewImage.Source = null;
                }

                Child = null;
                _isPreviewActive = false;
            }
            catch (Exception)
            {
                // Handle preview stop error silently
            }
        }

        private void ShowError(string message)
        {
            var errorText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Child = errorText;
        }
    }
}