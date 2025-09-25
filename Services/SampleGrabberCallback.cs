using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DirectShowLib;

namespace UVCCameraControl.Services
{
    public class SampleGrabberCallback : ISampleGrabberCB
    {
        private int _width;
        private int _height;
        private int _stride;

        public event EventHandler<BitmapSource>? FrameCaptured;

        public void SetImageSize(int width, int height)
        {
            _width = width;
            _height = height;
            _stride = _width * 3; // RGB24 format
        }

        public int SampleCB(double sampleTime, IMediaSample mediaSample)
        {
            return 0; // We don't use this callback
        }

        public int BufferCB(double sampleTime, IntPtr buffer, int bufferLength)
        {
            try
            {
                if (_width > 0 && _height > 0 && bufferLength > 0)
                {
                    // Create bitmap from the buffer
                    var bitmapSource = CreateBitmapSourceFromBuffer(buffer, bufferLength);
                    if (bitmapSource != null)
                    {
                        // Raise the event on UI thread
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            FrameCaptured?.Invoke(this, bitmapSource);
                        }));
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors during frame processing
            }

            return 0;
        }

        private BitmapSource? CreateBitmapSourceFromBuffer(IntPtr buffer, int bufferLength)
        {
            try
            {
                if (bufferLength <= 0)
                    return null;

                // Copy JPEG buffer to managed array
                byte[] jpegData = new byte[bufferLength];
                Marshal.Copy(buffer, jpegData, 0, bufferLength);

                // Decode JPEG using WPF's built-in decoder
                using (var memoryStream = new MemoryStream(jpegData))
                {
                    var jpegDecoder = new JpegBitmapDecoder(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    if (jpegDecoder.Frames.Count > 0)
                    {
                        var frame = jpegDecoder.Frames[0];

                        // Create a writable bitmap and freeze it for thread safety
                        var bitmapSource = new WriteableBitmap(frame);
                        bitmapSource.Freeze();

                        return bitmapSource;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}