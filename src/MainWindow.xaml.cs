using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using Microsoft.Win32;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace DevLauncherCS
{
    public partial class MainWindow : Window
    {
        private readonly MinecraftPath _minecraftPath;
        private readonly MinecraftLauncher _launcher;
        private readonly string _configFile;

        private List<string> _installedVersions = new();
        private List<string> _allVersions = new();
        private readonly List<string> _popularVersions = new()
        {
            "1.21.1", "1.20.4", "1.20.1", "1.19.4", "1.18.2", "1.16.5", "1.12.2", "1.8.9"
        };

        private bool _isLaunching = false;
        private string? _currentSkinPath;
        private string? _pendingSkinPath;

        public MainWindow()
        {
            AppLog.Log("MainWindow ctor enter");
            Loaded += (s, e) => AppLog.Log("MainWindow Loaded event");
            ContentRendered += (s, e) => AppLog.Log("MainWindow ContentRendered event");
            Closing += (s, e) => AppLog.Log("MainWindow Closing event! Cancel=" + e.Cancel);
            Closed += (s, e) => AppLog.Log("MainWindow Closed event!");

            InitializeComponent();
            AppLog.Log("MainWindow InitializeComponent finished");

            try
            {
                var iconUri = new Uri("pack://application:,,,/DevLauncher;component/icon.ico", UriKind.Absolute);
                Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
                AppLog.Log("Window Icon loaded successfully from pack URI");
            }
            catch (Exception ex)
            {
                AppLog.Log("Notice: Icon could not be loaded via pack URI: " + ex.Message);
            }

            _minecraftPath = new MinecraftPath();
            _launcher = new MinecraftLauncher(_minecraftPath);
            _configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_config.json");

            txtMcPath.Text = _minecraftPath.BasePath;

            // Enlazar evento de progreso de descarga
            _launcher.FileProgressChanged += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = $"Descargando: {args.ProgressedTasks} / {args.TotalTasks} ({args.Name})";
                    if (args.TotalTasks > 0)
                        pbProgress.Value = (double)args.ProgressedTasks / args.TotalTasks * 100;
                });
            };

            LoadConfig();
            AppLog.Log("MainWindow LoadConfig finished");
            _ = InitializeVersionsAsync();
            AppLog.Log("MainWindow ctor exit");
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    var json = File.ReadAllText(_configFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("username", out var u))
                        txtUsername.Text = u.GetString() ?? "Steve";
                    if (doc.RootElement.TryGetProperty("ram_gb", out var r))
                        sliderRam.Value = r.GetInt32();
                    if (doc.RootElement.TryGetProperty("skin_path", out var sp))
                    {
                        var sPath = sp.GetString();
                        if (!string.IsNullOrEmpty(sPath) && File.Exists(sPath))
                        {
                            LoadSkinPreview(sPath, isPending: false);
                        }
                    }
                    else
                    {
                        var defaultSkin = Path.Combine(_minecraftPath.BasePath, "skins", "current_skin.png");
                        if (File.Exists(defaultSkin))
                        {
                            LoadSkinPreview(defaultSkin, isPending: false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error cargando config: " + ex.Message);
            }
        }

        private void SaveConfig()
        {
            try
            {
                var dict = new Dictionary<string, object>
                {
                    { "username", txtUsername.Text.Trim() },
                    { "version", cbVersions.SelectedItem?.ToString() ?? "1.21.1" },
                    { "ram_gb", (int)sliderRam.Value },
                    { "skin_path", _currentSkinPath ?? "" }
                };
                File.WriteAllText(_configFile, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error guardando config: " + ex.Message);
            }
        }

        private async Task InitializeVersionsAsync()
        {
            AppLog.Log("InitializeVersionsAsync enter");
            try
            {
                txtStatus.Text = "Cargando versiones...";
                AppLog.Log("GetAllVersionsAsync calling...");
                var versions = await _launcher.GetAllVersionsAsync();
                AppLog.Log($"GetAllVersionsAsync returned {versions.Count()} versions");

                _allVersions = versions
                    .Where(v => v.Type == "release")
                    .Select(v => v.Name)
                    .ToList();

                // Filtrar las versiones instaladas localmente en la carpeta versions
                var versionsDir = Path.Combine(_minecraftPath.BasePath, "versions");
                if (Directory.Exists(versionsDir))
                {
                    _installedVersions = Directory.GetDirectories(versionsDir)
                        .Select(Path.GetFileName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList()!;
                }

                AppLog.Log("Setting UI versions list...");
                // Por defecto mostrar instaladas si hay, sino populares
                if (_installedVersions.Count > 0)
                {
                    cbVersions.ItemsSource = _installedVersions;
                    cbVersions.SelectedItem = _installedVersions[0];
                    SetFilterActive(btnFilterInstaladas);
                }
                else
                {
                    cbVersions.ItemsSource = _popularVersions;
                    cbVersions.SelectedItem = _popularVersions[0];
                    SetFilterActive(btnFilterPopulares);
                }

                txtStatus.Text = "Listo para jugar.";
                AppLog.Log("Calling UpdateInstalledTabUI...");
                UpdateInstalledTabUI();
                AppLog.Log("InitializeVersionsAsync completed successfully!");
            }
            catch (Exception ex)
            {
                AppLog.Log("InitializeVersionsAsync EXCEPTION: " + ex);
                txtStatus.Text = "Error cargando versiones: " + ex.Message;
            }
        }

        private void SetFilterActive(Button activeBtn)
        {
            btnFilterInstaladas.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
            btnFilterPopulares.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
            btnFilterTodas.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));

            activeBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46"));
        }

        private void BtnFilterInstaladas_Click(object sender, RoutedEventArgs e)
        {
            SetFilterActive(btnFilterInstaladas);
            cbVersions.ItemsSource = _installedVersions.Count > 0 ? _installedVersions : new List<string> { "Ninguna instalada" };
            if (_installedVersions.Count > 0)
                cbVersions.SelectedItem = _installedVersions[0];
        }

        private void BtnFilterPopulares_Click(object sender, RoutedEventArgs e)
        {
            SetFilterActive(btnFilterPopulares);
            cbVersions.ItemsSource = _popularVersions;
            if (_popularVersions.Count > 0)
                cbVersions.SelectedItem = _popularVersions[0];
        }

        private void BtnFilterTodas_Click(object sender, RoutedEventArgs e)
        {
            SetFilterActive(btnFilterTodas);
            cbVersions.ItemsSource = _allVersions.Count > 0 ? _allVersions : _popularVersions;
            if (_allVersions.Count > 0)
                cbVersions.SelectedItem = _allVersions[0];
        }

        private void CbVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveConfig();
        }

        private void SliderRam_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtRamValue != null)
            {
                int val = (int)e.NewValue;
                txtRamValue.Text = $"RAM: {val} GB";
                SaveConfig();
            }
        }

        private void TxtUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        // -------------------------------------------------------------
        // LANZAMIENTO DE MINECRAFT JAVA
        // -------------------------------------------------------------
        private async void BtnPlayJava_Click(object sender, RoutedEventArgs e)
        {
            if (_isLaunching) return;

            var username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                DarkDialog.ShowWarning(this, "Atención", "Por favor ingresa un nombre de usuario (Nick).");
                return;
            }

            var version = cbVersions.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(version) || version == "Ninguna instalada")
            {
                DarkDialog.ShowWarning(this, "Atención", "Por favor selecciona una versión válida.");
                return;
            }

            _isLaunching = true;
            btnPlayJava.IsEnabled = false;
            btnPlayJava.Content = "INICIANDO...";
            pbProgress.Value = 0;
            txtStatus.Text = $"Preparando Minecraft {version}...";

            LogMessage($"--- Iniciando Minecraft {version} ---");
            LogMessage($"Usuario: {username} | RAM: {(int)sliderRam.Value} GB");

            var skinToApply = (!string.IsNullOrEmpty(_currentSkinPath) && File.Exists(_currentSkinPath))
                ? _currentSkinPath
                : Path.Combine(_minecraftPath.BasePath, "skins", "current_skin.png");
            if (File.Exists(skinToApply))
            {
                try { ApplySkinToMinecraft(skinToApply); } catch { }
            }

            try
            {
                var launchOption = new MLaunchOption
                {
                    Session = MSession.CreateOfflineSession(username),
                    MaximumRamMb = (int)sliderRam.Value * 1024,
                    MinimumRamMb = Math.Max(1024, (int)sliderRam.Value * 512)
                };

                txtStatus.Text = "Descargando archivos y librerías de Mojang...";
                var process = await _launcher.CreateProcessAsync(version, launchOption);

                txtStatus.Text = "¡Lanzando Minecraft!";
                pbProgress.Value = 100;
                LogMessage("El proceso de Minecraft se ha creado correctamente.");

                process.Start();

                txtStatus.Text = "Minecraft en ejecución.";
                btnPlayJava.IsEnabled = true;
                btnPlayJava.Content = "JUGAR";
                _isLaunching = false;

                // Actualizar versiones instaladas por si se descargó una nueva
                _ = InitializeVersionsAsync();
            }
            catch (Exception ex)
            {
                LogMessage("❌ Error al iniciar: " + ex.Message);
                DarkDialog.ShowError(this, "Error al iniciar", "No se pudo iniciar el juego:\n" + ex.Message);
                btnPlayJava.IsEnabled = true;
                btnPlayJava.Content = "JUGAR";
                _isLaunching = false;
                txtStatus.Text = "Error en el lanzamiento.";
            }
        }

        private void LogMessage(string text)
        {
            Dispatcher.Invoke(() =>
            {
                txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
                txtLogs.ScrollToEnd();
            });
        }

        // -------------------------------------------------------------
        // SUBPESTAÑAS DE JAVA EDITION
        // -------------------------------------------------------------
        private void BtnSubJugar_Click(object sender, RoutedEventArgs e)
        {
            gridSubJugar.Visibility = Visibility.Visible;
            gridSubInstalaciones.Visibility = Visibility.Collapsed;
            gridSubSkins.Visibility = Visibility.Collapsed;
            gridSubLogs.Visibility = Visibility.Collapsed;

            btnSubJugar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
            btnSubInst.Background = Brushes.Transparent;
            btnSubSkins.Background = Brushes.Transparent;
            btnSubLogs.Background = Brushes.Transparent;
        }

        private void BtnSubInst_Click(object sender, RoutedEventArgs e)
        {
            gridSubJugar.Visibility = Visibility.Collapsed;
            gridSubInstalaciones.Visibility = Visibility.Visible;
            gridSubSkins.Visibility = Visibility.Collapsed;
            gridSubLogs.Visibility = Visibility.Collapsed;

            btnSubJugar.Background = Brushes.Transparent;
            btnSubInst.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
            btnSubSkins.Background = Brushes.Transparent;
            btnSubLogs.Background = Brushes.Transparent;

            UpdateInstalledTabUI();
        }

        private void BtnSubSkins_Click(object sender, RoutedEventArgs e)
        {
            gridSubJugar.Visibility = Visibility.Collapsed;
            gridSubInstalaciones.Visibility = Visibility.Collapsed;
            gridSubSkins.Visibility = Visibility.Visible;
            gridSubLogs.Visibility = Visibility.Collapsed;

            btnSubJugar.Background = Brushes.Transparent;
            btnSubInst.Background = Brushes.Transparent;
            btnSubSkins.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
            btnSubLogs.Background = Brushes.Transparent;
        }

        private void BtnQuickChangeSkin_Click(object sender, RoutedEventArgs e)
        {
            BtnNavJava_Click(this, new RoutedEventArgs());
            BtnSubSkins_Click(this, new RoutedEventArgs());
        }

        private void BtnSubLogs_Click(object sender, RoutedEventArgs e)
        {
            gridSubJugar.Visibility = Visibility.Collapsed;
            gridSubInstalaciones.Visibility = Visibility.Collapsed;
            gridSubSkins.Visibility = Visibility.Collapsed;
            gridSubLogs.Visibility = Visibility.Visible;

            btnSubJugar.Background = Brushes.Transparent;
            btnSubInst.Background = Brushes.Transparent;
            btnSubSkins.Background = Brushes.Transparent;
            btnSubLogs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
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
                DarkDialog.ShowWarning(this, "Aviso", "No se pudo abrir el navegador:\n" + ex.Message);
            }
        }

        // -------------------------------------------------------------
        // GESTIÓN DE SKINS (PERSONALIZACIÓN Y EXTRACCIÓN)
        // -------------------------------------------------------------
        private ImageSource? ExtractSkinHead(BitmapSource skinBitmap)
        {
            try
            {
                // En Minecraft 64x64 o 64x32, la cara base está en x=8, y=8, ancho=8, alto=8
                var baseHead = new CroppedBitmap(skinBitmap, new Int32Rect(8, 8, 8, 8));

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawImage(baseHead, new Rect(0, 0, 64, 64));

                    // Capa de accesorios/pelo en x=40, y=8
                    if (skinBitmap.PixelWidth >= 48)
                    {
                        try
                        {
                            var hatHead = new CroppedBitmap(skinBitmap, new Int32Rect(40, 8, 8, 8));
                            dc.DrawImage(hatHead, new Rect(0, 0, 64, 64));
                        }
                        catch { }
                    }
                }

                var rtb = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);
                RenderOptions.SetBitmapScalingMode(rtb, BitmapScalingMode.NearestNeighbor);
                return rtb;
            }
            catch (Exception ex)
            {
                AppLog.Log("Error recortando cara de skin: " + ex.Message);
                return null;
            }
        }

        private void LoadSkinPreview(string filePath, bool isPending = true)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(filePath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                var head = ExtractSkinHead(bi);

                if (isPending)
                {
                    _pendingSkinPath = filePath;
                    imgSkinFullTexture.Source = bi;
                    if (head != null)
                        imgSkinHeadPreview.Source = head;
                    txtSkinStatus.Text = "Skin seleccionada: " + Path.GetFileName(filePath);
                    txtSkinApplyResult.Text = "👉 Pulsa 'APLICAR SKIN A MIS MUNDOS' para activarla.";
                    txtSkinApplyResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                }
                else
                {
                    _currentSkinPath = filePath;
                    imgSkinFullTexture.Source = bi;
                    if (head != null)
                    {
                        imgSkinHeadPreview.Source = head;
                        imgAvatar.Source = head;
                    }
                    txtSkinStatus.Text = "Skin activa: " + Path.GetFileName(filePath);
                }
            }
            catch (Exception ex)
            {
                DarkDialog.ShowWarning(this, "Error de Skin", "No se pudo leer la imagen de la skin:\n" + ex.Message);
            }
        }

        private void BtnChooseSkinFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Seleccionar archivo de Skin de Minecraft",
                Filter = "Skin de Minecraft (*.png)|*.png|Todos los archivos (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (ofd.ShowDialog() == true)
            {
                LoadSkinPreview(ofd.FileName, isPending: true);
            }
        }

        private async void BtnDownloadSkinOnline_Click(object sender, RoutedEventArgs e)
        {
            var nick = txtPlayerSkinName.Text.Trim();
            if (string.IsNullOrEmpty(nick))
            {
                DarkDialog.ShowWarning(this, "Atención", "Por favor ingresa el Nick o apodo de un jugador.");
                return;
            }

            btnDownloadSkinOnline.IsEnabled = false;
            btnDownloadSkinOnline.Content = "Descargando...";

            try
            {
                var skinsDir = Path.Combine(_minecraftPath.BasePath, "skins");
                Directory.CreateDirectory(skinsDir);
                var destPath = Path.Combine(skinsDir, $"{nick}_skin.png");

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var bytes = await client.GetByteArrayAsync($"https://minotar.net/skin/{nick}");
                await File.WriteAllBytesAsync(destPath, bytes);

                LoadSkinPreview(destPath, isPending: true);
                txtSkinApplyResult.Text = $"✅ Skin de '{nick}' descargada. Pulsa Aplicar para usarla en tus mundos.";
                txtSkinApplyResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            catch (Exception ex)
            {
                DarkDialog.ShowWarning(this, "Aviso", $"No se pudo descargar la skin de '{nick}'. Verifica que el nombre sea correcto.\n{ex.Message}");
            }
            finally
            {
                btnDownloadSkinOnline.IsEnabled = true;
                btnDownloadSkinOnline.Content = "🌐 Descargar Skin";
            }
        }

        private void BtnApplySkin_Click(object sender, RoutedEventArgs e)
        {
            var targetPath = _pendingSkinPath ?? _currentSkinPath;
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
            {
                DarkDialog.ShowWarning(this, "Atención", "Por favor primero selecciona o descarga una skin (.png).");
                return;
            }

            try
            {
                ApplySkinToMinecraft(targetPath);
                _currentSkinPath = targetPath;
                _pendingSkinPath = null;
                SaveConfig();

                LoadSkinPreview(targetPath, isPending: false);

                txtSkinApplyResult.Text = "✅ ¡Skin aplicada con éxito! Pulsa F5 en tus mundos para verla.";
                txtSkinApplyResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                DarkDialog.ShowInfo(this, "Skin Activada", "¡Skin aplicada correctamente!\n\nTu skin personalizada ahora se cargará automáticamente en todos tus mundos individuales al pulsar F5.");
            }
            catch (Exception ex)
            {
                DarkDialog.ShowError(this, "Error", "Error aplicando skin:\n" + ex.Message);
            }
        }

        private void ApplySkinToMinecraft(string skinFilePath)
        {
            if (!File.Exists(skinFilePath)) return;

            var skinsDir = Path.Combine(_minecraftPath.BasePath, "skins");
            Directory.CreateDirectory(skinsDir);
            var activeSkinFile = Path.Combine(skinsDir, "current_skin.png");
            if (!skinFilePath.Equals(activeSkinFile, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(skinFilePath, activeSkinFile, true);
            }

            var rpFolder = Path.Combine(_minecraftPath.BasePath, "resourcepacks", "DevLauncherSkin");
            var texturesPlayerWide = Path.Combine(rpFolder, "assets", "minecraft", "textures", "entity", "player", "wide");
            var texturesPlayerSlim = Path.Combine(rpFolder, "assets", "minecraft", "textures", "entity", "player", "slim");
            var texturesEntity = Path.Combine(rpFolder, "assets", "minecraft", "textures", "entity");

            Directory.CreateDirectory(texturesPlayerWide);
            Directory.CreateDirectory(texturesPlayerSlim);
            Directory.CreateDirectory(texturesEntity);

            // Copiar skin para TODOS los modelos por defecto de Minecraft 1.20/1.21 (Steve, Alex, Ari, Efe, Kai, Makena, Noor, Sunny, Zuri)
            string[] defaultSkinNames = new[] { "steve", "alex", "ari", "efe", "kai", "makena", "noor", "sunny", "zuri" };
            foreach (var name in defaultSkinNames)
            {
                File.Copy(skinFilePath, Path.Combine(texturesPlayerWide, $"{name}.png"), true);
                File.Copy(skinFilePath, Path.Combine(texturesPlayerSlim, $"{name}.png"), true);
            }

            // Compatibilidad para versiones tradicionales (1.8 a 1.19)
            File.Copy(skinFilePath, Path.Combine(texturesEntity, "steve.png"), true);
            File.Copy(skinFilePath, Path.Combine(texturesEntity, "alex.png"), true);

            // Crear pack.mcmeta universal (compatible con todas las versiones)
            var mcmeta = @"{
  ""pack"": {
    ""pack_format"": 34,
    ""supported_formats"": {
      ""min_inclusive"": 1,
      ""max_inclusive"": 99
    },
    ""description"": ""DevLauncher Skin Personalizada""
  }
}";
            File.WriteAllText(Path.Combine(rpFolder, "pack.mcmeta"), mcmeta);
            File.Copy(skinFilePath, Path.Combine(rpFolder, "pack.png"), true);

            // Inyectar en options.txt para que quede activa sin tocar nada dentro del juego
            var optionsPath = Path.Combine(_minecraftPath.BasePath, "options.txt");
            if (File.Exists(optionsPath))
            {
                var lines = File.ReadAllLines(optionsPath);
                bool found = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("resourcePacks:"))
                    {
                        found = true;
                        lines[i] = "resourcePacks:[\"vanilla\",\"file/DevLauncherSkin\"]";
                        break;
                    }
                }
                if (!found)
                {
                    var list = lines.ToList();
                    list.Add("resourcePacks:[\"vanilla\",\"file/DevLauncherSkin\"]");
                    lines = list.ToArray();
                }
                File.WriteAllLines(optionsPath, lines);
            }
            else
            {
                File.WriteAllText(optionsPath, "resourcePacks:[\"vanilla\",\"file/DevLauncherSkin\"]\n");
            }
        }

        private void UpdateInstalledTabUI()
        {
            panelInstalledCards.Children.Clear();
            txtInstalledCount.Text = $"Versiones instaladas ({_installedVersions.Count}):";

            if (_installedVersions.Count == 0)
            {
                var lblEmpty = new TextBlock
                {
                    Text = "No hay versiones instaladas todavía. Descarga una desde la pestaña 'JUGAR'.",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 30, 0, 0)
                };
                panelInstalledCards.Children.Add(lblEmpty);
                return;
            }

            foreach (var ver in _installedVersions)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202024")),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(15, 10, 15, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var spInfo = new StackPanel();
                spInfo.Children.Add(new TextBlock { Text = $"Minecraft {ver}", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
                spInfo.Children.Add(new TextBlock { Text = "Lista para jugar offline", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 0) });
                Grid.SetColumn(spInfo, 0);
                grid.Children.Add(spInfo);

                var spBtns = new StackPanel { Orientation = Orientation.Horizontal };
                
                var btnPlayThis = new Button
                {
                    Content = "▶ Jugar",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Width = 80,
                    Height = 30,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                btnPlayThis.Click += (s, args) =>
                {
                    cbVersions.SelectedItem = ver;
                    BtnSubJugar_Click(this, new RoutedEventArgs());
                };
                spBtns.Children.Add(btnPlayThis);

                var btnOpenDir = new Button
                {
                    Content = "📁",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                    Foreground = Brushes.White,
                    Width = 34,
                    Height = 30,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                btnOpenDir.Click += (s, args) =>
                {
                    var vDir = Path.Combine(_minecraftPath.BasePath, "versions", ver);
                    if (Directory.Exists(vDir))
                        Process.Start(new ProcessStartInfo("explorer.exe", vDir));
                };
                spBtns.Children.Add(btnOpenDir);

                var btnDelete = new Button
                {
                    Content = "🗑️",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                    Foreground = Brushes.White,
                    Width = 34,
                    Height = 30,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnDelete.Click += (s, args) =>
                {
                    if (DarkDialog.ShowConfirm(this, "Confirmar", $"¿Eliminar Minecraft {ver} para liberar espacio?", confirmText: "Eliminar", cancelText: "Cancelar", isDestructive: true))
                    {
                        var vDir = Path.Combine(_minecraftPath.BasePath, "versions", ver);
                        if (Directory.Exists(vDir))
                        {
                            try
                            {
                                Directory.Delete(vDir, true);
                                _ = InitializeVersionsAsync();
                            }
                            catch (Exception ex)
                            {
                                DarkDialog.ShowError(this, "Error", "Error eliminando versión: " + ex.Message);
                            }
                        }
                    }
                };
                spBtns.Children.Add(btnDelete);

                Grid.SetColumn(spBtns, 1);
                grid.Children.Add(spBtns);
                card.Child = grid;

                panelInstalledCards.Children.Add(card);
            }
        }

        private void BtnRefreshInstalled_Click(object sender, RoutedEventArgs e)
        {
            _ = InitializeVersionsAsync();
        }

        private void BtnOpenMcFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_minecraftPath.BasePath))
                Process.Start(new ProcessStartInfo("explorer.exe", _minecraftPath.BasePath));
        }

        // -------------------------------------------------------------
        // NAVEGACIÓN PRINCIPAL (JAVA / AJUSTES)
        // -------------------------------------------------------------
        private void BtnNavJava_Click(object sender, RoutedEventArgs e)
        {
            gridJava.Visibility = Visibility.Visible;
            gridSettings.Visibility = Visibility.Collapsed;

            btnNavJava.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
            btnNavSettings.Background = Brushes.Transparent;
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            gridJava.Visibility = Visibility.Collapsed;
            gridSettings.Visibility = Visibility.Visible;

            btnNavJava.Background = Brushes.Transparent;
            btnNavSettings.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));
        }
    }
}