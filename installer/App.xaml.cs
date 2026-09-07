using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace DevLauncherInstaller
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args.Length > 0 ? e.Args : Environment.GetCommandLineArgs().Skip(1).ToArray();

            if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
            {
                RunUninstaller();
                Shutdown();
                return;
            }

            if (args.Contains("--silent-install", StringComparer.OrdinalIgnoreCase))
            {
                RunSilentInstall(args);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void RunSilentInstall(string[] args)
        {
            try
            {
                string targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "DevLauncher");

                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i].Equals("--dir", StringComparison.OrdinalIgnoreCase))
                    {
                        targetDir = args[i + 1];
                        break;
                    }
                }

                bool noDesktop = args.Contains("--no-desktop", StringComparer.OrdinalIgnoreCase);

                DevLauncherInstaller.MainWindow.PerformInstallation(
                    targetDir,
                    createDesktop: !noDesktop,
                    createStartMenu: true);
            }
            catch
            {
            }
        }

        private void RunUninstaller()
        {
            if (!DarkDialog.ShowConfirm(
                null,
                "Desinstalar DevLauncher",
                "¿Estás seguro de que deseas desinstalar DevLauncher por completo?",
                confirmText: "Desinstalar",
                cancelText: "Cancelar",
                isDestructive: true))
            {
                return;
            }

            try
            {
                // 1. Eliminar accesos directos
                string desktopShortcut = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "DevLauncher.lnk");
                if (File.Exists(desktopShortcut))
                    File.Delete(desktopShortcut);

                string startMenuShortcut = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "DevLauncher.lnk");
                if (File.Exists(startMenuShortcut))
                    File.Delete(startMenuShortcut);

                // 2. Eliminar entrada en el Registro de Windows
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    {
                        key?.DeleteSubKeyTree("DevLauncher", false);
                    }
                }
                catch { }

                // 3. Auto-eliminación de la carpeta usando cmd en segundo plano
                string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string cmdScript = $"/c timeout /t 2 /nobreak > NUL & rmdir /s /q \"{installDir}\"";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdScript,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                DarkDialog.ShowInfo(
                    null,
                    "Desinstalación Completa",
                    "DevLauncher ha sido desinstalado correctamente del equipo.");
            }
            catch (Exception ex)
            {
                DarkDialog.ShowError(
                    null,
                    "Error",
                    $"Error al desinstalar: {ex.Message}");
            }
        }
    }
}
