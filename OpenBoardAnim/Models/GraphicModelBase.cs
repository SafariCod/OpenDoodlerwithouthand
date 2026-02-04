using OpenBoardAnim.Core;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    [JsonDerivedType(typeof(DrawingModel), typeDiscriminator:"drawing")]
    [JsonDerivedType(typeof(TextModel), typeDiscriminator: "text")]
    public class GraphicModelBase : ObservableObject
    {
        public GraphicModelBase()
        {
            
        }
        public string Name { get; set; }
        private double x;
        public double X
        {
            get { return x; }
            set
            {
                x = value;
                OnPropertyChanged();
            }
        }

        private double y;
        public double Y
        {
            get { return y; }
            set
            {
                y = value;
                OnPropertyChanged();
            }
        }

        private double _delay = 0;
        public double Delay
        {
            get { return _delay; }
            set
            {
                _delay = value;
                OnPropertyChanged();
            }
        }

        private double _duration = 1;
        public double Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        private double _height = 100;
        public double Height
        {
            get { return _height; }
            set
            {
                _height = value;
                OnPropertyChanged();
            }
        }

        private double _width = 100;
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }

        private double _resizeRatio = 1;
        public double ResizeRatio
        {
            get { return _resizeRatio; }
            set
            {
                _resizeRatio = value;
                OnPropertyChanged();
            }
        }

        private double _rotation = 0;
        public double Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                OnPropertyChanged();
            }
        }

        private string _color = "#000000";
        public string Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }

        private bool _useUniformScale = true;
        public bool UseUniformScale
        {
            get { return _useUniformScale; }
            set
            {
                _useUniformScale = value;
                OnPropertyChanged();
            }
        }

        public virtual GraphicModelBase Clone()
        {
            return new GraphicModelBase();
        }
    }
}
