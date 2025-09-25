using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenCvSharp;
using DirectShowLib;
using UVCCameraControl.Interfaces;
using UVCCameraControl.Engines;

namespace UVCCameraControl.Controllers
{
    /// <summary>
    /// Unified camera controller that combines DirectShow parameter control with OpenCV image capture
    /// This provides the separation of concerns requested: DirectShow for UVC parameters, OpenCV for image processing
    /// </summary>
    public class UnifiedUVCCameraController : IUnifiedCameraController
    {
        private ICameraParameterController? _parameterController;
        private ICameraCaptureEngine? _captureEngine;
        private bool _disposed = false;

        public bool IsConnected =>
            _parameterController?.IsConnected == true && _captureEngine?.IsConnected == true;

        public int CameraIndex => _captureEngine?.CameraIndex ?? -1;
        public string DeviceName => _captureEngine?.DeviceName ?? string.Empty;
        public string ParameterControllerType => _parameterController?.ControllerName ?? "None";
        public string CaptureEngineType => _captureEngine?.EngineName ?? "None";

        public bool Initialize(int cameraIndex, string deviceName = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Initializing camera index {cameraIndex} with device '{deviceName}'");

                // Initialize parameter controller (DirectShow for UVC parameter control)
                _parameterController = new UvcCameraController();
                string targetDevice = string.IsNullOrEmpty(deviceName)
                    ? GetDeviceNameByIndex(cameraIndex)
                    : deviceName;

                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Target device name: '{targetDevice}'");

                if (!_parameterController.Initialize(targetDevice))
                {
                    System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Failed to initialize parameter controller for device '{targetDevice}'");
                    Cleanup();
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Parameter controller initialized successfully");

                // Initialize capture engine (OpenCV for image acquisition and processing)
                _captureEngine = new OpenCVCaptureEngine();
                if (!_captureEngine.Initialize(cameraIndex, targetDevice))
                {
                    System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Failed to initialize capture engine for index {cameraIndex}");
                    Cleanup();
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Capture engine initialized successfully");
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Unified controller ready - Parameter: {ParameterControllerType}, Capture: {CaptureEngineType}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Exception during initialization: {ex.Message}");
                Cleanup();
                return false;
            }
        }

        private string GetDeviceNameByIndex(int index)
        {
            try
            {
                var dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (dsDevices != null && index < dsDevices.Length)
                {
                    return dsDevices[index].Name;
                }
            }
            catch
            {
            }
            return $"Camera {index}";
        }

        // Image Capture Methods (delegated to OpenCV engine)
        public Mat? CaptureFrame()
        {
            return _captureEngine?.CaptureFrame();
        }

        public Bitmap? CaptureBitmap()
        {
            return _captureEngine?.CaptureBitmap();
        }

        // Video Format Control Methods (with DirectShow fallback)
        public bool SetFrameSize(int width, int height)
        {
            if (_captureEngine == null || _parameterController == null)
                return false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Attempting to set frame size to {width}x{height}");

                // First attempt: Use OpenCV capture engine
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Trying OpenCV method first");
                bool success = _captureEngine.SetFrameSize(width, height);

                // If OpenCV method failed, try DirectShow fallback
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: OpenCV method failed, trying DirectShow parameter controller fallback");

                    // Use DirectShow parameter controller to set video format
                    bool directShowSuccess = _parameterController.SetVideoFormat(width, height);

                    if (directShowSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: DirectShow fallback succeeded");

                        // Verify the setting by checking capture engine
                        var (actualWidth, actualHeight) = _captureEngine.GetFrameSize();
                        System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Frame size after DirectShow setting: {actualWidth}x{actualHeight}");

                        success = (actualWidth == width && actualHeight == height) ||
                                 (Math.Abs(actualWidth - width) <= 1 && Math.Abs(actualHeight - height) <= 1);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: DirectShow fallback also failed");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: SetFrameSize result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Exception setting frame size: {ex.Message}");
                return false;
            }
        }

        public (int width, int height) GetFrameSize()
        {
            return _captureEngine?.GetFrameSize() ?? (0, 0);
        }

        public bool SetFPS(double fps)
        {
            return _captureEngine?.SetFPS(fps) ?? false;
        }

        public double GetFPS()
        {
            return _captureEngine?.GetFPS() ?? 0;
        }

        public List<(int width, int height)> GetSupportedResolutions()
        {
            var resolutions = new List<(int, int)>();

            try
            {
                System.Diagnostics.Debug.WriteLine("UnifiedUVCCameraController: Getting supported resolutions using both DirectShow and OpenCV methods");

                // Method 1: Get resolutions from DirectShow parameter controller
                if (_parameterController != null)
                {
                    var dsFormats = _parameterController.GetSupportedVideoFormats();
                    foreach (var (width, height, bpp) in dsFormats)
                    {
                        if (!resolutions.Contains((width, height)))
                        {
                            resolutions.Add((width, height));
                            System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: DirectShow resolution found: {width}x{height}");
                        }
                    }
                }

                // Method 2: Get resolutions from OpenCV capture engine (additional/fallback)
                if (_captureEngine != null)
                {
                    var cvResolutions = _captureEngine.GetSupportedResolutions();
                    foreach (var resolution in cvResolutions)
                    {
                        if (!resolutions.Contains(resolution))
                        {
                            resolutions.Add(resolution);
                            System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: OpenCV resolution found: {resolution.width}x{resolution.height}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Total supported resolutions found: {resolutions.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnifiedUVCCameraController: Error getting supported resolutions: {ex.Message}");
            }

            return resolutions.Distinct().OrderBy(r => r.Item1 * r.Item2).ToList();
        }

        // Camera Parameter Control Methods (delegated to DirectShow controller)
        public bool SetCameraProperty(CameraProperty property, int value, bool isAuto = false)
        {
            return _parameterController?.SetCameraProperty(property, value, isAuto) ?? false;
        }

        public (int value, bool isAuto, bool success) GetCameraProperty(CameraProperty property)
        {
            return _parameterController?.GetCameraProperty(property) ?? (0, false, false);
        }

        public (int min, int max, int step, int defaultValue, bool success) GetCameraPropertyRange(CameraProperty property)
        {
            return _parameterController?.GetCameraPropertyRange(property) ?? (0, 0, 0, 0, false);
        }

        // Device Enumeration Methods
        public static List<string> GetAvailableCameras()
        {
            var devices = new List<string>();

            try
            {
                // Use DirectShow to get device names (most reliable for UVC cameras on Windows)
                var dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (dsDevices != null && dsDevices.Length > 0)
                {
                    foreach (var device in dsDevices)
                    {
                        devices.Add(device.Name);
                    }
                }
                else
                {
                    // Fallback: enumerate OpenCV devices by testing indices
                    for (int i = 0; i < 10; i++)
                    {
                        using var testCapture = new VideoCapture(i);
                        if (testCapture.IsOpened())
                        {
                            devices.Add($"Camera {i}");
                            testCapture.Release();
                        }
                    }
                }
            }
            catch
            {
                // Final fallback: just add a default camera
                devices.Add("Default Camera (0)");
            }

            return devices;
        }

        private void Cleanup()
        {
            _parameterController?.Dispose();
            _parameterController = null;
            _captureEngine?.Dispose();
            _captureEngine = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~UnifiedUVCCameraController()
        {
            Dispose(disposing: false);
        }
    }
}