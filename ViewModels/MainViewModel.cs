using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UVCCameraControl.Models;
using UVCCameraControl.Services;
using System.Collections.Generic;
using System.Linq;

namespace UVCCameraControl.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICameraService _cameraService;

        [ObservableProperty]
        private ObservableCollection<CameraDevice> availableCameras = new();

        [ObservableProperty]
        private List<VideoResolution> availableResolutions = VideoResolution.CommonResolutions;

        [ObservableProperty]
        private List<double> availableFrameRates = new() { 10.0, 15.0, 20.0, 25.0, 30.0, 50.0, 60.0 };

        private CameraDevice? _selectedCamera;
        public CameraDevice? SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (SetProperty(ref _selectedCamera, value))
                {
                    ((AsyncRelayCommand)StartPreviewCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private VideoResolution? _selectedResolution;
        public VideoResolution? SelectedResolution
        {
            get => _selectedResolution;
            set
            {
                if (SetProperty(ref _selectedResolution, value))
                {
                    if (value != null)
                    {
                        CameraSettings.Width = value.Width;
                        CameraSettings.Height = value.Height;
                    }
                }
            }
        }

        private bool _isPreviewActive;
        public bool IsPreviewActive
        {
            get => _isPreviewActive;
            set
            {
                if (SetProperty(ref _isPreviewActive, value))
                {
                    ((AsyncRelayCommand)StartPreviewCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)StopPreviewCommand).NotifyCanExecuteChanged();
                }
            }
        }

        [ObservableProperty]
        private CameraSettings cameraSettings = new();

        [ObservableProperty]
        private string statusMessage = "Ready";

        // 用于显示设备实际当前状态的属性
        [ObservableProperty]
        private int actualWidth = 0;

        [ObservableProperty]
        private int actualHeight = 0;

        [ObservableProperty]
        private double actualFrameRate = 0.0;

        // Camera property ranges for UI binding
        [ObservableProperty]
        private int brightnessMin = -100, brightnessMax = 100, brightnessStep = 1, brightnessDefault = 0;

        [ObservableProperty]
        private int contrastMin = 0, contrastMax = 100, contrastStep = 1, contrastDefault = 50;

        [ObservableProperty]
        private int saturationMin = 0, saturationMax = 200, saturationStep = 1, saturationDefault = 100;

        [ObservableProperty]
        private int hueMin = -180, hueMax = 180, hueStep = 1, hueDefault = 0;

        [ObservableProperty]
        private double gammaMin = 0.1, gammaMax = 3.0, gammaStep = 0.1, gammaDefault = 1.0;

        [ObservableProperty]
        private int whiteBalanceMin = 2000, whiteBalanceMax = 10000, whiteBalanceStep = 100, whiteBalanceDefault = 6500;

        [ObservableProperty]
        private int exposureMin = -10, exposureMax = 0, exposureStep = 1, exposureDefault = -5;

        [ObservableProperty]
        private int focusMin = 0, focusMax = 1000, focusStep = 1, focusDefault = 500;

        [ObservableProperty]
        private int zoomMin = 1, zoomMax = 10, zoomStep = 1, zoomDefault = 1;

        public ICameraService CameraService => _cameraService;

        public ICommand LoadCamerasCommand { get; }
        public ICommand StartPreviewCommand { get; }
        public ICommand StopPreviewCommand { get; }
        public ICommand ApplySettingsCommand { get; }

        public MainViewModel(ICameraService cameraService)
        {
            _cameraService = cameraService;

            LoadCamerasCommand = new AsyncRelayCommand(LoadCamerasAsync);
            StartPreviewCommand = new AsyncRelayCommand(StartPreviewAsync, CanStartPreview);
            StopPreviewCommand = new AsyncRelayCommand(StopPreviewAsync, () => IsPreviewActive);
            ApplySettingsCommand = new AsyncRelayCommand(ApplySettingsAsync);
        }

        private async Task LoadCamerasAsync()
        {
            try
            {
                StatusMessage = "Loading cameras...";
                AvailableCameras = await _cameraService.GetAvailableCamerasAsync();
                StatusMessage = $"Found {AvailableCameras.Count} cameras";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading cameras: {ex.Message}";
            }
        }

        private async Task StartPreviewAsync()
        {
            if (SelectedCamera == null) return;

            try
            {
                StatusMessage = "Starting preview...";
                var initialized = await _cameraService.InitializeCameraAsync(SelectedCamera.Id);
                if (initialized)
                {
                    var started = await _cameraService.StartPreviewAsync();
                    if (started)
                    {
                        IsPreviewActive = true;
                        StatusMessage = "Preview active";
                        await LoadCurrentSettingsAsync();
                    }
                    else
                    {
                        StatusMessage = "Failed to start preview";
                    }
                }
                else
                {
                    StatusMessage = "Failed to initialize camera";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting preview: {ex.Message}";
            }
        }

        private async Task StopPreviewAsync()
        {
            try
            {
                StatusMessage = "Stopping preview...";
                await _cameraService.StopPreviewAsync();
                IsPreviewActive = false;
                StatusMessage = "Preview stopped";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping preview: {ex.Message}";
            }
        }

        private async Task ApplySettingsAsync()
        {
            if (!IsPreviewActive) return;

            try
            {
                await _cameraService.SetCameraSettingsAsync(CameraSettings);
                StatusMessage = "Settings applied";

                // Update actual device status after applying settings
                var (actualWidth, actualHeight, actualFrameRate) = await _cameraService.GetActualCameraStatusAsync();
                ActualWidth = actualWidth;
                ActualHeight = actualHeight;
                ActualFrameRate = actualFrameRate;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error applying settings: {ex.Message}";
            }
        }

        private async Task LoadCurrentSettingsAsync()
        {
            try
            {
                CameraSettings = await _cameraService.GetCameraSettingsAsync();

                // Update selected resolution based on current settings
                SelectedResolution = AvailableResolutions.FirstOrDefault(r =>
                    r.Width == CameraSettings.Width && r.Height == CameraSettings.Height);

                // Get actual device status for display
                var (actualWidth, actualHeight, actualFrameRate) = await _cameraService.GetActualCameraStatusAsync();
                ActualWidth = actualWidth;
                ActualHeight = actualHeight;
                ActualFrameRate = actualFrameRate;

                // Load camera property ranges from device
                await LoadCameraPropertyRangesAsync();

                System.Diagnostics.Debug.WriteLine($"MainViewModel: Updated actual status - {ActualWidth}x{ActualHeight} @ {ActualFrameRate:F1}fps");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
        }

        private async Task LoadCameraPropertyRangesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: Loading camera property ranges...");

                // Load brightness range
                var brightnessRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Brightness);
                if (brightnessRange.success)
                {
                    BrightnessMin = brightnessRange.min;
                    BrightnessMax = brightnessRange.max;
                    BrightnessStep = brightnessRange.step > 0 ? brightnessRange.step : 1;
                    BrightnessDefault = brightnessRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Brightness range: {BrightnessMin} to {BrightnessMax}, step: {BrightnessStep}, default: {BrightnessDefault}");
                }

                // Load contrast range
                var contrastRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Contrast);
                if (contrastRange.success)
                {
                    ContrastMin = contrastRange.min;
                    ContrastMax = contrastRange.max;
                    ContrastStep = contrastRange.step > 0 ? contrastRange.step : 1;
                    ContrastDefault = contrastRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Contrast range: {ContrastMin} to {ContrastMax}, step: {ContrastStep}, default: {ContrastDefault}");
                }

                // Load saturation range
                var saturationRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Saturation);
                if (saturationRange.success)
                {
                    SaturationMin = saturationRange.min;
                    SaturationMax = saturationRange.max;
                    SaturationStep = saturationRange.step > 0 ? saturationRange.step : 1;
                    SaturationDefault = saturationRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Saturation range: {SaturationMin} to {SaturationMax}, step: {SaturationStep}, default: {SaturationDefault}");
                }

                // Load hue range
                var hueRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Hue);
                if (hueRange.success)
                {
                    HueMin = hueRange.min;
                    HueMax = hueRange.max;
                    HueStep = hueRange.step > 0 ? hueRange.step : 1;
                    HueDefault = hueRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Hue range: {HueMin} to {HueMax}, step: {HueStep}, default: {HueDefault}");
                }

                // Load gamma range (need to convert from 0-300 range to 0-3.0 range)
                var gammaRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Gamma);
                if (gammaRange.success)
                {
                    GammaMin = gammaRange.min / 100.0;
                    GammaMax = gammaRange.max / 100.0;
                    GammaStep = (gammaRange.step > 0 ? gammaRange.step : 1) / 100.0;
                    GammaDefault = gammaRange.defaultValue / 100.0;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Gamma range: {GammaMin:F2} to {GammaMax:F2}, step: {GammaStep:F2}, default: {GammaDefault:F2}");
                }

                // Load white balance range
                var whiteBalanceRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.WhiteBalance);
                if (whiteBalanceRange.success)
                {
                    WhiteBalanceMin = whiteBalanceRange.min;
                    WhiteBalanceMax = whiteBalanceRange.max;
                    WhiteBalanceStep = whiteBalanceRange.step > 0 ? whiteBalanceRange.step : 100;
                    WhiteBalanceDefault = whiteBalanceRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: WhiteBalance range: {WhiteBalanceMin} to {WhiteBalanceMax}, step: {WhiteBalanceStep}, default: {WhiteBalanceDefault}");
                }

                // Load exposure range (DirectShow exposure values are in log2 scale, negative values = shorter exposure)
                var exposureRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Exposure);
                if (exposureRange.success)
                {
                    // DirectShow exposure is log2 scale where -13 = very short, -1 = longer exposure
                    // This is different from v4l2 exposure_time_absolute (1-5000 microseconds)
                    ExposureMin = exposureRange.min;
                    ExposureMax = exposureRange.max;
                    ExposureStep = exposureRange.step > 0 ? exposureRange.step : 1;
                    ExposureDefault = exposureRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: DirectShow Exposure range: {ExposureMin} to {ExposureMax}, step: {ExposureStep}, default: {ExposureDefault}");
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Note - DirectShow uses log2 scale (negative=shorter exposure), v4l2 uses absolute time in microseconds");
                }

                // Load focus range
                var focusRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Focus);
                if (focusRange.success)
                {
                    FocusMin = focusRange.min;
                    FocusMax = focusRange.max;
                    FocusStep = focusRange.step > 0 ? focusRange.step : 1;
                    FocusDefault = focusRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Focus range: {FocusMin} to {FocusMax}, step: {FocusStep}, default: {FocusDefault}");
                }

                // Load zoom range
                var zoomRange = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Zoom);
                if (zoomRange.success)
                {
                    ZoomMin = zoomRange.min;
                    ZoomMax = zoomRange.max;
                    ZoomStep = zoomRange.step > 0 ? zoomRange.step : 1;
                    ZoomDefault = zoomRange.defaultValue;
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Zoom range: {ZoomMin} to {ZoomMax}, step: {ZoomStep}, default: {ZoomDefault}");
                }

                System.Diagnostics.Debug.WriteLine("MainViewModel: Completed loading camera property ranges");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel: Exception loading camera property ranges: {ex.Message}");
            }
        }

        private bool CanStartPreview()
        {
            return SelectedCamera != null && !IsPreviewActive;
        }
    }
}