using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenBoardAnim.ViewModels
{
    public class EditorActionsViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        private readonly INavigationService _navigation;
        private readonly CacheService _cache;
        private readonly IDialogService _dialog;

        public EditorActionsViewModel(IPubSubService pubSub, INavigationService navigation, CacheService Cache,
            IDialogService dialog)
        {
            try
            {
                _pubSub = pubSub;
                pubSub.Subscribe(SubTopic.SceneChanged, SceneChangedHandler);
                _navigation = navigation;
                _cache = Cache;
                _dialog = dialog;
                CloseProjectCommand = new RelayCommand(execute: o => CloseProject(), canExecute: o => true);
                SaveProjectCommand = new RelayCommand(execute: o => SaveProject(), canExecute: o => true);
                ExportProjectCommand = new RelayCommand(execute: o => ExportProject(), canExecute: o => true);
                PreviewProjectCommand = new RelayCommand(execute: o => PreviewProject(), canExecute: o => true);
                DeleteItemCommand = new RelayCommand(execute: o => DeleteItem(), canExecute: o => SelectedGraphic != null);
                MoveUpCommand = new RelayCommand(execute: o => MoveUp(), canExecute: o => SelectedGraphic != null);
                MoveDownCommand = new RelayCommand(execute: o => MoveDown(), canExecute: o => SelectedGraphic != null);
                MoveLeftCommand = new RelayCommand(execute: o => MoveLeft(), canExecute: o => SelectedGraphic != null);
                MoveRightCommand = new RelayCommand(execute: o => MoveRight(), canExecute: o => SelectedGraphic != null);
                LaunchSceneSettingsCommand = new RelayCommand(execute: o => LaunchSceneSettings(), canExecute: o => CurrentScene != null);
                LaunchProjectSettingsCommand = new RelayCommand(execute: o => LaunchProjectSettings(), canExecute: o => true);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LaunchSceneSettings()
        {
            try
            {
                _ = _dialog.ShowDialog(DialogType.SceneSettings, CurrentScene);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LaunchProjectSettings()
        {
            try
            {

                _ = _dialog.ShowDialog(DialogType.ProjectSettings, Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }

        }

        private void MoveUp()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                if (model.RowIndex <= 0) return;
                model.RowIndex -= 1;
                SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void MoveDown()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                model.RowIndex += 1;
                SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void MoveLeft()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                if (model.Column <= 0) return;
                model.Column -= 1;
                SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void MoveRight()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                model.Column += 1;
                SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DeleteItem()
        {
            try
            {
                if (SelectedGraphic != null)
                    CurrentScene?.Graphics.Remove(SelectedGraphic);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void PreviewProject()
        {
            try
            {
                _ = _dialog.ShowDialog(DialogType.PreviewProject, Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private async void ExportProject()
        {
            try
            {
                _pubSub.Publish(SubTopic.ProjectExporting, true);
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "MP4 Video (*.mp4)|*.mp4",
                    FileName = "output.mp4"
                };
                if (saveFileDialog.ShowDialog() != true)
                {
                    _pubSub.Publish(SubTopic.ProjectExporting, false);
                    return;
                }
                using (var host = new HwndSource(new HwndSourceParameters
                {
                    WindowStyle = 0x800000, // WS_POPUP (invisible window)
                    Width = 1,
                    Height = 1,
                    PositionX = -10000,    // Position off-screen
                    PositionY = -10000,
                }))
                {
                    System.Windows.Controls.Canvas canvas = new();
                    canvas.Background = Brushes.White;
                    canvas.Height = 540;
                    canvas.Width = 960;
                    host.RootVisual = canvas;

                    // Force layout and render passes
                    canvas.Measure(new Size(canvas.Width, canvas.Height));
                    canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
                    canvas.UpdateLayout();
                    //window.Show();
                    await PreviewAndExportHandler.RunAnimationsOnCanvas(Project, canvas, true, saveFileDialog.FileName);
                }
                _pubSub.Publish(SubTopic.ProjectExporting, false);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneChangedHandler(object obj)
        {
            try
            {
                SceneModel scene = (SceneModel)obj;
                CurrentScene = scene;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SaveProject()
        {
            try
            {
                if (string.IsNullOrEmpty(Project.Path))
                {
                    SaveFileDialog saveFileDialog = new()
                    {
                        Filter = "Project file (*.obap)|*.obap",
                    };
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        _cache.SaveNewProject(Project, saveFileDialog.FileName);
                    }
                    else
                        return;
                }
                _cache.UpdateExistingProject(Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CloseProject()
        {
            try
            {

                _navigation.NavigateTo<LaunchViewModel>();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        public ProjectDetails Project { get; set; }
        public ICommand CloseProjectCommand { get; set; }
        public ICommand DeleteItemCommand { get; set; }
        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }
        public ICommand MoveLeftCommand { get; set; }
        public ICommand MoveRightCommand { get; set; }
        public ICommand SaveProjectCommand { get; set; }
        public ICommand ExportProjectCommand { get; set; }
        public ICommand PreviewProjectCommand { get; set; }
        public ICommand LaunchSceneSettingsCommand { get; set; }
        public ICommand LaunchProjectSettingsCommand { get; set; }
        private SceneModel _currentScene;

        private double _actionsPanelWidth = 340;
        public double ActionsPanelWidth
        {
            get { return _actionsPanelWidth; }
            set
            {
                _actionsPanelWidth = value < 220 ? 220 : value;
                OnPropertyChanged();
            }
        }

        public SceneModel CurrentScene
        {
            get { return _currentScene; }
            set
            {
                if (_currentScene?.Graphics != null)
                    DetachSceneGraphics(_currentScene.Graphics);
                _currentScene = value;
                if (_currentScene?.Graphics != null)
                    AttachSceneGraphics(_currentScene.Graphics);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SceneGraphics));
                UpdateColumns();
            }
        }

        public BindingList<GraphicModelBase> SceneGraphics => CurrentScene?.Graphics;
        public GraphicModelBase SelectedGraphic { get; set; }

        private BindingList<GraphicModelBase> _attachedGraphics;
        private readonly HashSet<GraphicModelBase> _subscribedGraphics = new HashSet<GraphicModelBase>();

        private void AttachSceneGraphics(BindingList<GraphicModelBase> graphics)
        {
            _attachedGraphics = graphics;
            graphics.ListChanged += SceneGraphics_ListChanged;
            SyncGraphicSubscriptions();
        }

        private void DetachSceneGraphics(BindingList<GraphicModelBase> graphics)
        {
            graphics.ListChanged -= SceneGraphics_ListChanged;
            foreach (var g in _subscribedGraphics)
                g.PropertyChanged -= Graphic_PropertyChanged;
            _subscribedGraphics.Clear();
            _attachedGraphics = null;
        }

        private void SceneGraphics_ListChanged(object sender, ListChangedEventArgs e)
        {
            SyncGraphicSubscriptions();
            UpdateColumns();
        }

        private void Graphic_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GraphicModelBase.Column) || e.PropertyName == nameof(GraphicModelBase.RowIndex))
                UpdateColumns();
        }

        private void SyncGraphicSubscriptions()
        {
            if (_attachedGraphics == null) return;
            var current = new HashSet<GraphicModelBase>(_attachedGraphics);
            foreach (var g in _subscribedGraphics.ToList())
            {
                if (!current.Contains(g))
                {
                    g.PropertyChanged -= Graphic_PropertyChanged;
                    _subscribedGraphics.Remove(g);
                }
            }
            foreach (var g in current)
            {
                if (_subscribedGraphics.Add(g))
                    g.PropertyChanged += Graphic_PropertyChanged;
            }
        }

        public class RowSlot
        {
            public int RowIndex { get; set; }
            public GraphicModelBase Item { get; set; }
        }

        public class ColumnGroup
        {
            public int Column { get; set; }
            public ObservableCollection<RowSlot> Rows { get; set; }
        }

        private ObservableCollection<ColumnGroup> _columns = new ObservableCollection<ColumnGroup>();
        public ObservableCollection<ColumnGroup> Columns
        {
            get { return _columns; }
            set
            {
                _columns = value;
                OnPropertyChanged();
            }
        }

        private void UpdateColumns()
        {
            if (CurrentScene?.Graphics == null)
            {
                Columns = new ObservableCollection<ColumnGroup>();
                return;
            }

            var indexed = CurrentScene.Graphics
                .Select((g, idx) => new { Graphic = g, Index = idx })
                .Where(x => x.Graphic != null)
                .ToList();

            var hubOrdered = indexed
                .Where(x => x.Graphic.Column == 0)
                .OrderBy(x => x.Graphic.RowIndex)
                .ThenBy(x => x.Index)
                .ToList();

            var rowOrder = hubOrdered.Count > 0
                ? hubOrdered.Select(x => x.Graphic.RowIndex).ToList()
                : indexed.Select(x => x.Graphic.RowIndex).Distinct().OrderBy(x => x).ToList();

            var groups = indexed
                .GroupBy(x => x.Graphic.Column)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var items = g.OrderBy(x => x.Index).Select(x => x.Graphic).ToList();
                    var used = new HashSet<GraphicModelBase>();
                    var rows = new List<RowSlot>();

                    if (g.Key == 0 && hubOrdered.Count > 0)
                    {
                        foreach (var hub in hubOrdered)
                        {
                            rows.Add(new RowSlot { RowIndex = hub.Graphic.RowIndex, Item = hub.Graphic });
                        }
                    }
                    else
                    {
                        foreach (var row in rowOrder)
                        {
                            var match = items.FirstOrDefault(item => !used.Contains(item) && item.RowIndex == row);
                            if (match != null) used.Add(match);
                            rows.Add(new RowSlot { RowIndex = row, Item = match });
                        }
                    }

                    return new ColumnGroup
                    {
                        Column = g.Key,
                        Rows = new ObservableCollection<RowSlot>(rows)
                    };
                })
                .ToList();

            Columns = new ObservableCollection<ColumnGroup>(groups);
        }
    }
}
