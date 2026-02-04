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
                int index = 0;
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
                        if (graphic is DrawingModel drawing)
                        {
                            DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                            drawingGroup.Transform = new ScaleTransform(drawing.ResizeRatio, drawing.ResizeRatio);
                            geometry = GeometryHelper.ConvertToGeometry(drawingGroup);
                            element = new System.Windows.Shapes.Path
                            {
                                Data = geometry,
                                Stroke = strokeBrush,
                                StrokeThickness = 3,
                                Fill = Brushes.Transparent
                            };
                        }
                        else if (graphic is TextModel text)
                        {
                            geometry = text.TextGeometry;
                            element = new TextBlock()
                            {
                                Text = text.RawText,
                                Foreground = strokeBrush,
                                FontFamily = text.SelectedFontFamily,
                                FontSize = text.SelectedFontSize,
                                FontStyle = text.SelectedFontStyle,
                                FontWeight = text.SelectedFontWeight
                            };
                            //paths.Add(GetPathFromGeometry(Brushes.Black, text.TextGeometry));
                        }
                        if (element != null && graphic.Rotation != 0)
                        {
                            element.RenderTransformOrigin = new Point(0.5, 0.5);
                            element.RenderTransform = new RotateTransform(graphic.Rotation);
                        }

                        PathGeometry pathGeometry = geometry.GetFlattenedPathGeometry();

                        List<PathGeometry> pathGeometries = GeometryHelper.GenerateMultiplePaths(pathGeometry, graphic is DrawingModel);
                        foreach (var geo in pathGeometries)
                        {
                            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
                            {
                                Data = geo,
                                Stroke = strokeBrush
                            };
                            if (graphic.Rotation != 0)
                            {
                                path.RenderTransformOrigin = new Point(0.5, 0.5);
                                path.RenderTransform = new RotateTransform(graphic.Rotation);
                            }
                            paths.Add(path);
                        }
                        var example = new PathAnimationHelper(canvas, paths, graphic, null);
                        example.AnimatePathOnCanvas();

                        await example.tcs.Task;
                        if (element != null)
                        {
                            canvas.Children.Add(element);
                            Canvas.SetLeft(element, graphic.X);
                            Canvas.SetTop(element, graphic.Y);
                            int count = canvas.Children.Count - index - 1;
                            canvas.Children.RemoveRange(index, count);
                            index = canvas.Children.Count;
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
    }
}
