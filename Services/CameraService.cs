using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UVCCameraControl.Models;
using UVCCameraControl.Improved;
using System.Windows.Threading;
using System.Windows;

namespace UVCCameraControl.Services
{
    public class CameraService : ICameraService
    {
        private ImprovedCameraManager? _cameraManager;
        private DispatcherTimer? _frameTimer;
        private bool _isPreviewActive = false;

        public bool IsConnected => _cameraManager?.IsConnected == true;

        public event EventHandler<BitmapSource>? FrameCaptured;

        public async Task<ObservableCollection<CameraDevice>> GetAvailableCamerasAsync()
        {
            await Task.CompletedTask; // Make it async

            var cameras = new ObservableCollection<CameraDevice>();

            try
            {
                var availableCameras = ImprovedCameraManager.GetAvailableCameras();

                for (int i = 0; i < availableCameras.Count; i++)
                {
                    cameras.Add(new CameraDevice
                    {
                        Id = i.ToString(),
                        Name = availableCameras[i],
                        Location = "New Architecture (DirectShow + OpenCV)",
                        IsEnabled = true
                    });
                }

                System.Diagnostics.Debug.WriteLine($"CameraService: Found {cameras.Count} cameras using new architecture");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Error getting cameras: {ex.Message}");
            }

            return cameras;
        }

        public async Task<bool> InitializeCameraAsync(string deviceId)
        {
            await Task.CompletedTask; // Make it async

            try
            {
                if (!int.TryParse(deviceId, out int cameraIndex))
                {
                    System.Diagnostics.Debug.WriteLine($"CameraService: Invalid device ID: {deviceId}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"CameraService: Initializing camera with index {cameraIndex} using new architecture");

                _cameraManager?.Dispose();
                _cameraManager = new ImprovedCameraManager();

                bool success = _cameraManager.Initialize(cameraIndex);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"CameraService: Camera initialized successfully");
                    System.Diagnostics.Debug.WriteLine($"CameraService: {_cameraManager.GetArchitectureInfo()}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CameraService: Failed to initialize camera");
                    _cameraManager.Dispose();
                    _cameraManager = null;
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception during initialization: {ex.Message}");
                _cameraManager?.Dispose();
                _cameraManager = null;
                return false;
            }
        }

        public async Task<bool> StartPreviewAsync()
        {
            await Task.CompletedTask; // Make it async

            if (_cameraManager == null || !_cameraManager.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("CameraService: Cannot start preview - camera not connected");
                return false;
            }

            if (_isPreviewActive)
            {
                System.Diagnostics.Debug.WriteLine("CameraService: Preview already active");
                return true;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("CameraService: Starting preview using new architecture");

                // Create timer on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _frameTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
                    };
                    _frameTimer.Tick += OnFrameTimerTick;
                    _frameTimer.Start();
                });

                _isPreviewActive = true;
                System.Diagnostics.Debug.WriteLine("CameraService: Preview started successfully with new architecture");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception starting preview: {ex.Message}");
                _isPreviewActive = false;
                return false;
            }
        }

        public async Task StopPreviewAsync()
        {
            await Task.CompletedTask; // Make it async

            try
            {
                System.Diagnostics.Debug.WriteLine("CameraService: Stopping preview");

                if (_frameTimer != null)
                {
                    _frameTimer.Stop();
                    _frameTimer.Tick -= OnFrameTimerTick;
                    _frameTimer = null;
                }

                _isPreviewActive = false;
                System.Diagnostics.Debug.WriteLine("CameraService: Preview stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception stopping preview: {ex.Message}");
            }
        }

        public async Task StopPreviewOnlyAsync()
        {
            await Task.CompletedTask; // Make it async

            try
            {
                System.Diagnostics.Debug.WriteLine("CameraService: Stopping preview only (keeping camera manager)");

                if (_frameTimer != null)
                {
                    _frameTimer.Stop();
                    _frameTimer.Tick -= OnFrameTimerTick;
                    _frameTimer = null;
                }

                _isPreviewActive = false;
                System.Diagnostics.Debug.WriteLine("CameraService: Preview stopped (camera manager preserved)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception stopping preview only: {ex.Message}");
            }
        }

        private void OnFrameTimerTick(object? sender, EventArgs e)
        {
            try
            {
                if (_cameraManager == null || !_isPreviewActive)
                    return;

                using var bitmap = _cameraManager.CaptureBitmap();
                if (bitmap != null)
                {
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    bitmapSource.Freeze();
                    FrameCaptured?.Invoke(this, bitmapSource);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception in frame capture: {ex.Message}");
            }
        }

        public async Task<CameraSettings> GetCameraSettingsAsync()
        {
            await Task.CompletedTask; // Make it async

            var settings = new CameraSettings();

            if (_cameraManager == null || !_cameraManager.IsConnected)
                return settings;

            try
            {
                // Get current frame size and FPS
                var (width, height) = _cameraManager.GetFrameSize();
                settings.Width = width;
                settings.Height = height;
                settings.FrameRate = _cameraManager.GetFPS();

                // Get camera properties using new architecture
                var (brightness, _, brightSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Brightness);
                if (brightSuccess) settings.Brightness = brightness;

                var (contrast, _, contrastSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Contrast);
                if (contrastSuccess) settings.Contrast = contrast;

                var (saturation, _, satSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Saturation);
                if (satSuccess) settings.Saturation = saturation;

                var (hue, _, hueSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Hue);
                if (hueSuccess) settings.Hue = hue;

                var (gamma, _, gammaSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Gamma);
                if (gammaSuccess) settings.Gamma = gamma / 100.0; // Convert to 0-3 range

                var (wb, wbAuto, wbSuccess) = _cameraManager.GetCameraProperty(CameraProperty.WhiteBalance);
                if (wbSuccess)
                {
                    settings.WhiteBalance = wb;
                    settings.AutoWhiteBalance = wbAuto;
                }

                var (exposure, expAuto, expSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Exposure);
                if (expSuccess)
                {
                    settings.Exposure = exposure; // Use raw DirectShow value
                    settings.AutoExposure = expAuto;
                }

                var (focus, focusAuto, focusSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Focus);
                if (focusSuccess)
                {
                    settings.Focus = focus; // Use raw DirectShow value
                    settings.AutoFocus = focusAuto;
                }

                var (zoom, _, zoomSuccess) = _cameraManager.GetCameraProperty(CameraProperty.Zoom);
                if (zoomSuccess) settings.Zoom = zoom; // Use raw DirectShow value

                System.Diagnostics.Debug.WriteLine($"CameraService: Retrieved camera settings using new architecture");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception getting camera settings: {ex.Message}");
            }

            return settings;
        }

        public async Task SetCameraSettingsAsync(CameraSettings settings)
        {
            await Task.CompletedTask; // Make it async

            if (_cameraManager == null || !_cameraManager.IsConnected)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Setting camera settings using new architecture");

                // Check if resolution or frame rate needs to change
                var (currentWidth, currentHeight) = _cameraManager.GetFrameSize();
                var currentFps = _cameraManager.GetFPS();

                bool needsRestart = (currentWidth != settings.Width ||
                                   currentHeight != settings.Height ||
                                   Math.Abs(currentFps - settings.FrameRate) > 0.1);

                if (needsRestart && _isPreviewActive)
                {
                    System.Diagnostics.Debug.WriteLine("CameraService: Resolution/FPS change needed, restarting preview");
                    await StopPreviewOnlyAsync();

                    // Set new resolution and frame rate (with DirectShow fallback)
                    _cameraManager.SetFrameSize(settings.Width, settings.Height);
                    _cameraManager.SetFPS(settings.FrameRate);

                    await StartPreviewAsync();
                }

                // Set camera properties using new architecture (DirectShow parameter control)
                _cameraManager.SetCameraProperty(CameraProperty.Brightness, (int)settings.Brightness);
                _cameraManager.SetCameraProperty(CameraProperty.Contrast, (int)settings.Contrast);
                _cameraManager.SetCameraProperty(CameraProperty.Saturation, (int)settings.Saturation);
                _cameraManager.SetCameraProperty(CameraProperty.Hue, (int)settings.Hue);
                _cameraManager.SetCameraProperty(CameraProperty.Gamma, (int)(settings.Gamma * 100)); // Convert from 0-3 range
                _cameraManager.SetCameraProperty(CameraProperty.WhiteBalance, (int)settings.WhiteBalance, settings.AutoWhiteBalance);
                _cameraManager.SetCameraProperty(CameraProperty.Exposure, settings.Exposure, settings.AutoExposure); // Use raw DirectShow value
                _cameraManager.SetCameraProperty(CameraProperty.Focus, settings.Focus, settings.AutoFocus); // Use raw DirectShow value
                _cameraManager.SetCameraProperty(CameraProperty.Zoom, settings.Zoom); // Use raw DirectShow value

                System.Diagnostics.Debug.WriteLine($"CameraService: Camera settings applied using new architecture");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception setting camera settings: {ex.Message}");
            }
        }

        public async Task<(int width, int height, double frameRate)> GetActualCameraStatusAsync()
        {
            await Task.CompletedTask; // Make it async

            if (_cameraManager == null || !_cameraManager.IsConnected)
                return (0, 0, 0.0);

            try
            {
                var (width, height) = _cameraManager.GetFrameSize();
                var frameRate = _cameraManager.GetFPS();

                System.Diagnostics.Debug.WriteLine($"CameraService: Actual camera status: {width}x{height} @ {frameRate:F1}fps");
                return (width, height, frameRate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception getting actual camera status: {ex.Message}");
                return (0, 0, 0.0);
            }
        }

        public async Task<(int min, int max, int step, int defaultValue, bool success)> GetCameraPropertyRangeAsync(CameraProperty property)
        {
            await Task.CompletedTask; // Make it async

            if (_cameraManager == null || !_cameraManager.IsConnected)
                return (0, 0, 0, 0, false);

            try
            {
                var range = _cameraManager.GetCameraPropertyRange(property);
                System.Diagnostics.Debug.WriteLine($"CameraService: Property {property} range: min={range.min}, max={range.max}, step={range.step}, default={range.defaultValue}, success={range.success}");
                return range;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception getting camera property range for {property}: {ex.Message}");
                return (0, 0, 0, 0, false);
            }
        }

        public void Dispose()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CameraService: Disposing with new architecture");

                _frameTimer?.Stop();
                _frameTimer = null;
                _isPreviewActive = false;

                _cameraManager?.Dispose();
                _cameraManager = null;

                System.Diagnostics.Debug.WriteLine("CameraService: Disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraService: Exception during dispose: {ex.Message}");
            }
        }
    }
}