using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DevLauncherInstaller
{
    public enum DarkDialogType
    {
        Info,
        Warning,
        Error,
        Question
    }

    public static class DarkDialog
    {
        public static bool ShowConfirm(Window? owner, string title, string message, string confirmText = "Aceptar", string cancelText = "Cancelar", bool isDestructive = false)
        {
            return ShowInternal(owner, title, message, DarkDialogType.Question, confirmText, cancelText, isDestructive);
        }

        public static void ShowInfo(Window? owner, string title, string message, string buttonText = "Aceptar")
        {
            ShowInternal(owner, title, message, DarkDialogType.Info, buttonText, null, false);
        }

        public static void ShowWarning(Window? owner, string title, string message, string buttonText = "Entendido")
        {
            ShowInternal(owner, title, message, DarkDialogType.Warning, buttonText, null, false);
        }

        public static void ShowError(Window? owner, string title, string message, string buttonText = "Aceptar")
        {
            ShowInternal(owner, title, message, DarkDialogType.Error, buttonText, null, false);
        }

        private static bool ShowInternal(Window? owner, string title, string message, DarkDialogType type, string confirmText, string? cancelText, bool isDestructive)
        {
            Window dialog = new Window
            {
                Title = title,
                Width = 430,
                SizeToContent = SizeToContent.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                FontFamily = new FontFamily("Segoe UI, sans-serif")
            };

            if (owner != null && owner.IsVisible)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                dialog.Owner = Application.Current.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            bool result = false;

            var outerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181B")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDestructive ? "#EF4444" : "#2E3342")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 4,
                    BlurRadius = 18,
                    Opacity = 0.65
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Barra de título
            var titleBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121214")),
                CornerRadius = new CornerRadius(11, 11, 0, 0)
            };
            titleBar.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) dialog.DragMove(); };

            var titleGrid = new Grid { Margin = new Thickness(14, 0, 10, 0) };
            var lblTitle = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A1A1AA")),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            var btnClose = new Button
            {
                Content = "✕",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#71717A")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Width = 28,
                Height = 26,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnClose.Click += (s, e) => dialog.Close();

            titleGrid.Children.Add(lblTitle);
            titleGrid.Children.Add(btnClose);
            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 2. Contenido (Icono + Mensaje)
            var contentGrid = new Grid { Margin = new Thickness(20, 20, 20, 16) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string iconBg;
            string iconBorder;
            string iconChar;
            string iconFg;

            switch (type)
            {
                case DarkDialogType.Question:
                    if (isDestructive)
                    {
                        iconBg = "#331619";
                        iconBorder = "#EF4444";
                        iconChar = "!";
                        iconFg = "#EF4444";
                    }
                    else
                    {
                        iconBg = "#1A2536";
                        iconBorder = "#3B82F6";
                        iconChar = "?";
                        iconFg = "#60A5FA";
                    }
                    break;
                case DarkDialogType.Warning:
                    iconBg = "#362B16";
                    iconBorder = "#F59E0B";
                    iconChar = "⚠";
                    iconFg = "#FBBF24";
                    break;
                case DarkDialogType.Error:
                    iconBg = "#331619";
                    iconBorder = "#EF4444";
                    iconChar = "✕";
                    iconFg = "#EF4444";
                    break;
                case DarkDialogType.Info:
                default:
                    iconBg = "#162E22";
                    iconBorder = "#22C55E";
                    iconChar = "✓";
                    iconFg = "#22C55E";
                    break;
            }

            var iconBorderEl = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconBg)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconBorder)),
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var txtIcon = new TextBlock
            {
                Text = iconChar,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconFg)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorderEl.Child = txtIcon;
            Grid.SetColumn(iconBorderEl, 0);
            contentGrid.Children.Add(iconBorderEl);

            var txtMessage = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E4E7")),
                FontSize = 13,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(txtMessage, 1);
            contentGrid.Children.Add(txtMessage);

            Grid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            // 3. Barra de Botones
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 18)
            };

            if (!string.IsNullOrEmpty(cancelText))
            {
                var btnCancel = CreateModernButton(cancelText, "#27272A", "#3F3F46", "#E4E4E7");
                btnCancel.IsCancel = true;
                btnCancel.Click += (s, e) => { result = false; dialog.Close(); };
                buttonPanel.Children.Add(btnCancel);
            }

            string confirmBg = isDestructive ? "#DC2626" : "#22C55E";
            string confirmHover = isDestructive ? "#EF4444" : "#16A34A";
            var btnConfirm = CreateModernButton(confirmText, confirmBg, confirmHover, "#FFFFFF", isBold: true);
            btnConfirm.IsDefault = true;
            if (string.IsNullOrEmpty(cancelText))
                btnConfirm.IsCancel = true;
            if (!string.IsNullOrEmpty(cancelText))
                btnConfirm.Margin = new Thickness(10, 0, 0, 0);

            btnConfirm.Click += (s, e) => { result = true; dialog.Close(); };
            buttonPanel.Children.Add(btnConfirm);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            outerBorder.Child = mainGrid;
            dialog.Content = outerBorder;

            dialog.ShowDialog();
            return result;
        }

        private static Button CreateModernButton(string text, string bgHex, string hoverHex, string fgHex, bool isBold = false)
        {
            var btn = new Button
            {
                Content = text,
                Height = 34,
                MinWidth = 90,
                Padding = new Thickness(16, 0, 16, 0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                FontWeight = isBold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex))
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));
            borderFactory.AppendChild(contentPresenter);

            template.VisualTree = borderFactory;

            var triggerHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hoverHex))
            });
            template.Triggers.Add(triggerHover);

            btn.Template = template;
            return btn;
        }
    }
}
