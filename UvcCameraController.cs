using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DirectShowLib;
using UVCCameraControl.Interfaces;

namespace UVCCameraControl
{
    public class UvcCameraController : ICameraParameterController
    {
        private IFilterGraph2? _filterGraph;
        private IGraphBuilder? _graphBuilder;
        private IMediaControl? _mediaControl;
        private IBaseFilter? _captureFilter;
        private IAMStreamConfig? _streamConfig;
        private bool _disposed = false;

        public bool IsConnected { get; private set; } = false;

        public string ControllerName => "DirectShow UVC Parameter Controller";

        public bool Initialize(string deviceName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Attempting to initialize with device '{deviceName}'");

                _filterGraph = (IFilterGraph2)new FilterGraph();
                _graphBuilder = _filterGraph;
                _mediaControl = (IMediaControl)_filterGraph;

                var captureFilter = FindCaptureDevice(deviceName);
                if (captureFilter == null)
                {
                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Failed to find device '{deviceName}'");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Found device '{deviceName}'");

                _captureFilter = captureFilter;

                var hr = _graphBuilder.AddFilter(_captureFilter, "Video Capture");
                if (hr != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Failed to add filter to graph, HR: {hr}");
                    return false;
                }

                var pin = DsFindPin.ByCategory(_captureFilter, PinCategory.Capture, 0);
                if (pin != null)
                {
                    _streamConfig = pin as IAMStreamConfig;
                    Marshal.ReleaseComObject(pin);
                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Found capture pin and stream config");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Warning - No capture pin found");
                }

                IsConnected = true;
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Successfully initialized");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Exception during initialization: {ex.Message}");
                return false;
            }
        }

        private IBaseFilter? FindCaptureDevice(string deviceName)
        {
            DsDevice[]? devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (devices == null)
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: No DirectShow devices found");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"UvcCameraController: Found {devices.Length} DirectShow devices");

            for (int i = 0; i < devices.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Device {i}: '{devices[i].Name}'");
            }

            foreach (var device in devices)
            {
                if (device.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Match found for device '{deviceName}'");
                    var guid = typeof(IBaseFilter).GUID;
                    device.Mon.BindToObject(null, null, ref guid, out object filterObj);
                    return filterObj as IBaseFilter;
                }
            }

            System.Diagnostics.Debug.WriteLine($"UvcCameraController: No exact match found for device '{deviceName}'");
            return null;
        }

        public bool SetCameraProperty(CameraProperty property, int value, bool isAuto = false)
        {
            if (_captureFilter == null || !IsConnected)
                return false;

            try
            {
                var cameraControl = _captureFilter as IAMCameraControl;
                var videoProcAmp = _captureFilter as IAMVideoProcAmp;

                var flags = isAuto ? CameraControlFlags.Auto : CameraControlFlags.Manual;

                switch (property)
                {
                    case CameraProperty.Exposure:
                        return cameraControl?.Set(CameraControlProperty.Exposure, value, flags) == 0;
                    case CameraProperty.Focus:
                        return cameraControl?.Set(CameraControlProperty.Focus, value, flags) == 0;
                    case CameraProperty.Zoom:
                        return cameraControl?.Set(CameraControlProperty.Zoom, value, flags) == 0;
                    case CameraProperty.Pan:
                        return cameraControl?.Set(CameraControlProperty.Pan, value, flags) == 0;
                    case CameraProperty.Tilt:
                        return cameraControl?.Set(CameraControlProperty.Tilt, value, flags) == 0;
                    case CameraProperty.Roll:
                        return cameraControl?.Set(CameraControlProperty.Roll, value, flags) == 0;
                    case CameraProperty.Iris:
                        return cameraControl?.Set(CameraControlProperty.Iris, value, flags) == 0;

                    case CameraProperty.Brightness:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Brightness, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.Contrast:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Contrast, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.Hue:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Hue, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.Saturation:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Saturation, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.Sharpness:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Sharpness, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.Gamma:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Gamma, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.ColorEnable:
                        return videoProcAmp?.Set(VideoProcAmpProperty.ColorEnable, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.WhiteBalance:
                        return videoProcAmp?.Set(VideoProcAmpProperty.WhiteBalance, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.BacklightCompensation:
                        return videoProcAmp?.Set(VideoProcAmpProperty.BacklightCompensation, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                    case CameraProperty.Gain:
                        return videoProcAmp?.Set(VideoProcAmpProperty.Gain, value,
                            isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual) == 0;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public (int value, bool isAuto, bool success) GetCameraProperty(CameraProperty property)
        {
            if (_captureFilter == null || !IsConnected)
                return (0, false, false);

            try
            {
                var cameraControl = _captureFilter as IAMCameraControl;
                var videoProcAmp = _captureFilter as IAMVideoProcAmp;

                int value = 0;
                CameraControlFlags cameraFlags = CameraControlFlags.Manual;
                VideoProcAmpFlags videoFlags = VideoProcAmpFlags.Manual;
                int hr = -1;

                switch (property)
                {
                    case CameraProperty.Exposure:
                        hr = cameraControl?.Get(CameraControlProperty.Exposure, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);
                    case CameraProperty.Focus:
                        hr = cameraControl?.Get(CameraControlProperty.Focus, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);
                    case CameraProperty.Zoom:
                        hr = cameraControl?.Get(CameraControlProperty.Zoom, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);
                    case CameraProperty.Pan:
                        hr = cameraControl?.Get(CameraControlProperty.Pan, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);
                    case CameraProperty.Tilt:
                        hr = cameraControl?.Get(CameraControlProperty.Tilt, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);
                    case CameraProperty.Roll:
                        hr = cameraControl?.Get(CameraControlProperty.Roll, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);
                    case CameraProperty.Iris:
                        hr = cameraControl?.Get(CameraControlProperty.Iris, out value, out cameraFlags) ?? -1;
                        return (value, cameraFlags == CameraControlFlags.Auto, hr == 0);

                    case CameraProperty.Brightness:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Brightness, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.Contrast:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Contrast, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.Hue:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Hue, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.Saturation:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Saturation, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.Sharpness:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Sharpness, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.Gamma:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Gamma, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.ColorEnable:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.ColorEnable, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.WhiteBalance:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.WhiteBalance, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.BacklightCompensation:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.BacklightCompensation, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                    case CameraProperty.Gain:
                        hr = videoProcAmp?.Get(VideoProcAmpProperty.Gain, out value, out videoFlags) ?? -1;
                        return (value, videoFlags == VideoProcAmpFlags.Auto, hr == 0);
                }

                return (0, false, false);
            }
            catch
            {
                return (0, false, false);
            }
        }

        public (int min, int max, int step, int defaultValue, bool success) GetCameraPropertyRange(CameraProperty property)
        {
            if (_captureFilter == null || !IsConnected)
                return (0, 0, 0, 0, false);

            try
            {
                var cameraControl = _captureFilter as IAMCameraControl;
                var videoProcAmp = _captureFilter as IAMVideoProcAmp;

                int min = 0, max = 0, step = 0, defaultValue = 0;
                CameraControlFlags cameraFlags = CameraControlFlags.Manual;
                VideoProcAmpFlags videoFlags = VideoProcAmpFlags.Manual;
                int hr = -1;

                switch (property)
                {
                    case CameraProperty.Exposure:
                    case CameraProperty.Focus:
                    case CameraProperty.Zoom:
                    case CameraProperty.Pan:
                    case CameraProperty.Tilt:
                    case CameraProperty.Roll:
                    case CameraProperty.Iris:
                        var camProp = property switch
                        {
                            CameraProperty.Exposure => CameraControlProperty.Exposure,
                            CameraProperty.Focus => CameraControlProperty.Focus,
                            CameraProperty.Zoom => CameraControlProperty.Zoom,
                            CameraProperty.Pan => CameraControlProperty.Pan,
                            CameraProperty.Tilt => CameraControlProperty.Tilt,
                            CameraProperty.Roll => CameraControlProperty.Roll,
                            CameraProperty.Iris => CameraControlProperty.Iris,
                            _ => CameraControlProperty.Exposure
                        };
                        hr = cameraControl?.GetRange(camProp, out min, out max, out step, out defaultValue, out cameraFlags) ?? -1;
                        break;

                    default:
                        var videoProp = property switch
                        {
                            CameraProperty.Brightness => VideoProcAmpProperty.Brightness,
                            CameraProperty.Contrast => VideoProcAmpProperty.Contrast,
                            CameraProperty.Hue => VideoProcAmpProperty.Hue,
                            CameraProperty.Saturation => VideoProcAmpProperty.Saturation,
                            CameraProperty.Sharpness => VideoProcAmpProperty.Sharpness,
                            CameraProperty.Gamma => VideoProcAmpProperty.Gamma,
                            CameraProperty.ColorEnable => VideoProcAmpProperty.ColorEnable,
                            CameraProperty.WhiteBalance => VideoProcAmpProperty.WhiteBalance,
                            CameraProperty.BacklightCompensation => VideoProcAmpProperty.BacklightCompensation,
                            CameraProperty.Gain => VideoProcAmpProperty.Gain,
                            _ => VideoProcAmpProperty.Brightness
                        };
                        hr = videoProcAmp?.GetRange(videoProp, out min, out max, out step, out defaultValue, out videoFlags) ?? -1;
                        break;
                }

                return (min, max, step, defaultValue, hr == 0);
            }
            catch
            {
                return (0, 0, 0, 0, false);
            }
        }

        public List<string> GetAvailableDevices()
        {
            var devices = new List<string>();
            try
            {
                DsDevice[]? dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (dsDevices != null)
                {
                    foreach (var device in dsDevices)
                    {
                        devices.Add(device.Name);
                    }
                }
            }
            catch
            {
            }
            return devices;
        }

        public bool SetVideoFormat(int width, int height, int bitsPerPixel = 24)
        {
            if (_streamConfig == null)
            {
                System.Diagnostics.Debug.WriteLine("UvcCameraController: StreamConfig not available");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Attempting to set video format to {width}x{height} @ {bitsPerPixel}bpp");

                // Get current media type count and capabilities
                int hr = _streamConfig.GetNumberOfCapabilities(out int capCount, out int capSize);
                if (hr != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Failed to get capabilities count, HR: {hr}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Found {capCount} video capabilities");

                // Try to find a matching format
                IntPtr pSC = Marshal.AllocCoTaskMem(capSize);
                AMMediaType bestMatch = new AMMediaType();
                bool foundMatch = false;

                try
                {
                    for (int i = 0; i < capCount; i++)
                    {
                        hr = _streamConfig.GetStreamCaps(i, out AMMediaType mt, pSC);
                        if (hr != 0) continue;

                        if (mt.formatType == FormatType.VideoInfo)
                        {
                            VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader));

                            if (vih.BmiHeader.Width == width &&
                                Math.Abs(vih.BmiHeader.Height) == height &&
                                vih.BmiHeader.BitCount == bitsPerPixel)
                            {
                                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Found exact match at capability {i}");
                                bestMatch = mt;
                                foundMatch = true;
                                break;
                            }
                        }
                        else if (mt.formatType == FormatType.VideoInfo2)
                        {
                            VideoInfoHeader2 vih2 = (VideoInfoHeader2)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader2));

                            if (vih2.BmiHeader.Width == width &&
                                Math.Abs(vih2.BmiHeader.Height) == height &&
                                vih2.BmiHeader.BitCount == bitsPerPixel)
                            {
                                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Found exact match (VideoInfo2) at capability {i}");
                                bestMatch = mt;
                                foundMatch = true;
                                break;
                            }
                        }

                        DsUtils.FreeAMMediaType(mt);
                    }

                    if (foundMatch)
                    {
                        hr = _streamConfig.SetFormat(bestMatch);
                        DsUtils.FreeAMMediaType(bestMatch);

                        if (hr == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"UvcCameraController: Successfully set video format to {width}x{height}");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"UvcCameraController: Failed to set video format, HR: {hr}");
                            return false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"UvcCameraController: No matching video format found for {width}x{height} @ {bitsPerPixel}bpp");
                        return false;
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pSC);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Exception setting video format: {ex.Message}");
                return false;
            }
        }

        public List<(int width, int height, int bpp)> GetSupportedVideoFormats()
        {
            var formats = new List<(int, int, int)>();

            if (_streamConfig == null)
                return formats;

            try
            {
                int hr = _streamConfig.GetNumberOfCapabilities(out int capCount, out int capSize);
                if (hr != 0) return formats;

                IntPtr pSC = Marshal.AllocCoTaskMem(capSize);

                try
                {
                    for (int i = 0; i < capCount; i++)
                    {
                        hr = _streamConfig.GetStreamCaps(i, out AMMediaType mt, pSC);
                        if (hr != 0) continue;

                        try
                        {
                            if (mt.formatType == FormatType.VideoInfo)
                            {
                                VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader));
                                var format = (vih.BmiHeader.Width, Math.Abs(vih.BmiHeader.Height), vih.BmiHeader.BitCount);

                                if (!formats.Contains(format))
                                {
                                    formats.Add(format);
                                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Supported format: {format.Width}x{format.Item2} @ {format.BitCount}bpp");
                                }
                            }
                            else if (mt.formatType == FormatType.VideoInfo2)
                            {
                                VideoInfoHeader2 vih2 = (VideoInfoHeader2)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader2));
                                var format = (vih2.BmiHeader.Width, Math.Abs(vih2.BmiHeader.Height), vih2.BmiHeader.BitCount);

                                if (!formats.Contains(format))
                                {
                                    formats.Add(format);
                                    System.Diagnostics.Debug.WriteLine($"UvcCameraController: Supported format (VideoInfo2): {format.Width}x{format.Item2} @ {format.BitCount}bpp");
                                }
                            }
                        }
                        finally
                        {
                            DsUtils.FreeAMMediaType(mt);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pSC);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UvcCameraController: Error getting supported formats: {ex.Message}");
            }

            return formats.Distinct().OrderBy(f => f.Item1 * f.Item2).ToList();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                if (_mediaControl != null)
                {
                    Marshal.ReleaseComObject(_mediaControl);
                    _mediaControl = null;
                }

                if (_streamConfig != null)
                {
                    Marshal.ReleaseComObject(_streamConfig);
                    _streamConfig = null;
                }

                if (_captureFilter != null)
                {
                    Marshal.ReleaseComObject(_captureFilter);
                    _captureFilter = null;
                }

                if (_filterGraph != null)
                {
                    Marshal.ReleaseComObject(_filterGraph);
                    _filterGraph = null;
                }

                _graphBuilder = null;
                IsConnected = false;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~UvcCameraController()
        {
            Dispose(disposing: false);
        }
    }

    public enum CameraProperty
    {
        Exposure,
        Focus,
        Zoom,
        Pan,
        Tilt,
        Roll,
        Iris,
        Brightness,
        Contrast,
        Hue,
        Saturation,
        Sharpness,
        Gamma,
        ColorEnable,
        WhiteBalance,
        BacklightCompensation,
        Gain
    }
}