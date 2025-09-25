using System;
using System.Collections.Generic;
using System.Drawing;
using OpenCvSharp;

namespace UVCCameraControl.Interfaces
{
    /// <summary>
    /// Interface for camera image capture backends (OpenCV, DirectShow, etc.)
    /// </summary>
    public interface ICameraCaptureEngine : IDisposable
    {
        /// <summary>
        /// Indicates if the capture engine is connected and ready to capture
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Camera index being used for capture
        /// </summary>
        int CameraIndex { get; }

        /// <summary>
        /// Device name for the current camera
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Initialize the capture engine with a specific camera
        /// </summary>
        /// <param name="cameraIndex">Index of the camera device</param>
        /// <param name="deviceName">Optional device name for reference</param>
        /// <returns>True if initialization successful</returns>
        bool Initialize(int cameraIndex, string deviceName = "");

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

        /// <summary>
        /// Set the capture frame size
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
        /// Get supported resolutions for the current device
        /// </summary>
        /// <returns>List of supported resolutions as (width, height) tuples</returns>
        List<(int width, int height)> GetSupportedResolutions();

        /// <summary>
        /// Get a human-readable name for this capture engine type
        /// </summary>
        string EngineName { get; }
    }
}