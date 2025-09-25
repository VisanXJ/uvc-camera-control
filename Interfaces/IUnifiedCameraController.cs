using System;
using System.Collections.Generic;
using System.Drawing;
using OpenCvSharp;

namespace UVCCameraControl.Interfaces
{
    /// <summary>
    /// Unified interface for complete camera control combining parameter control and image capture
    /// </summary>
    public interface IUnifiedCameraController : IDisposable
    {
        /// <summary>
        /// Indicates if the camera controller is fully connected and ready for operation
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Camera index being used
        /// </summary>
        int CameraIndex { get; }

        /// <summary>
        /// Device name for the current camera
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// The parameter controller backend being used (e.g., "DirectShow", "V4L2")
        /// </summary>
        string ParameterControllerType { get; }

        /// <summary>
        /// The capture engine backend being used (e.g., "OpenCV", "DirectShow")
        /// </summary>
        string CaptureEngineType { get; }

        /// <summary>
        /// Initialize the camera controller with both parameter control and image capture
        /// </summary>
        /// <param name="cameraIndex">Index of the camera device</param>
        /// <param name="deviceName">Optional device name for reference</param>
        /// <returns>True if initialization successful</returns>
        bool Initialize(int cameraIndex, string deviceName = "");

        // Image Capture Methods
        /// <summary>
        /// Capture a single frame as OpenCV Mat
        /// </summary>
        /// <returns>Captured frame or null if capture failed</returns>
        Mat? CaptureFrame();

        /// <summary>
        /// Capture a single frame as System.Drawing.Bitmap
        /// </summary>
        /// <returns>Captured frame or null if capture failed</returns>
        Bitmap? CaptureBitmap();

        // Video Format Control Methods
        /// <summary>
        /// Set the capture frame size with fallback support
        /// </summary>
        /// <param name="width">Frame width in pixels</param>
        /// <param name="height">Frame height in pixels</param>
        /// <returns>True if frame size was set successfully</returns>
        bool SetFrameSize(int width, int height);

        /// <summary>
        /// Get the current frame size
        /// </summary>
        /// <returns>Tuple containing (width, height)</returns>
        (int width, int height) GetFrameSize();

        /// <summary>
        /// Set the capture frame rate
        /// </summary>
        /// <param name="fps">Frames per second</param>
        /// <returns>True if frame rate was set successfully</returns>
        bool SetFPS(double fps);

        /// <summary>
        /// Get the current frame rate
        /// </summary>
        /// <returns>Current frames per second</returns>
        double GetFPS();

        /// <summary>
        /// Get supported resolutions from all available sources
        /// </summary>
        /// <returns>List of supported resolutions as (width, height) tuples</returns>
        List<(int width, int height)> GetSupportedResolutions();

        // Camera Parameter Control Methods
        /// <summary>
        /// Set a camera property value
        /// </summary>
        /// <param name="property">The camera property to set</param>
        /// <param name="value">The value to set</param>
        /// <param name="isAuto">Whether to enable automatic control for this property</param>
        /// <returns>True if the property was set successfully</returns>
        bool SetCameraProperty(CameraProperty property, int value, bool isAuto = false);

        /// <summary>
        /// Get the current value of a camera property
        /// </summary>
        /// <param name="property">The camera property to get</param>
        /// <returns>Tuple containing (value, isAuto, success)</returns>
        (int value, bool isAuto, bool success) GetCameraProperty(CameraProperty property);

        /// <summary>
        /// Get the valid range for a camera property
        /// </summary>
        /// <param name="property">The camera property to query</param>
        /// <returns>Tuple containing (min, max, step, defaultValue, success)</returns>
        (int min, int max, int step, int defaultValue, bool success) GetCameraPropertyRange(CameraProperty property);

        // Device Enumeration Methods
        /// <summary>
        /// Get a list of available camera devices
        /// </summary>
        /// <returns>List of device names</returns>
        static abstract List<string> GetAvailableCameras();
    }
}