using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Controls
{
    public class RotateThumb : Thumb
    {
        private Point _centerPoint;
        private double _startAngle;
        private double _startRotation;

        public RotateThumb()
        {
            DragStarted += RotateThumb_DragStarted;
            DragDelta += RotateThumb_DragDelta;
        }

        private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            try
            {
                Control designerItem = DataContext as Control;
                if (designerItem == null) return;

                var model = designerItem.DataContext as GraphicModelBase;
                if (model == null) return;

                _centerPoint = new Point(designerItem.ActualWidth / 2, designerItem.ActualHeight / 2);
                Point start = Mouse.GetPosition(designerItem);
                _startAngle = Math.Atan2(start.Y - _centerPoint.Y, start.X - _centerPoint.X) * 180 / Math.PI;
                _startRotation = model.Rotation;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                Control designerItem = DataContext as Control;
                if (designerItem == null) return;

                var model = designerItem.DataContext as GraphicModelBase;
                if (model == null) return;

                Point current = Mouse.GetPosition(designerItem);
                double angle = Math.Atan2(current.Y - _centerPoint.Y, current.X - _centerPoint.X) * 180 / Math.PI;
                double delta = angle - _startAngle;
                model.Rotation = _startRotation + delta;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
