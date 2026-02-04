using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;

namespace OpenBoardAnim.Controls
{
    public class ResizeThumb : Thumb
    {
        private double originalRatio = -1;
        private double originalHeight = -1;
        private double originalWidth = -1;
        public ResizeThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
            Loaded += ResizeThumb_Loaded;
        }

        private void ResizeThumb_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;

                if (designerItem != null)
                {
                    var model = designerItem.DataContext as GraphicModelBase;
                    if (model != null && model.ResizeRatio == 1)
                    {
                        model.Height = designerItem.ActualHeight;
                        model.Width = designerItem.ActualWidth;
                    }
                    else
                    {
                        designerItem.Height = model.Height;
                        designerItem.Width = model.Width;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;

                if (designerItem != null)
                {
                    if (originalRatio < 0)
                    {
                        designerItem.Height = designerItem.ActualHeight;
                        designerItem.Width = designerItem.ActualWidth;
                        originalRatio = designerItem.ActualHeight / designerItem.ActualWidth;
                        originalHeight = designerItem.Height;
                        originalWidth = designerItem.Width;
                    }
                    var model = designerItem.DataContext as GraphicModelBase;
                    if (model != null)
                    {
                        bool isLine = model is DrawingModel dm &&
                                      !string.IsNullOrWhiteSpace(dm.Name) &&
                                      dm.Name.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0;
                        double deltaVertical, deltaHorizontal;

                        if (!isLine)
                        {
                            switch (VerticalAlignment)
                            {
                                case System.Windows.VerticalAlignment.Bottom:
                                    deltaVertical = Math.Min(-e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
                                    designerItem.Height -= deltaVertical;
                                    break;
                                case System.Windows.VerticalAlignment.Top:
                                    deltaVertical = Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
                                    model.Y += deltaVertical;
                                    designerItem.Height -= deltaVertical;
                                    break;
                                default:
                                    break;
                            }
                        }

                        switch (HorizontalAlignment)
                        {
                            case System.Windows.HorizontalAlignment.Left:
                                deltaHorizontal = Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
                                model.X += deltaHorizontal;
                                designerItem.Width -= deltaHorizontal;
                                break;
                            case System.Windows.HorizontalAlignment.Right:
                                deltaHorizontal = Math.Min(-e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
                                designerItem.Width -= deltaHorizontal;
                                break;
                            default:
                                break;
                        }
                        bool useUniform = model.UseUniformScale && !isLine;
                        if (useUniform)
                        {
                            double newRatio = designerItem.Height / designerItem.Width;
                            if (newRatio > originalRatio) designerItem.Height = originalRatio * designerItem.Width;
                            else designerItem.Width = designerItem.Height / originalRatio;
                        }
                        else if (isLine)
                        {
                            designerItem.Height = originalHeight;
                        }
                        model.ResizeRatio = designerItem.Width / originalWidth;
                        model.Height = designerItem.Height;
                        model.Width = designerItem.Width;
                    }
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}

