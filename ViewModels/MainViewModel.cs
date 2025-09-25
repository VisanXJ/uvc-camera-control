using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UVCCameraControl.Models;
using UVCCameraControl.Services;

namespace UVCCameraControl.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICameraService _cameraService;

        [ObservableProperty]
        private ObservableCollection<CameraDevice> availableCameras = new();

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

        public object? MediaCaptureSource =>
            _cameraService.MediaCapture;

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
                        OnPropertyChanged(nameof(MediaCaptureSource));
                        await LoadCurrentSettingsAsync();
                    }
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
                OnPropertyChanged(nameof(MediaCaptureSource));
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
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
        }

        private bool CanStartPreview()
        {
            return SelectedCamera != null && !IsPreviewActive;
        }
    }
}