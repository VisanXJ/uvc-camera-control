using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using DirectShowLib;
using UVCCameraControl.Interfaces;

namespace UVCCameraControl.Engines
{
    public class OpenCVCaptureEngine : ICameraCaptureEngine
    {
        private VideoCapture? _videoCapture;
        private bool _disposed = false;

        public bool IsConnected => _videoCapture?.IsOpened() == true;
        public int CameraIndex { get; private set; } = -1;
        public string DeviceName { get; private set; } = string.Empty;
        public string EngineName => "OpenCV Video Capture Engine";

        public bool Initialize(int cameraIndex, string deviceName = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Attempting to initialize camera index {cameraIndex}");

                // Try DirectShow backend first for better UVC camera support on Windows
                _videoCapture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
                if (!_videoCapture.IsOpened())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: DirectShow backend failed, trying default backend");
                    _videoCapture?.Dispose();
                    _videoCapture = new VideoCapture(cameraIndex);
                }

                if (!_videoCapture.IsOpened())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Failed to open VideoCapture for index {cameraIndex}");
                    _videoCapture?.Dispose();
                    _videoCapture = null;
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: VideoCapture opened successfully for index {cameraIndex}");

                CameraIndex = cameraIndex;
                DeviceName = string.IsNullOrEmpty(deviceName) ? GetDeviceNameByIndex(cameraIndex) : deviceName;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Exception during initialization: {ex.Message}");
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

        public Mat? CaptureFrame()
        {
            if (_videoCapture == null || !_videoCapture.IsOpened())
            {
                System.Diagnostics.Debug.WriteLine("OpenCVCaptureEngine: VideoCapture is null or not opened");
                return null;
            }

            try
            {
                var frame = new Mat();
                if (_videoCapture.Read(frame) && !frame.Empty())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Frame captured, size: {frame.Width}x{frame.Height}, channels: {frame.Channels()}");
                    return frame;
                }

                System.Diagnostics.Debug.WriteLine("OpenCVCaptureEngine: Failed to read frame or frame is empty");
                frame.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Exception in CaptureFrame: {ex.Message}");
                return null;
            }
        }

        public Bitmap? CaptureBitmap()
        {
            System.Diagnostics.Debug.WriteLine("OpenCVCaptureEngine: CaptureBitmap called");
            try
            {
                using var frame = CaptureFrame();
                if (frame == null || frame.Empty())
                {
                    System.Diagnostics.Debug.WriteLine("OpenCVCaptureEngine: No frame available for bitmap conversion");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Converting frame to bitmap, size: {frame.Width}x{frame.Height}");
                return frame.ToBitmap();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Exception in CaptureBitmap: {ex.Message}");
                return null;
            }
        }

        public bool SetFrameSize(int width, int height)
        {
            if (_videoCapture == null || !_videoCapture.IsOpened())
                return false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Attempting to set frame size to {width}x{height}");

                // Get current size for comparison
                var (currentWidth, currentHeight) = GetFrameSize();
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Current frame size: {currentWidth}x{currentHeight}");

                if (currentWidth == width && currentHeight == height)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Frame size already set to {width}x{height}");
                    return true;
                }

                // Set the new frame size
                _videoCapture.Set(VideoCaptureProperties.FrameWidth, width);
                _videoCapture.Set(VideoCaptureProperties.FrameHeight, height);

                // Verify the setting took effect
                var actualWidth = (int)_videoCapture.Get(VideoCaptureProperties.FrameWidth);
                var actualHeight = (int)_videoCapture.Get(VideoCaptureProperties.FrameHeight);

                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Frame size after setting: {actualWidth}x{actualHeight}");

                bool success = (actualWidth == width && actualHeight == height);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Warning - Could not set exact resolution {width}x{height}, got {actualWidth}x{actualHeight}");
                    // Some cameras might not support the exact resolution but set the closest one
                    success = Math.Abs(actualWidth - width) <= 1 && Math.Abs(actualHeight - height) <= 1;
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Exception setting frame size: {ex.Message}");
                return false;
            }
        }

        public (int width, int height) GetFrameSize()
        {
            if (_videoCapture == null || !_videoCapture.IsOpened())
                return (0, 0);

            try
            {
                var width = (int)_videoCapture.Get(VideoCaptureProperties.FrameWidth);
                var height = (int)_videoCapture.Get(VideoCaptureProperties.FrameHeight);
                return (width, height);
            }
            catch
            {
                return (0, 0);
            }
        }

        public bool SetFPS(double fps)
        {
            if (_videoCapture == null || !_videoCapture.IsOpened())
                return false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Attempting to set FPS to {fps}");

                // Get current FPS for comparison
                var currentFps = GetFPS();
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Current FPS: {currentFps}");

                if (Math.Abs(currentFps - fps) < 0.1)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: FPS already set to {fps}");
                    return true;
                }

                // Set the new FPS
                _videoCapture.Set(VideoCaptureProperties.Fps, fps);

                // Verify the setting took effect
                var actualFps = _videoCapture.Get(VideoCaptureProperties.Fps);
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: FPS after setting: {actualFps}");

                bool success = Math.Abs(actualFps - fps) < 0.1;
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Warning - Could not set exact FPS {fps}, got {actualFps}");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Exception setting FPS: {ex.Message}");
                return false;
            }
        }

        public double GetFPS()
        {
            if (_videoCapture == null || !_videoCapture.IsOpened())
                return 0;

            try
            {
                return _videoCapture.Get(VideoCaptureProperties.Fps);
            }
            catch
            {
                return 0;
            }
        }

        public List<(int width, int height)> GetSupportedResolutions()
        {
            var resolutions = new List<(int, int)>();

            if (_videoCapture == null || !_videoCapture.IsOpened())
                return resolutions;

            // Common resolutions to test
            var testResolutions = new[]
            {
                (320, 240),   // QVGA
                (640, 480),   // VGA
                (800, 600),   // SVGA
                (1024, 768),  // XGA
                (1280, 720),  // 720p
                (1280, 960),  // SXGA
                (1600, 1200), // UXGA
                (1920, 1080), // 1080p
                (1280, 1024), // SXGA
            };

            var currentWidth = (int)_videoCapture.Get(VideoCaptureProperties.FrameWidth);
            var currentHeight = (int)_videoCapture.Get(VideoCaptureProperties.FrameHeight);

            try
            {
                foreach (var (width, height) in testResolutions)
                {
                    _videoCapture.Set(VideoCaptureProperties.FrameWidth, width);
                    _videoCapture.Set(VideoCaptureProperties.FrameHeight, height);

                    var actualWidth = (int)_videoCapture.Get(VideoCaptureProperties.FrameWidth);
                    var actualHeight = (int)_videoCapture.Get(VideoCaptureProperties.FrameHeight);

                    if (actualWidth == width && actualHeight == height && !resolutions.Contains((width, height)))
                    {
                        resolutions.Add((width, height));
                        System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Supported resolution found: {width}x{height}");
                    }
                }

                // Restore original resolution
                _videoCapture.Set(VideoCaptureProperties.FrameWidth, currentWidth);
                _videoCapture.Set(VideoCaptureProperties.FrameHeight, currentHeight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCVCaptureEngine: Error testing resolutions: {ex.Message}");
                // Restore original resolution in case of error
                try
                {
                    _videoCapture.Set(VideoCaptureProperties.FrameWidth, currentWidth);
                    _videoCapture.Set(VideoCaptureProperties.FrameHeight, currentHeight);
                }
                catch { }
            }

            return resolutions.OrderBy(r => r.Item1 * r.Item2).ToList();
        }

        private void Cleanup()
        {
            _videoCapture?.Dispose();
            _videoCapture = null;
            CameraIndex = -1;
            DeviceName = string.Empty;
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

        ~OpenCVCaptureEngine()
        {
            Dispose(disposing: false);
        }
    }
}