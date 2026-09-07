using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace DevLauncherInstaller
{
    public partial class MainWindow : Window
    {
        private string _targetDirectory = "";

        public MainWindow()
        {
            InitializeComponent();

            // Ruta de instalación predeterminada en AppData\Local\Programs\DevLauncher
            string defaultFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "DevLauncher");

            txtInstallPath.Text = defaultFolder;
            UpdateDiskSpace();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnGithub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/MrPinkFlamingo",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DarkDialog.ShowWarning(this, "Aviso", "No se pudo abrir el navegador: " + ex.Message);
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Selecciona la carpeta donde instalar DevLauncher",
                InitialDirectory = Directory.Exists(txtInstallPath.Text) 
                    ? txtInstallPath.Text 
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            if (dialog.ShowDialog() == true)
            {
                string selected = dialog.FolderName;
                if (!selected.EndsWith("DevLauncher", StringComparison.OrdinalIgnoreCase))
                {
                    selected = Path.Combine(selected, "DevLauncher");
                }
                txtInstallPath.Text = selected;
            }
        }

        private void TxtInstallPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateDiskSpace();
        }

        private void UpdateDiskSpace()
        {
            try
            {
                string path = txtInstallPath.Text.Trim();
                if (string.IsNullOrEmpty(path)) return;

                string root = Path.GetPathRoot(path) ?? "C:\\";
                var drive = new DriveInfo(root);
                if (drive.IsReady)
                {
                    double freeGb = Math.Round((double)drive.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
                    txtDiskSpace.Text = $"Espacio requerido: ~160 MB | Espacio libre en disco ({root.TrimEnd('\\')}): {freeGb} GB";
                }
            }
            catch
            {
                txtDiskSpace.Text = "Espacio requerido: ~160 MB";
            }
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            _targetDirectory = txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(_targetDirectory))
            {
                DarkDialog.ShowWarning(this, "Ruta Inválida", "Por favor especifica una ruta válida de instalación.");
                return;
            }

            // Cambiar a pantalla de progreso
            gridSetup.Visibility = Visibility.Collapsed;
            gridProgress.Visibility = Visibility.Visible;

            bool createDesktop = chkDesktopShortcut.IsChecked == true;
            bool createStartMenu = chkStartMenuShortcut.IsChecked == true;

            await Task.Run(() => PerformInstallation(_targetDirectory, createDesktop, createStartMenu, UpdateProgress));

            // Cambiar a pantalla de completado
            gridProgress.Visibility = Visibility.Collapsed;
            gridFinished.Visibility = Visibility.Visible;

            txtFinishedPath.Text = $"Ubicación: {_targetDirectory}";
            if (createDesktop && createStartMenu)
                txtFinishedShortcuts.Text = "Accesos directos creados en el Escritorio y Menú Inicio.";
            else if (createDesktop)
                txtFinishedShortcuts.Text = "Acceso directo creado en el Escritorio.";
            else if (createStartMenu)
                txtFinishedShortcuts.Text = "Acceso directo agregado al Menú Inicio.";
            else
                txtFinishedShortcuts.Text = "DevLauncher listo para ejecutarse.";
        }

        public static void PerformInstallation(string targetDir, bool createDesktop, bool createStartMenu, Action<int, string, string>? onProgress = null)
        {
            onProgress?.Invoke(10, "Creando carpeta de instalación...", targetDir);
            Directory.CreateDirectory(targetDir);

            // 1. Extraer archivos del juego
            onProgress?.Invoke(25, "Extrayendo archivos del launcher y dependencias...", "Descomprimiendo...");

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("DevLauncherInstaller.payload.zip"))
            {
                if (stream != null)
                {
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                    int count = archive.Entries.Count;
                    int i = 0;
                    foreach (var entry in archive.Entries)
                    {
                        i++;
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(Path.Combine(targetDir, entry.FullName));
                            continue;
                        }

                        string destinationPath = Path.Combine(targetDir, entry.FullName);
                        string? parentDir = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(parentDir))
                            Directory.CreateDirectory(parentDir);

                        onProgress?.Invoke(25 + (int)((i / (double)count) * 45), $"Extrayendo: {entry.Name}", entry.FullName);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
                else
                {
                    // Fallback para pruebas locales si payload.zip no está incrustado pero existe al lado o en raíz
                    string localZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload.zip");
                    if (!File.Exists(localZip))
                    {
                        localZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "installer", "payload.zip");
                    }

                    if (File.Exists(localZip))
                    {
                        ZipFile.ExtractToDirectory(localZip, targetDir, overwriteFiles: true);
                    }
                    else
                    {
                        // Copiar archivos locales directamente si existen
                        string sourceExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevLauncher.exe");
                        if (!File.Exists(sourceExe))
                            sourceExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "DevLauncher.exe");

                        if (File.Exists(sourceExe))
                        {
                            File.Copy(sourceExe, Path.Combine(targetDir, "DevLauncher.exe"), true);
                            string iconSrc = Path.ChangeExtension(sourceExe, ".ico");
                            if (File.Exists(iconSrc))
                                File.Copy(iconSrc, Path.Combine(targetDir, "icon.ico"), true);
                        }
                    }
                }
            }

            // 2. Copiar desinstalador (el instalador actual se auto-copia como Desinstalar.exe)
            onProgress?.Invoke(75, "Configurando desinstalador...", "Desinstalar.exe");
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (File.Exists(currentExe))
                {
                    string uninstallerDest = Path.Combine(targetDir, "Desinstalar.exe");
                    File.Copy(currentExe, uninstallerDest, true);
                }
            }
            catch { }

            // 3. Crear accesos directos
            string installedExe = Path.Combine(targetDir, "DevLauncher.exe");
            string installedIcon = Path.Combine(targetDir, "icon.ico");
            if (!File.Exists(installedIcon))
                installedIcon = installedExe;

            if (createDesktop)
            {
                onProgress?.Invoke(85, "Creando acceso directo en el Escritorio...", "Escritorio\\DevLauncher.lnk");
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string shortcutPath = Path.Combine(desktopPath, "DevLauncher.lnk");
                    CreateWindowsShortcut(shortcutPath, installedExe, targetDir, installedIcon, "DevLauncher");
                }
                catch { }
            }

            if (createStartMenu)
            {
                onProgress?.Invoke(90, "Creando acceso en el Menú Inicio...", "Menú Inicio\\DevLauncher.lnk");
                try
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                    Directory.CreateDirectory(startMenu);
                    string shortcutPath = Path.Combine(startMenu, "DevLauncher.lnk");
                    CreateWindowsShortcut(shortcutPath, installedExe, targetDir, installedIcon, "DevLauncher");
                }
                catch { }
            }

            // Registro en Windows
            onProgress?.Invoke(95, "Registrando aplicación...", "Registro de Windows");
            try
            {
                RegisterInWindowsUninstall(targetDir, installedExe, installedIcon);
            }
            catch { }

            onProgress?.Invoke(100, "¡Instalación completada!", "Listo");
            System.Threading.Thread.Sleep(300);
        }

        private static void CreateWindowsShortcut(string shortcutPath, string targetExePath, string workingDir, string iconPath, string description)
        {
            try
            {
                string iconArg = File.Exists(iconPath) ? iconPath : targetExePath;
                string psScript = $"$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('{shortcutPath.Replace("'", "''")}'); $s.TargetPath = '{targetExePath.Replace("'", "''")}'; $s.WorkingDirectory = '{workingDir.Replace("'", "''")}'; $s.IconLocation = '{iconArg.Replace("'", "''")},0'; $s.Description = '{description.Replace("'", "''")}'; $s.Save()";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"& {{ {psScript} }}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(6000);
            }
            catch
            {
            }
        }

        private static void RegisterInWindowsUninstall(string targetDir, string exePath, string iconPath)
        {
            using var baseKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DevLauncher");
            if (baseKey != null)
            {
                baseKey.SetValue("DisplayName", "DevLauncher");
                baseKey.SetValue("DisplayIcon", iconPath);
                baseKey.SetValue("DisplayVersion", "1.0.0");
                baseKey.SetValue("Publisher", "DevLauncher");
                baseKey.SetValue("InstallLocation", targetDir);
                baseKey.SetValue("UninstallString", $"\"{Path.Combine(targetDir, "Desinstalar.exe")}\" --uninstall");
                baseKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                baseKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        private void UpdateProgress(int percentage, string status, string detail)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = percentage;
                txtProgressStatus.Text = status;
                txtProgressDetail.Text = detail;
            });
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            if (chkLaunchAfter.IsChecked == true)
            {
                try
                {
                    string targetExe = Path.Combine(_targetDirectory, "DevLauncher.exe");
                    if (File.Exists(targetExe))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = targetExe,
                            WorkingDirectory = _targetDirectory,
                            UseShellExecute = true
                        });
                    }
                }
                catch { }
            }

            Close();
        }
    }
}
