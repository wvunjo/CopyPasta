using System.Windows;
using System.Windows.Threading;

namespace CopyPastaNative
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show(
                "CopyPasta ran into a problem and could not complete that action. Your snippet file was not modified unless a save had already finished.",
                "CopyPasta",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
