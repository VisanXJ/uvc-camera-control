using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UVCCameraControl.Models;

namespace UVCCameraControl.Services
{
    public interface ICameraService
    {
        Task<ObservableCollection<CameraDevice>> GetAvailableCamerasAsync();
        Task<bool> InitializeCameraAsync(string deviceId);
        Task<bool> StartPreviewAsync();
        Task StopPreviewAsync();
        Task<CameraSettings> GetCameraSettingsAsync();
        Task SetCameraSettingsAsync(CameraSettings settings);
        object? MediaCapture { get; }
        event EventHandler<BitmapSource>? FrameCaptured;
    }
}