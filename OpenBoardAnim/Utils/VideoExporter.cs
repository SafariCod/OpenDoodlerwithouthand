using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Utils
{
    public class VideoExporter
    {
        private Canvas _targetCanvas;
        private string _tempImageDir;
        private int _frameRate;
        private string _outputVideoPath;
        private string _ffmpegPath;
        private bool _flipXY;
        private List<BitmapFrame> frames = [];

        public VideoExporter(Canvas canvas, int frameRate, string outputVideoPath, bool flipXY = false)
        {
            try
            {
                _targetCanvas = canvas;
                _frameRate = frameRate;
                _outputVideoPath = outputVideoPath;
                _flipXY = flipXY;
                _tempImageDir = Path.Combine(Path.GetTempPath(), $"WpfAnimationFrames_{Guid.NewGuid()}");
                _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DLLs", "ffmpeg.exe");
                if (Directory.Exists(_tempImageDir)) Directory.Delete(_tempImageDir, true); // Cleanup
                Directory.CreateDirectory(_tempImageDir);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        // Start capturing frames
        public void StartCapture()
        {
            CompositionTarget.Rendering += OnRendering;
        }

        // Stop capturing and compile the video
        public void StopCapture()
        {
            try
            {
                CompositionTarget.Rendering -= OnRendering;
                CompileVideo();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            try
            {
                var rtb = new RenderTargetBitmap(
                    (int)_targetCanvas.Width,
                    (int)_targetCanvas.Height,
                    96, 96, PixelFormats.Pbgra32
                );
                if (_flipXY)
                {
                    DrawingVisual dv = new DrawingVisual();
                    using (DrawingContext dc = dv.RenderOpen())
                    {
                        VisualBrush brush = new VisualBrush(_targetCanvas);
                        dc.PushTransform(new TranslateTransform(_targetCanvas.Width, _targetCanvas.Height));
                        dc.PushTransform(new ScaleTransform(-1, -1));
                        dc.DrawRectangle(brush, null, new Rect(0, 0, _targetCanvas.Width, _targetCanvas.Height));
                        dc.Pop();
                        dc.Pop();
                    }
                    rtb.Render(dv);
                }
                else
                {
                    rtb.Render(_targetCanvas);
                }
                frames.Add(BitmapFrame.Create(rtb));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        private void CompileVideo()
        {
            try
            {
                if (!File.Exists(_ffmpegPath))
                    throw new FileNotFoundException($"FFmpeg not found at '{_ffmpegPath}'");

                // Save as PNG
                for (int currentFrame = 0; currentFrame < frames.Count; currentFrame++)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(frames[currentFrame]);
                    string framePath = Path.Combine(_tempImageDir, $"frame_{currentFrame:D4}.png");
                    using var stream = new FileStream(framePath, FileMode.Create);
                    encoder.Save(stream);
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{_outputVideoPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}

