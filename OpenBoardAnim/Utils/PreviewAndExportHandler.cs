using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOPath = System.IO.Path;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    public class PreviewAndExportHandler
    {
        public static async Task RunAnimationsOnCanvas(ProjectDetails project, Canvas canvas, bool isExport, string outputVideoPath = null)
        {
            try
            {
                if (project == null) return;
                VideoExporter exporter = null;
                if (isExport)
                {
                    if (string.IsNullOrWhiteSpace(outputVideoPath))
                    {
                        outputVideoPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.mp4");
                    }
                    exporter = new(canvas, 30, outputVideoPath, flipXY: false);
                    exporter.StartCapture();
                }
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    canvas.Children.Clear();
                    SceneModel scene = project.Scenes[i];
                    if (scene == null) continue;
                    for (int j = 0; j < scene.Graphics.Count; j++)
                    {
                        GraphicModelBase graphic = scene.Graphics[j];
                        await Task.Delay((int)graphic.Delay * 1000);
                        List<System.Windows.Shapes.Path> paths = [];
                        Geometry geometry = null;
                        UIElement element = null;
                        Brush strokeBrush = GetBrush(graphic.Color);
                        Rect geometryBounds = Rect.Empty;
                        PathGeometry pathGeometry = null;
                        double strokeThickness = graphic is DrawingModel ? 3 : 1;
                        Canvas groupCanvas = new Canvas();
                        Canvas.SetLeft(groupCanvas, graphic.X);
                        Canvas.SetTop(groupCanvas, graphic.Y);
                        if (graphic is DrawingModel drawing)
                        {
                            DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                            geometry = GeometryHelper.ConvertToGeometry(drawingGroup);
                            geometry = NormalizeGeometry(geometry, out geometryBounds);
                            pathGeometry = geometry.GetFlattenedPathGeometry();
                            geometryBounds = pathGeometry.Bounds;
                            element = new System.Windows.Shapes.Path
                            {
                                Data = pathGeometry,
                                Stroke = strokeBrush,
                                StrokeThickness = strokeThickness,
                                Fill = Brushes.Transparent
                            };
                        }
                        else if (graphic is TextModel text)
                        {
                            geometry = text.TextGeometry;
                            geometry = NormalizeGeometry(geometry, out geometryBounds);
                            pathGeometry = geometry.GetFlattenedPathGeometry();
                            geometryBounds = pathGeometry.Bounds;
                            element = new System.Windows.Shapes.Path
                            {
                                Data = pathGeometry,
                                Stroke = strokeBrush,
                                Fill = strokeBrush,
                                StrokeThickness = strokeThickness
                            };
                            //paths.Add(GetPathFromGeometry(Brushes.Black, text.TextGeometry));
                        }
                        if (!geometryBounds.IsEmpty)
                        {
                            double scaleX = geometryBounds.Width > 0 ? graphic.Width / geometryBounds.Width : 1;
                            double scaleY = geometryBounds.Height > 0 ? graphic.Height / geometryBounds.Height : 1;
                            ApplyScaleAndRotation(groupCanvas, scaleX, scaleY, graphic.Rotation, geometryBounds);
                        }

                        List<PathGeometry> pathGeometries = GeometryHelper.GenerateMultiplePaths(pathGeometry, false);
                        foreach (var geo in pathGeometries)
                        {
                            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
                            {
                                Data = geo,
                                Stroke = strokeBrush,
                                StrokeThickness = strokeThickness
                            };
                            paths.Add(path);
                        }
                        canvas.Children.Add(groupCanvas);
                        GraphicModelBase animGraphic = new GraphicModelBase
                        {
                            X = 0,
                            Y = 0,
                            Duration = graphic.Duration
                        };
                        var example = new PathAnimationHelper(groupCanvas, paths, animGraphic, null);
                        example.AnimatePathOnCanvas();

                        await example.tcs.Task;
                        if (element != null)
                        {
                            groupCanvas.Children.Clear();
                            groupCanvas.Children.Add(element);
                        }
                    }
                }
                await Task.Delay(500);
                if (isExport)
                    exporter.StopCapture();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        private static Brush GetBrush(string colorString)
        {
            try
            {
                if (BrushConverter.TryParseColor(colorString, out Color color))
                    return new SolidColorBrush(color);
            }
            catch
            {
                // ignore parse errors and fall back to black
            }
            return Brushes.Black;
        }

        private static Geometry NormalizeGeometry(Geometry geometry, out Rect bounds)
        {
            bounds = geometry?.Bounds ?? Rect.Empty;
            if (bounds.IsEmpty || geometry == null)
                return geometry;

            Geometry clone = geometry.Clone();
            TransformGroup group = new TransformGroup();
            if (clone.Transform != null && !clone.Transform.Value.IsIdentity)
                group.Children.Add(clone.Transform);
            group.Children.Add(new TranslateTransform(-bounds.Left, -bounds.Top));
            clone.Transform = group;
            bounds = new Rect(0, 0, bounds.Width, bounds.Height);
            return clone;
        }

        private static void ApplyScaleAndRotation(UIElement element, double scaleX, double scaleY, double rotation, Rect bounds)
        {
            TransformGroup group = new TransformGroup();
            group.Children.Add(new ScaleTransform(scaleX, scaleY));
            if (rotation != 0)
            {
                double centerX = (bounds.Width * scaleX) / 2;
                double centerY = (bounds.Height * scaleY) / 2;
                group.Children.Add(new RotateTransform(rotation, centerX, centerY));
            }
            element.RenderTransform = group;
        }
    }
}
