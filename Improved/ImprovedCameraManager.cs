using System;
using System.Collections.Generic;
using System.Drawing;
using OpenCvSharp;
using UVCCameraControl.Interfaces;
using UVCCameraControl.Controllers;

namespace UVCCameraControl.Improved
{
    /// <summary>
    /// Improved Camera Manager using the new unified architecture
    /// This demonstrates proper separation of concerns with DirectShow handling UVC parameters
    /// and OpenCV handling image acquisition and processing
    /// </summary>
    public class ImprovedCameraManager : IDisposable
    {
        private IUnifiedCameraController? _unifiedController;
        private bool _disposed = false;

        public bool IsConnected => _unifiedController?.IsConnected == true;
        public int CameraIndex => _unifiedController?.CameraIndex ?? -1;
        public string DeviceName => _unifiedController?.DeviceName ?? string.Empty;
        public string ParameterControllerType => _unifiedController?.ParameterControllerType ?? "None";
        public string CaptureEngineType => _unifiedController?.CaptureEngineType ?? "None";

        /// <summary>
        /// Initialize the camera manager with the specified camera
        /// </summary>
        /// <param name="cameraIndex">Camera index to use</param>
        /// <param name="deviceName">Optional device name for reference</param>
        /// <returns>True if initialization successful</returns>
        public bool Initialize(int cameraIndex, string deviceName = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ImprovedCameraManager: Initializing with camera index {cameraIndex}");

                // Use the unified controller that properly separates DirectShow parameter control from OpenCV image capture
                _unifiedController = new UnifiedUVCCameraController();

                bool success = _unifiedController.Initialize(cameraIndex, deviceName);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"ImprovedCameraManager: Successfully initialized");
                    System.Diagnostics.Debug.WriteLine($"ImprovedCameraManager: Parameter Controller: {ParameterControllerType}");
                    System.Diagnostics.Debug.WriteLine($"ImprovedCameraManager: Capture Engine: {CaptureEngineType}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ImprovedCameraManager: Failed to initialize unified controller");
                    Cleanup();
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImprovedCameraManager: Exception during initialization: {ex.Message}");
                Cleanup();
                return false;
            }
        }

        // Image Capture Methods - handled by OpenCV engine
        public Mat? CaptureFrame()
        {
            return _unifiedController?.CaptureFrame();
        }

        public Bitmap? CaptureBitmap()
        {
            return _unifiedController?.CaptureBitmap();
        }

        // Video Format Control Methods - handled by OpenCV with DirectShow fallback
        public bool SetFrameSize(int width, int height)
        {
            return _unifiedController?.SetFrameSize(width, height) ?? false;
        }

        public (int width, int height) GetFrameSize()
        {
            return _unifiedController?.GetFrameSize() ?? (0, 0);
        }

        public bool SetFPS(double fps)
        {
            return _unifiedController?.SetFPS(fps) ?? false;
        }

        public double GetFPS()
        {
            return _unifiedController?.GetFPS() ?? 0;
        }

        public List<(int width, int height)> GetSupportedResolutions()
        {
            return _unifiedController?.GetSupportedResolutions() ?? new List<(int, int)>();
        }

        // Camera Parameter Control Methods - handled by DirectShow parameter controller
        public bool SetCameraProperty(CameraProperty property, int value, bool isAuto = false)
        {
            return _unifiedController?.SetCameraProperty(property, value, isAuto) ?? false;
        }

        public (int value, bool isAuto, bool success) GetCameraProperty(CameraProperty property)
        {
            return _unifiedController?.GetCameraProperty(property) ?? (0, false, false);
        }

        public (int min, int max, int step, int defaultValue, bool success) GetCameraPropertyRange(CameraProperty property)
        {
            return _unifiedController?.GetCameraPropertyRange(property) ?? (0, 0, 0, 0, false);
        }

        // Static Device Enumeration
        public static List<string> GetAvailableCameras()
        {
            return UnifiedUVCCameraController.GetAvailableCameras();
        }

        /// <summary>
        /// Get detailed information about the current controller architecture
        /// </summary>
        /// <returns>String describing the current setup</returns>
        public string GetArchitectureInfo()
        {
            if (!IsConnected)
                return "Not connected";

            return $"Camera: {DeviceName} | Parameter Control: {ParameterControllerType} | Image Capture: {CaptureEngineType}";
        }

        private void Cleanup()
        {
            _unifiedController?.Dispose();
            _unifiedController = null;
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

        ~ImprovedCameraManager()
        {
            Dispose(disposing: false);
        }
    }
}