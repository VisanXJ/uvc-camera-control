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

        public static readonly DependencyProperty CameraServiceProperty =
            DependencyProperty.Register(nameof(CameraService), typeof(ICameraService), typeof(CameraPreviewControl),
                new PropertyMetadata(null, OnCameraServiceChanged));

        public ICameraService? CameraService
        {
            get => (ICameraService?)GetValue(CameraServiceProperty);
            set => SetValue(CameraServiceProperty, value);
        }

        private static void OnCameraServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraPreviewControl control)
            {
                control.UpdateCameraService(e.NewValue as ICameraService);
            }
        }

        public CameraPreviewControl()
        {
            Background = Brushes.Black;
            Loaded += CameraPreviewControl_Loaded;
            Unloaded += CameraPreviewControl_Unloaded;

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

        private void UpdateCameraService(ICameraService? cameraService)
        {
            StopPreview();

            _cameraService = cameraService;

            if (IsLoaded && _cameraService != null)
            {
                StartPreview();
            }
        }

        private void StartPreview()
        {
            if (_cameraService == null || _isPreviewActive)
            {
                System.Diagnostics.Debug.WriteLine($"CameraPreviewControl: Cannot start preview - CameraService: {_cameraService != null}, IsActive: {_isPreviewActive}");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"CameraPreviewControl: Starting preview, subscribing to FrameCaptured event");
                _cameraService.FrameCaptured += OnFrameCaptured;
                Child = _previewImage;
                _isPreviewActive = true;
                System.Diagnostics.Debug.WriteLine($"CameraPreviewControl: Preview started successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraPreviewControl: Failed to start preview: {ex.Message}");
                ShowError($"Failed to start preview: {ex.Message}");
            }
        }

        private void OnFrameCaptured(object? sender, BitmapSource bitmapSource)
        {
            Dispatcher.Invoke(() =>
            {
                if (_previewImage != null && _isPreviewActive)
                {
                    System.Diagnostics.Debug.WriteLine($"CameraPreviewControl: Received frame, size: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight}");
                    _previewImage.Source = bitmapSource;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CameraPreviewControl: Frame received but preview is not active or image is null");
                }
            });
        }

        private void StopPreview()
        {
            if (!_isPreviewActive)
                return;

            try
            {
                if (_cameraService != null)
                {
                    _cameraService.FrameCaptured -= OnFrameCaptured;
                }

                if (_previewImage != null)
                {
                    _previewImage.Source = null;
                }

                Child = null;
                _isPreviewActive = false;
            }
            catch (Exception)
            {
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