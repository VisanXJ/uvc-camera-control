using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UVCCameraControl.Models;
using DirectShowLib;

namespace UVCCameraControl.Services
{
    public class CameraService : ICameraService
    {
        private IFilterGraph2? _filterGraph;
        private IMediaControl? _mediaControl;
        private IBaseFilter? _captureFilter;
        private IBaseFilter? _sampleGrabberFilter;
        private ISampleGrabber? _sampleGrabber;
        private SampleGrabberCallback? _sampleGrabberCallback;
        private IAMCameraControl? _cameraControl;
        private IAMVideoProcAmp? _videoProcAmp;
        private bool _isInitialized = false;
        private string? _currentDeviceMoniker;

        public object? MediaCapture => _filterGraph;
        public event EventHandler<BitmapSource>? FrameCaptured;

        public async Task<ObservableCollection<CameraDevice>> GetAvailableCamerasAsync()
        {
            var cameras = new ObservableCollection<CameraDevice>();

            try
            {
                await Task.Run(() =>
                {
                    DsDevice[] videoInputDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

                    foreach (DsDevice device in videoInputDevices)
                    {
                        cameras.Add(new CameraDevice
                        {
                            Id = device.DevicePath,
                            Name = device.Name,
                            Location = "DirectShow Device",
                            IsEnabled = true
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enumerate cameras: {ex.Message}", ex);
            }

            return cameras;
        }

        public async Task<bool> InitializeCameraAsync(string deviceId)
        {
            try
            {
                if (_isInitialized)
                {
                    await StopPreviewAsync();
                }

                await Task.Run(() =>
                {
                    // Create the filter graph
                    _filterGraph = (IFilterGraph2)new FilterGraph();
                    _mediaControl = _filterGraph as IMediaControl;

                    // Find and create the capture filter
                    DsDevice[] devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                    DsDevice? targetDevice = null;

                    foreach (DsDevice device in devices)
                    {
                        if (device.DevicePath == deviceId)
                        {
                            targetDevice = device;
                            break;
                        }
                    }

                    if (targetDevice == null)
                        throw new InvalidOperationException("Camera device not found");

                    // Create the capture filter
                    Guid iid = typeof(IBaseFilter).GUID;
                    targetDevice.Mon.BindToObject(null, null, ref iid, out object captureFilterObj);
                    _captureFilter = (IBaseFilter)captureFilterObj;

                    // Add the capture filter to the graph
                    int hr = _filterGraph.AddFilter(_captureFilter, "Capture Filter");
                    DsError.ThrowExceptionForHR(hr);

                    // Get control interfaces
                    _cameraControl = _captureFilter as IAMCameraControl;
                    _videoProcAmp = _captureFilter as IAMVideoProcAmp;

                    _currentDeviceMoniker = deviceId;
                    _isInitialized = true;
                });

                return true;
            }
            catch (Exception ex)
            {
                await StopPreviewAsync();
                throw new InvalidOperationException($"Failed to initialize camera: {ex.Message}", ex);
            }
        }

        public async Task<bool> StartPreviewAsync()
        {
            if (_filterGraph == null || !_isInitialized)
                return false;

            try
            {
                await Task.Run(() =>
                {
                    // Create and configure sample grabber
                    _sampleGrabberFilter = (IBaseFilter)new SampleGrabber();
                    _sampleGrabber = _sampleGrabberFilter as ISampleGrabber;

                    if (_sampleGrabber != null)
                    {
                        // Try to use camera's native format first (MJPEG for JPEG cameras)
                        AMMediaType mediaType = new AMMediaType();
                        mediaType.majorType = MediaType.Video;
                        mediaType.subType = MediaSubType.MJPG; // Use MJPEG for JPEG cameras
                        mediaType.formatType = FormatType.VideoInfo;

                        int hr = _sampleGrabber.SetMediaType(mediaType);
                        DsError.ThrowExceptionForHR(hr);

                        // Add sample grabber to graph
                        hr = _filterGraph.AddFilter(_sampleGrabberFilter, "Sample Grabber");
                        DsError.ThrowExceptionForHR(hr);

                        // Manually connect capture filter to sample grabber (no auto-rendering)
                        ConnectFilters(_captureFilter, _sampleGrabberFilter);

                        // Set up callback for frame capture
                        _sampleGrabberCallback = new SampleGrabberCallback();
                        _sampleGrabberCallback.FrameCaptured += OnFrameCaptured;

                        // Get media type to determine image size
                        mediaType = new AMMediaType();
                        hr = _sampleGrabber.GetConnectedMediaType(mediaType);
                        if (hr == 0)
                        {
                            VideoInfoHeader videoInfo = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                            _sampleGrabberCallback.SetImageSize(videoInfo.BmiHeader.Width, Math.Abs(videoInfo.BmiHeader.Height));
                            DsUtils.FreeAMMediaType(mediaType);
                        }

                        // Set the callback
                        hr = _sampleGrabber.SetCallback(_sampleGrabberCallback, 1);
                        DsError.ThrowExceptionForHR(hr);

                        // Don't buffer samples
                        hr = _sampleGrabber.SetBufferSamples(false);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    // Start the graph
                    int startHr = _mediaControl.Run();
                    DsError.ThrowExceptionForHR(startHr);
                });
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start preview: {ex.Message}", ex);
            }
        }

        private void ConnectFilters(IBaseFilter? sourceFilter, IBaseFilter? destinationFilter)
        {
            if (sourceFilter == null || destinationFilter == null)
                return;

            // Get output pin from source filter
            IPin? sourcePin = FindPin(sourceFilter, PinDirection.Output, "Capture");
            if (sourcePin == null)
                sourcePin = FindPin(sourceFilter, PinDirection.Output, null); // Get any output pin

            // Get input pin from destination filter
            IPin? destPin = FindPin(destinationFilter, PinDirection.Input, null);

            if (sourcePin != null && destPin != null)
            {
                int hr = _filterGraph.Connect(sourcePin, destPin);
                DsError.ThrowExceptionForHR(hr);

                Marshal.ReleaseComObject(sourcePin);
                Marshal.ReleaseComObject(destPin);
            }
        }

        private IPin? FindPin(IBaseFilter filter, PinDirection direction, string? pinName)
        {
            IEnumPins enumPins;
            int hr = filter.EnumPins(out enumPins);
            if (hr != 0) return null;

            IPin[] pins = new IPin[1];
            while (enumPins.Next(1, pins, IntPtr.Zero) == 0)
            {
                PinDirection pinDir;
                hr = pins[0].QueryDirection(out pinDir);
                if (hr == 0 && pinDir == direction)
                {
                    if (pinName == null)
                    {
                        Marshal.ReleaseComObject(enumPins);
                        return pins[0];
                    }

                    PinInfo pinInfo;
                    hr = pins[0].QueryPinInfo(out pinInfo);
                    if (hr == 0)
                    {
                        if (pinInfo.name.Contains(pinName))
                        {
                            DsUtils.FreePinInfo(pinInfo);
                            Marshal.ReleaseComObject(enumPins);
                            return pins[0];
                        }
                        DsUtils.FreePinInfo(pinInfo);
                    }
                }
                Marshal.ReleaseComObject(pins[0]);
            }

            Marshal.ReleaseComObject(enumPins);
            return null;
        }

        private void OnFrameCaptured(object? sender, BitmapSource bitmapSource)
        {
            FrameCaptured?.Invoke(this, bitmapSource);
        }

        public async Task StopPreviewAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (_mediaControl != null)
                    {
                        _mediaControl.Stop();
                    }

                    // Clean up sample grabber callback
                    if (_sampleGrabberCallback != null)
                    {
                        _sampleGrabberCallback.FrameCaptured -= OnFrameCaptured;
                        _sampleGrabberCallback = null;
                    }

                    // Release COM objects
                    if (_sampleGrabber != null)
                    {
                        Marshal.ReleaseComObject(_sampleGrabber);
                        _sampleGrabber = null;
                    }

                    if (_sampleGrabberFilter != null)
                    {
                        Marshal.ReleaseComObject(_sampleGrabberFilter);
                        _sampleGrabberFilter = null;
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

                    _mediaControl = null;
                    _cameraControl = null;
                    _videoProcAmp = null;
                    _isInitialized = false;
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to stop preview: {ex.Message}", ex);
            }
        }

        public async Task<CameraSettings> GetCameraSettingsAsync()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Camera not initialized");

            var settings = new CameraSettings();

            try
            {
                await Task.Run(() =>
                {
                    int min, max, step, defaultValue;
                    VideoProcAmpFlags flags;
                    CameraControlFlags camFlags;

                    // Get video processing settings
                    if (_videoProcAmp != null)
                    {
                        // Brightness
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Brightness, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (_videoProcAmp.Get(VideoProcAmpProperty.Brightness, out int value, out _) == 0)
                            {
                                // Normalize to -100 to 100 range
                                settings.Brightness = ((double)(value - min) / (max - min) * 200) - 100;
                            }
                        }

                        // Contrast
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Contrast, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (_videoProcAmp.Get(VideoProcAmpProperty.Contrast, out int value, out _) == 0)
                            {
                                settings.Contrast = ((double)(value - min) / (max - min) * 200) - 100;
                            }
                        }

                        // Saturation
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Saturation, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (_videoProcAmp.Get(VideoProcAmpProperty.Saturation, out int value, out _) == 0)
                            {
                                settings.Saturation = ((double)(value - min) / (max - min) * 200) - 100;
                            }
                        }

                        // Hue
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Hue, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (_videoProcAmp.Get(VideoProcAmpProperty.Hue, out int value, out _) == 0)
                            {
                                settings.Hue = ((double)(value - min) / (max - min) * 360) - 180;
                            }
                        }

                        // Gamma
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Gamma, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (_videoProcAmp.Get(VideoProcAmpProperty.Gamma, out int value, out _) == 0)
                            {
                                settings.Gamma = ((double)(value - min) / (max - min) * 2.5) + 0.5;
                            }
                        }

                        // White Balance
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.WhiteBalance, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (_videoProcAmp.Get(VideoProcAmpProperty.WhiteBalance, out int value, out VideoProcAmpFlags currentFlags) == 0)
                            {
                                settings.WhiteBalance = ((double)(value - min) / (max - min) * 8000) + 2000;
                                settings.AutoWhiteBalance = currentFlags == VideoProcAmpFlags.Auto;
                            }
                        }
                    }

                    // Get camera control settings
                    if (_cameraControl != null)
                    {
                        // Exposure
                        if (_cameraControl.GetRange(CameraControlProperty.Exposure, out min, out max, out step, out defaultValue, out camFlags) == 0)
                        {
                            if (_cameraControl.Get(CameraControlProperty.Exposure, out int value, out CameraControlFlags currentFlags) == 0)
                            {
                                settings.Exposure = ((double)(value - min) / (max - min) * 10) - 10;
                                settings.AutoExposure = currentFlags == CameraControlFlags.Auto;
                            }
                        }

                        // Focus
                        if (_cameraControl.GetRange(CameraControlProperty.Focus, out min, out max, out step, out defaultValue, out camFlags) == 0)
                        {
                            if (_cameraControl.Get(CameraControlProperty.Focus, out int value, out CameraControlFlags currentFlags) == 0)
                            {
                                settings.Focus = ((double)(value - min) / (max - min) * 100);
                                settings.AutoFocus = currentFlags == CameraControlFlags.Auto;
                            }
                        }

                        // Zoom
                        if (_cameraControl.GetRange(CameraControlProperty.Zoom, out min, out max, out step, out defaultValue, out camFlags) == 0)
                        {
                            if (_cameraControl.Get(CameraControlProperty.Zoom, out int value, out _) == 0)
                            {
                                settings.Zoom = ((double)(value - min) / (max - min) * 9) + 1;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get camera settings: {ex.Message}", ex);
            }

            return settings;
        }

        public async Task SetCameraSettingsAsync(CameraSettings settings)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Camera not initialized");

            try
            {
                await Task.Run(() =>
                {
                    int min, max, step, defaultValue;
                    VideoProcAmpFlags flags;
                    CameraControlFlags camFlags;

                    // Set video processing settings
                    if (_videoProcAmp != null)
                    {
                        // Brightness
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Brightness, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            int value = (int)((settings.Brightness + 100) / 200 * (max - min) + min);
                            _videoProcAmp.Set(VideoProcAmpProperty.Brightness, value, VideoProcAmpFlags.Manual);
                        }

                        // Contrast
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Contrast, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            int value = (int)((settings.Contrast + 100) / 200 * (max - min) + min);
                            _videoProcAmp.Set(VideoProcAmpProperty.Contrast, value, VideoProcAmpFlags.Manual);
                        }

                        // Saturation
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Saturation, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            int value = (int)((settings.Saturation + 100) / 200 * (max - min) + min);
                            _videoProcAmp.Set(VideoProcAmpProperty.Saturation, value, VideoProcAmpFlags.Manual);
                        }

                        // Hue
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Hue, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            int value = (int)((settings.Hue + 180) / 360 * (max - min) + min);
                            _videoProcAmp.Set(VideoProcAmpProperty.Hue, value, VideoProcAmpFlags.Manual);
                        }

                        // Gamma
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.Gamma, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            int value = (int)((settings.Gamma - 0.5) / 2.5 * (max - min) + min);
                            _videoProcAmp.Set(VideoProcAmpProperty.Gamma, value, VideoProcAmpFlags.Manual);
                        }

                        // White Balance
                        if (_videoProcAmp.GetRange(VideoProcAmpProperty.WhiteBalance, out min, out max, out step, out defaultValue, out flags) == 0)
                        {
                            if (settings.AutoWhiteBalance)
                            {
                                _videoProcAmp.Set(VideoProcAmpProperty.WhiteBalance, defaultValue, VideoProcAmpFlags.Auto);
                            }
                            else
                            {
                                int value = (int)((settings.WhiteBalance - 2000) / 8000 * (max - min) + min);
                                _videoProcAmp.Set(VideoProcAmpProperty.WhiteBalance, value, VideoProcAmpFlags.Manual);
                            }
                        }
                    }

                    // Set camera control settings
                    if (_cameraControl != null)
                    {
                        // Exposure
                        if (_cameraControl.GetRange(CameraControlProperty.Exposure, out min, out max, out step, out defaultValue, out camFlags) == 0)
                        {
                            if (settings.AutoExposure)
                            {
                                _cameraControl.Set(CameraControlProperty.Exposure, defaultValue, CameraControlFlags.Auto);
                            }
                            else
                            {
                                int value = (int)((settings.Exposure + 10) / 10 * (max - min) + min);
                                _cameraControl.Set(CameraControlProperty.Exposure, value, CameraControlFlags.Manual);
                            }
                        }

                        // Focus
                        if (_cameraControl.GetRange(CameraControlProperty.Focus, out min, out max, out step, out defaultValue, out camFlags) == 0)
                        {
                            if (settings.AutoFocus)
                            {
                                _cameraControl.Set(CameraControlProperty.Focus, defaultValue, CameraControlFlags.Auto);
                            }
                            else
                            {
                                int value = (int)(settings.Focus / 100 * (max - min) + min);
                                _cameraControl.Set(CameraControlProperty.Focus, value, CameraControlFlags.Manual);
                            }
                        }

                        // Zoom
                        if (_cameraControl.GetRange(CameraControlProperty.Zoom, out min, out max, out step, out defaultValue, out camFlags) == 0)
                        {
                            int value = (int)((settings.Zoom - 1) / 9 * (max - min) + min);
                            _cameraControl.Set(CameraControlProperty.Zoom, value, CameraControlFlags.Manual);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set camera settings: {ex.Message}", ex);
            }
        }
    }
}