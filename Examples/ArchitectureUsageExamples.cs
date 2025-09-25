using System;
using System.Collections.Generic;
using UVCCameraControl.Interfaces;
using UVCCameraControl.Controllers;
using UVCCameraControl.Improved;
using UVCCameraControl.Engines;

namespace UVCCameraControl.Examples
{
    /// <summary>
    /// Usage examples demonstrating the improved reusable architecture
    /// Shows how DirectShow handles UVC parameters while OpenCV handles image acquisition/processing
    /// </summary>
    public static class ArchitectureUsageExamples
    {
        /// <summary>
        /// Example 1: Using the ImprovedCameraManager (Recommended approach)
        /// This is the simplest way to use the new architecture
        /// </summary>
        public static void ExampleUsingImprovedCameraManager()
        {
            Console.WriteLine("=== Example 1: Using ImprovedCameraManager ===");

            using var cameraManager = new ImprovedCameraManager();

            // Get available cameras
            var cameras = ImprovedCameraManager.GetAvailableCameras();
            if (cameras.Count == 0)
            {
                Console.WriteLine("No cameras found");
                return;
            }

            // Initialize with first camera
            if (cameraManager.Initialize(0, cameras[0]))
            {
                Console.WriteLine($"Camera initialized: {cameraManager.GetArchitectureInfo()}");

                // Test image capture (handled by OpenCV)
                using var frame = cameraManager.CaptureFrame();
                if (frame != null)
                {
                    Console.WriteLine($"Frame captured: {frame.Width}x{frame.Height}");
                }

                // Test parameter control (handled by DirectShow)
                var (brightness, isAuto, success) = cameraManager.GetCameraProperty(CameraProperty.Brightness);
                if (success)
                {
                    Console.WriteLine($"Current brightness: {brightness} (Auto: {isAuto})");

                    // Set brightness
                    if (cameraManager.SetCameraProperty(CameraProperty.Brightness, brightness + 10))
                    {
                        Console.WriteLine("Brightness adjusted successfully");
                    }
                }

                // Test resolution control (OpenCV with DirectShow fallback)
                var resolutions = cameraManager.GetSupportedResolutions();
                Console.WriteLine($"Supported resolutions: {resolutions.Count}");

                if (resolutions.Count > 1)
                {
                    var newRes = resolutions[1];
                    if (cameraManager.SetFrameSize(newRes.width, newRes.height))
                    {
                        Console.WriteLine($"Resolution set to {newRes.width}x{newRes.height}");
                    }
                }
            }
        }

        /// <summary>
        /// Example 2: Using individual components directly
        /// This shows how to use the interfaces for maximum flexibility
        /// </summary>
        public static void ExampleUsingComponentsDirectly()
        {
            Console.WriteLine("=== Example 2: Using Components Directly ===");

            ICameraParameterController? paramController = null;
            ICameraCaptureEngine? captureEngine = null;

            try
            {
                // Initialize DirectShow parameter controller
                paramController = new UvcCameraController();
                var cameras = paramController.GetAvailableDevices();
                if (cameras.Count == 0)
                {
                    Console.WriteLine("No cameras found");
                    return;
                }

                if (paramController.Initialize(cameras[0]))
                {
                    Console.WriteLine($"Parameter controller initialized: {paramController.ControllerName}");

                    // Initialize OpenCV capture engine
                    captureEngine = new OpenCVCaptureEngine();
                    if (captureEngine.Initialize(0, cameras[0]))
                    {
                        Console.WriteLine($"Capture engine initialized: {captureEngine.EngineName}");

                        // Test parameter control through DirectShow
                        var formats = paramController.GetSupportedVideoFormats();
                        Console.WriteLine($"DirectShow reports {formats.Count} video formats");

                        // Test image capture through OpenCV
                        using var bitmap = captureEngine.CaptureBitmap();
                        if (bitmap != null)
                        {
                            Console.WriteLine($"Bitmap captured: {bitmap.Width}x{bitmap.Height}");
                        }

                        // Test combined resolution setting with fallback
                        bool openCvSuccess = captureEngine.SetFrameSize(1280, 720);
                        if (!openCvSuccess)
                        {
                            Console.WriteLine("OpenCV resolution setting failed, trying DirectShow fallback...");
                            bool directShowSuccess = paramController.SetVideoFormat(1280, 720);
                            Console.WriteLine($"DirectShow fallback result: {directShowSuccess}");
                        }
                        else
                        {
                            Console.WriteLine("OpenCV resolution setting succeeded");
                        }
                    }
                }
            }
            finally
            {
                paramController?.Dispose();
                captureEngine?.Dispose();
            }
        }

        /// <summary>
        /// Example 3: Using the UnifiedUVCCameraController directly
        /// This shows the combined interface in action
        /// </summary>
        public static void ExampleUsingUnifiedController()
        {
            Console.WriteLine("=== Example 3: Using UnifiedUVCCameraController ===");

            using var unifiedController = new UnifiedUVCCameraController();

            var cameras = UnifiedUVCCameraController.GetAvailableCameras();
            if (cameras.Count == 0)
            {
                Console.WriteLine("No cameras found");
                return;
            }

            if (unifiedController.Initialize(0, cameras[0]))
            {
                Console.WriteLine($"Unified controller initialized:");
                Console.WriteLine($"  Device: {unifiedController.DeviceName}");
                Console.WriteLine($"  Parameter Control: {unifiedController.ParameterControllerType}");
                Console.WriteLine($"  Image Capture: {unifiedController.CaptureEngineType}");

                // Test all capabilities through single interface
                using var frame = unifiedController.CaptureFrame();
                if (frame != null)
                {
                    Console.WriteLine($"Frame: {frame.Width}x{frame.Height}");
                }

                var (contrast, isAuto, success) = unifiedController.GetCameraProperty(CameraProperty.Contrast);
                if (success)
                {
                    Console.WriteLine($"Contrast: {contrast} (Auto: {isAuto})");
                }

                var resolutions = unifiedController.GetSupportedResolutions();
                Console.WriteLine($"Total resolutions from both sources: {resolutions.Count}");
            }
        }

        /// <summary>
        /// Run all examples to demonstrate the architecture
        /// </summary>
        public static void RunAllExamples()
        {
            try
            {
                ExampleUsingImprovedCameraManager();
                Console.WriteLine();
                ExampleUsingComponentsDirectly();
                Console.WriteLine();
                ExampleUsingUnifiedController();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running examples: {ex.Message}");
            }
        }

        /// <summary>
        /// Show the benefits of the new architecture
        /// </summary>
        public static void ShowArchitectureBenefits()
        {
            Console.WriteLine("=== New Architecture Benefits ===");
            Console.WriteLine("1. Separation of Concerns:");
            Console.WriteLine("   - DirectShow handles UVC camera parameters (17 properties)");
            Console.WriteLine("   - OpenCV handles image acquisition and processing");
            Console.WriteLine();
            Console.WriteLine("2. Reusable Components:");
            Console.WriteLine("   - ICameraParameterController: Extensible to other backends (V4L2, etc.)");
            Console.WriteLine("   - ICameraCaptureEngine: Extensible to other capture methods");
            Console.WriteLine("   - IUnifiedCameraController: Complete camera control interface");
            Console.WriteLine();
            Console.WriteLine("3. Fallback Mechanisms:");
            Console.WriteLine("   - Resolution setting tries OpenCV first, falls back to DirectShow");
            Console.WriteLine("   - Resolution enumeration combines results from both sources");
            Console.WriteLine();
            Console.WriteLine("4. Easy Integration:");
            Console.WriteLine("   - ImprovedCameraManager provides simple interface for applications");
            Console.WriteLine("   - Direct component access for advanced use cases");
            Console.WriteLine("   - Clear debugging and logging throughout");
        }
    }
}