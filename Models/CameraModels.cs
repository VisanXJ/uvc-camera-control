namespace UVCCameraControl.Models
{
    public class CameraDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
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
        public double Exposure { get; set; } = -5;
        public bool AutoExposure { get; set; } = true;
        public double Focus { get; set; } = 50;
        public bool AutoFocus { get; set; } = true;
        public double Zoom { get; set; } = 1.0;

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