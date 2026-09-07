using System;
using System.IO;
using System.Windows;

namespace DevLauncherCS
{
    public static class AppLog
    {
        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    public partial class App : Application
    {
        public App()
        {
            AppLog.Log("App ctor called");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                AppLog.Log("AppDomain UnhandledException: " + e.ExceptionObject);
                DarkDialog.ShowError(null, "Error inesperado", "Fatal error:\n" + e.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                AppLog.Log("TaskScheduler UnobservedTaskException: " + e.Exception);
                e.SetObserved();
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppLog.Log("OnStartup enter");
            DispatcherUnhandledException += (sender, args) =>
            {
                AppLog.Log("DispatcherUnhandledException: " + args.Exception);
                DarkDialog.ShowError(null, "Error", "Error en DevLauncher:\n" + args.Exception.Message);
                args.Handled = true;
            };

            base.OnStartup(e);
            AppLog.Log("OnStartup base.OnStartup completed");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLog.Log($"App OnExit with code {e.ApplicationExitCode}");
            base.OnExit(e);
        }
    }
}
