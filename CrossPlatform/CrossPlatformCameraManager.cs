using System;
using System.Runtime.InteropServices;
using UVCCameraControl.Interfaces;
using UVCCameraControl.Controllers;
using UVCCameraControl.Linux;

namespace UVCCameraControl.CrossPlatform
{
    /// <summary>
    /// 跨平台相机管理器 - 演示如何根据操作系统选择不同的后端
    /// </summary>
    public class CrossPlatformCameraManager : IDisposable
    {
        private IUnifiedCameraController? _controller;
        private bool _disposed = false;

        public bool IsConnected => _controller?.IsConnected == true;
        public string CurrentPlatform { get; private set; } = "";
        public string ParameterBackend => _controller?.ParameterControllerType ?? "None";
        public string CaptureBackend => _controller?.CaptureEngineType ?? "None";

        /// <summary>
        /// 根据当前操作系统自动选择合适的相机控制器
        /// </summary>
        public bool Initialize(int cameraIndex, string deviceName = "")
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    CurrentPlatform = "Windows";
                    System.Diagnostics.Debug.WriteLine("CrossPlatformCameraManager: Using Windows DirectShow + OpenCV");

                    // 使用DirectShow参数控制 + OpenCV图像采集
                    _controller = new UnifiedUVCCameraController();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    CurrentPlatform = "Linux";
                    System.Diagnostics.Debug.WriteLine("CrossPlatformCameraManager: Using Linux V4L2 + OpenCV");

                    // 创建Linux版本的统一控制器
                    _controller = CreateLinuxController();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    CurrentPlatform = "macOS";
                    System.Diagnostics.Debug.WriteLine("CrossPlatformCameraManager: Using macOS AVFoundation + OpenCV");

                    // 创建macOS版本的统一控制器
                    _controller = CreateMacOSController();
                }
                else
                {
                    CurrentPlatform = "Unknown";
                    System.Diagnostics.Debug.WriteLine("CrossPlatformCameraManager: Unknown platform, using fallback OpenCV-only");

                    // 回退到纯OpenCV实现
                    _controller = CreateOpenCVOnlyController();
                }

                bool success = _controller.Initialize(cameraIndex, deviceName);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"CrossPlatformCameraManager: Successfully initialized on {CurrentPlatform}");
                    System.Diagnostics.Debug.WriteLine($"  Parameter Backend: {ParameterBackend}");
                    System.Diagnostics.Debug.WriteLine($"  Capture Backend: {CaptureBackend}");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrossPlatformCameraManager: Error initializing: {ex.Message}");
                return false;
            }
        }

        // 委托所有方法到底层控制器
        public OpenCvSharp.Mat? CaptureFrame() => _controller?.CaptureFrame();
        public System.Drawing.Bitmap? CaptureBitmap() => _controller?.CaptureBitmap();
        public bool SetFrameSize(int width, int height) => _controller?.SetFrameSize(width, height) ?? false;
        public (int width, int height) GetFrameSize() => _controller?.GetFrameSize() ?? (0, 0);
        public bool SetFPS(double fps) => _controller?.SetFPS(fps) ?? false;
        public double GetFPS() => _controller?.GetFPS() ?? 0;
        public bool SetCameraProperty(CameraProperty property, int value, bool isAuto = false) =>
            _controller?.SetCameraProperty(property, value, isAuto) ?? false;
        public (int value, bool isAuto, bool success) GetCameraProperty(CameraProperty property) =>
            _controller?.GetCameraProperty(property) ?? (0, false, false);

        private IUnifiedCameraController CreateLinuxController()
        {
            // 注意：这个实现需要完整的V4L2 UnifiedController
            // 这里只是演示结构，实际需要实现LinuxUnifiedCameraController
            System.Diagnostics.Debug.WriteLine("Creating Linux V4L2 controller (placeholder)");

            // 暂时回退到OpenCV-only，实际项目中需要实现完整的Linux版本
            return CreateOpenCVOnlyController();
        }

        private IUnifiedCameraController CreateMacOSController()
        {
            // 注意：这个实现需要完整的AVFoundation UnifiedController
            System.Diagnostics.Debug.WriteLine("Creating macOS AVFoundation controller (placeholder)");

            // 暂时回退到OpenCV-only，实际项目中需要实现完整的macOS版本
            return CreateOpenCVOnlyController();
        }

        private IUnifiedCameraController CreateOpenCVOnlyController()
        {
            // 这是一个简化版本，只使用OpenCV进行图像采集，参数控制功能受限
            System.Diagnostics.Debug.WriteLine("Creating OpenCV-only controller");

            // 实际项目中需要实现OpenCVOnlyUnifiedController
            // 暂时使用Windows版本作为回退
            return new UnifiedUVCCameraController();
        }

        /// <summary>
        /// 获取跨平台相机信息
        /// </summary>
        public string GetSystemInfo()
        {
            return $"Platform: {CurrentPlatform} | Parameter: {ParameterBackend} | Capture: {CaptureBackend}";
        }

        /// <summary>
        /// 静态方法：获取适合当前平台的可用相机
        /// </summary>
        public static System.Collections.Generic.List<string> GetAvailableCameras()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return UnifiedUVCCameraController.GetAvailableCameras();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // 实际项目中调用Linux特定的设备枚举
                var v4l2Controller = new V4L2ParameterController();
                return v4l2Controller.GetAvailableDevices();
            }
            else
            {
                // 回退到通用方法
                return UnifiedUVCCameraController.GetAvailableCameras();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _controller?.Dispose();
                _controller = null;
                _disposed = true;
            }
        }
    }
}