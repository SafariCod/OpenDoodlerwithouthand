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
                const double DrawingStrokeThickness = 4;
                const double TextStrokeThickness = 3;
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
                async Task AnimateGraphicAsync(GraphicModelBase graphic)
                {
                    if (graphic == null) return;
                    List<System.Windows.Shapes.Path> paths = [];
                    Geometry geometry = null;
                    UIElement element = null;
                    Brush strokeBrush = GetBrush(graphic.Color);
                    Rect geometryBounds = Rect.Empty;
                    PathGeometry pathGeometry = null;
                    double baseThickness = graphic is DrawingModel ? DrawingStrokeThickness : TextStrokeThickness;
                    double strokeThickness = baseThickness;
                    Canvas groupCanvas = new Canvas();
                    Canvas.SetLeft(groupCanvas, graphic.X);
                    Canvas.SetTop(groupCanvas, graphic.Y);
                    if (graphic is DrawingModel drawing)
                    {
                        DrawingGroup drawingGroup = drawing.ImgDrawingGroup;
                        if (drawingGroup == null && !string.IsNullOrWhiteSpace(drawing.SVGText))
                            drawingGroup = GeometryHelper.GetPathGeometryFromSVG(drawing.SVGText);
                        if (drawingGroup == null)
                            return;
                        drawingGroup = drawingGroup.Clone();
                        geometry = GeometryHelper.ConvertToGeometry(drawingGroup);
                        geometry = NormalizeGeometry(geometry, out geometryBounds);
                        pathGeometry = geometry.GetFlattenedPathGeometry();
                        geometryBounds = pathGeometry.Bounds;
                    }
                    else if (graphic is TextModel text)
                    {
                        geometry = text.TextGeometry;
                        geometry = NormalizeGeometry(geometry, out geometryBounds);
                        pathGeometry = geometry.GetFlattenedPathGeometry();
                        geometryBounds = pathGeometry.Bounds;
                    }
                    if (!geometryBounds.IsEmpty)
                    {
                        double scaleX = geometryBounds.Width > 0 ? graphic.Width / geometryBounds.Width : 1;
                        double scaleY = geometryBounds.Height > 0 ? graphic.Height / geometryBounds.Height : 1;
                        ApplyScaleAndRotation(groupCanvas, scaleX, scaleY, graphic.Rotation, geometryBounds);
                        double scale = Math.Max(scaleX, scaleY);
                        if (scale > 0)
                            strokeThickness = baseThickness / scale;
                    }
                    if (pathGeometry != null)
                    {
                        if (graphic is DrawingModel)
                        {
                            element = new System.Windows.Shapes.Path
                            {
                                Data = pathGeometry,
                                Stroke = strokeBrush,
                                StrokeThickness = strokeThickness,
                                Fill = Brushes.Transparent,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                        }
                        else if (graphic is TextModel)
                        {
                            element = new System.Windows.Shapes.Path
                            {
                                Data = pathGeometry,
                                Stroke = strokeBrush,
                                Fill = strokeBrush,
                                StrokeThickness = strokeThickness,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                        }
                        List<PathGeometry> pathGeometries = GeometryHelper.GenerateMultiplePaths(pathGeometry, false);
                        foreach (var geo in pathGeometries)
                        {
                            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
                            {
                                Data = geo,
                                Stroke = strokeBrush,
                                StrokeThickness = strokeThickness,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round,
                                StrokeDashCap = PenLineCap.Round
                            };
                            paths.Add(path);
                        }
                    }
                    canvas.Children.Add(groupCanvas);
                    if (paths.Count == 0)
                    {
                        if (element != null)
                            groupCanvas.Children.Add(element);
                        return;
                    }
                    if (graphic.Delay > 0)
                        await Task.Delay((int)(graphic.Delay * 1000));
                    GraphicModelBase animGraphic = new GraphicModelBase
                    {
                        X = 0,
                        Y = 0,
                        Duration = graphic.Duration,
                        Delay = 0
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
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    canvas.Children.Clear();
                    SceneModel scene = project.Scenes[i];
                    if (scene == null) continue;
                    if (scene.Graphics == null || scene.Graphics.Count == 0) continue;
                    var indexed = scene.Graphics
                        .Select((g, idx) => new { Graphic = g, Index = idx })
                        .Where(x => x.Graphic != null)
                        .ToList();
                    if (indexed.Count == 0) continue;
                    var allGraphics = indexed.Select(x => x.Graphic).ToList();
                    var hubOrdered = indexed
                        .Where(x => x.Graphic.Column == 0)
                        .OrderBy(x => x.Graphic.RowIndex)
                        .ThenBy(x => x.Index)
                        .Select(x => x.Graphic)
                        .ToList();

                    if (hubOrdered.Count == 0)
                    {
                        var rowOrder = indexed.Select(x => x.Graphic.RowIndex).Distinct().OrderBy(x => x).ToList();
                        foreach (int row in rowOrder)
                        {
                            List<Task> rowTasks = new List<Task>();
                            foreach (var graphic in allGraphics.Where(g => g.RowIndex == row))
                                rowTasks.Add(AnimateGraphicAsync(graphic));
                            if (rowTasks.Count > 0)
                                await Task.WhenAll(rowTasks);
                        }
                    }
                    else
                    {
                        foreach (var hub in hubOrdered)
                        {
                            List<Task> rowTasks = new List<Task> { AnimateGraphicAsync(hub) };
                            foreach (var graphic in allGraphics.Where(g => g.Column > 0 && g.RowIndex == hub.RowIndex))
                                rowTasks.Add(AnimateGraphicAsync(graphic));
                            await Task.WhenAll(rowTasks);
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
