using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBoardAnim.Core;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Threading.Tasks;

namespace OpenBoardAnim
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            try
            {
                RegisterGlobalExceptionHandlers();
                IServiceCollection services = new ServiceCollection();
                services.AddSingleton<DataContext>();
                services.AddSingleton<ShapeRepository>();
                services.AddSingleton<GraphicRepository>();
                services.AddSingleton<SceneRepository>();
                services.AddSingleton<ProjectRepository>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IPubSubService, PubSubService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<CacheService>();
                services.AddSingleton<StateSnapshotService>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LaunchViewModel>();
                services.AddSingleton<EditorActionsViewModel>();
                services.AddSingleton<EditorCanvasViewModel>();
                services.AddSingleton<EditorLibraryViewModel>();
                services.AddSingleton<EditorTimelineViewModel>();
                services.AddSingleton<EditorViewModel>();
                services.AddSingleton<Func<Type, ViewModel>>(sp => vMType => (ViewModel)sp.GetRequiredService(vMType));

                services.AddSingleton<MainWindow>(provider =>
                new MainWindow
                {
                    DataContext = provider.GetRequiredService<MainViewModel>()
                });
                _serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception, "DispatcherUnhandledException");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogCrash(ex, "UnhandledException");
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        }

        private static void LogCrash(Exception ex, string source)
        {
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "OpenBoardAnimLogs");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                File.WriteAllText(path, $"{source}\n{ex}");
            }
            catch
            {
                // swallow logging failures
            }
        }
    }

}
