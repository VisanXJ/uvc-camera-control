using System.Collections.Generic;

namespace UVCCameraControl.Models
{
    public class CameraDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class VideoResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; } = string.Empty;

        public override string ToString() => Name;

        public static List<VideoResolution> CommonResolutions => new()
        {
            new VideoResolution { Width = 320, Height = 240, Name = "320×240 (QVGA)" },
            new VideoResolution { Width = 640, Height = 480, Name = "640×480 (VGA)" },
            new VideoResolution { Width = 800, Height = 600, Name = "800×600 (SVGA)" },
            new VideoResolution { Width = 1024, Height = 768, Name = "1024×768 (XGA)" },
            new VideoResolution { Width = 1280, Height = 720, Name = "1280×720 (720p)" },
            new VideoResolution { Width = 1280, Height = 960, Name = "1280×960 (SXGA)" },
            new VideoResolution { Width = 1600, Height = 1200, Name = "1600×1200 (UXGA)" },
            new VideoResolution { Width = 1920, Height = 1080, Name = "1920×1080 (1080p)" },
        };
    }

    public class CameraSettings
    {
        // Basic video processing parameters
        public double Brightness { get; set; } = 0;
        public double Contrast { get; set; } = 0;
        public double Saturation { get; set; } = 0;
        public double Hue { get; set; } = 0;
        public double Gamma { get; set; } = 1.0;

        // White balance parameters
        public double WhiteBalance { get; set; } = 5000;
        public bool AutoWhiteBalance { get; set; } = true;
        public double WhiteBalanceU { get; set; } = 0; // Blue-Yellow balance
        public double WhiteBalanceV { get; set; } = 0; // Red-Green balance

        // Camera control parameters
        public int Exposure { get; set; } = -5;  // DirectShow exposure values are integers
        public bool AutoExposure { get; set; } = true;
        public int Focus { get; set; } = 50;     // DirectShow focus values are integers
        public bool AutoFocus { get; set; } = true;
        public int Zoom { get; set; } = 1;       // DirectShow zoom values are integers

        // Additional UVC parameters
        public double Sharpness { get; set; } = 0;
        public double Backlight { get; set; } = 0;
        public double Gain { get; set; } = 0;
        public bool AutoGain { get; set; } = true;
        public double Iris { get; set; } = 0;
        public bool AutoIris { get; set; } = true;

        // Pan/Tilt/Roll controls
        public double Pan { get; set; } = 0;
        public double Tilt { get; set; } = 0;
        public double Roll { get; set; } = 0;

        // Video format settings
        public int Width { get; set; } = 640;
        public int Height { get; set; } = 480;
        public double FrameRate { get; set; } = 30.0;
        public string ColorFormat { get; set; } = "RGB24";

        // Power line frequency (anti-flicker): 0=disabled, 1=50Hz, 2=60Hz
        public int PowerLineFrequency { get; set; } = 1;
    }
}