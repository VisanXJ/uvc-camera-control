using System;
using System.Collections.Generic;

namespace UVCCameraControl.Interfaces
{
    /// <summary>
    /// Interface for camera parameter control backends (DirectShow, V4L2, etc.)
    /// </summary>
    public interface ICameraParameterController : IDisposable
    {
        /// <summary>
        /// Indicates if the controller is connected to a camera device
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initialize the parameter controller with a specific device
        /// </summary>
        /// <param name="deviceName">Name or identifier of the camera device</param>
        /// <returns>True if initialization successful</returns>
        bool Initialize(string deviceName);

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

        /// <summary>
        /// Set the video format (resolution and color depth)
        /// </summary>
        /// <param name="width">Frame width in pixels</param>
        /// <param name="height">Frame height in pixels</param>
        /// <param name="bitsPerPixel">Color depth in bits per pixel</param>
        /// <returns>True if the format was set successfully</returns>
        bool SetVideoFormat(int width, int height, int bitsPerPixel = 24);

        /// <summary>
        /// Get all supported video formats for the current device
        /// </summary>
        /// <returns>List of supported formats as (width, height, bitsPerPixel) tuples</returns>
        List<(int width, int height, int bpp)> GetSupportedVideoFormats();

        /// <summary>
        /// Get a list of available camera devices for this controller type
        /// </summary>
        /// <returns>List of device names</returns>
        List<string> GetAvailableDevices();

        /// <summary>
        /// Get a human-readable name for this controller type
        /// </summary>
        string ControllerName { get; }
    }
}