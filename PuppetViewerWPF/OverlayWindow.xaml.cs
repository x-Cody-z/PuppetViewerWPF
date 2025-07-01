using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System;
using System.Diagnostics;

namespace PuppetViewerWPF
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private IntPtr targetHwnd;
        private string targetWindowTitle;
        private bool isOverlayVisible = false;

        public OverlayWindow(string title)
        {
            InitializeComponent();
            targetWindowTitle = title;

            Loaded += OverlayWindow_Loaded;
            Unloaded += OverlayWindow_Unloaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT | WS_EX_LAYERED;
            SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));

            // Force topmost and visible
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void UpdateOverlayPosition()
        {
            IntPtr hwnd = FindWindowByTitle(targetWindowTitle);
            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
            {
                this.Left = rect.Left;
                this.Top = rect.Top;
                this.Width = rect.Right - rect.Left;
                this.Height = rect.Bottom - rect.Top;

                var thisHwnd = new WindowInteropHelper(this).Handle;
                SetWindowPos(thisHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

                this.Visibility = Visibility.Visible;
                isOverlayVisible = true;
            }
            else
            {
                this.Visibility = Visibility.Hidden;
                isOverlayVisible = false;
            }
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateOverlayPosition(); // manual 1-time update
            CompositionTarget.Rendering += OnRendering;

            // Apply window styles and force topmost
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void OverlayWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            IntPtr hwnd = FindWindowByTitle(targetWindowTitle);
            if (hwnd == IntPtr.Zero)
            {
                if (isOverlayVisible)
                {
                    this.Visibility = Visibility.Hidden;
                    isOverlayVisible = false;
                }
                return;
            }

            targetHwnd = hwnd;

            if (!GetWindowRect(targetHwnd, out RECT rect))
                return;

            IntPtr foreground = GetForegroundWindow();

            if (foreground == targetHwnd)
            {
                if (!isOverlayVisible)
                {
                    this.Visibility = Visibility.Visible;
                    isOverlayVisible = true;
                }

                this.Left = rect.Left;
                this.Top = rect.Top;
                this.Width = rect.Right - rect.Left;
                this.Height = rect.Bottom - rect.Top;

                var thisHwnd = new WindowInteropHelper(this).Handle;
                SetWindowPos(thisHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                if (isOverlayVisible)
                {
                    this.Visibility = Visibility.Hidden;
                    isOverlayVisible = false;
                }
            }
        }

        // Helper: Find a window by part of its title
        private static IntPtr FindWindowByTitle(string title)
        {
            foreach (IntPtr hwnd in EnumerateAllWindows())
            {
                if (GetWindowText(hwnd, out string text) && text.Contains(title))
                    return hwnd;
            }
            return IntPtr.Zero;
        }

        private static IEnumerable<IntPtr> EnumerateAllWindows()
        {
            var windows = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                windows.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        private static bool GetWindowText(IntPtr hWnd, out string text)
        {
            int length = GetWindowTextLength(hWnd);
            var builder = new System.Text.StringBuilder(length + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            text = builder.ToString();
            return text.Length > 0;
        }

        // WIN32 interop
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_LAYERED = 0x80000;

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        const UInt32 SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }


        //Overlay Visuals
        
        Color GetColorFromBrush(Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
                return solidColorBrush.Color;
            else
                return Color.FromArgb(160,0,0,0);
        }

        private TextBlock CreateEffTextBlock(string text, Brush foreground, Brush background, bool rightAlign)
        {
            //rightAlign = !rightAlign;
            Color bgColor = GetColorFromBrush(background);
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = rightAlign ? new Point(1, 0) : new Point(0, 0), // left or right start
                EndPoint = rightAlign ? new Point(0, 0) : new Point(1, 0),   // left to right or right to left
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(bgColor, 0.2),
                    new GradientStop(Color.FromArgb(0, bgColor.R, bgColor.G, bgColor.B), 0.7)
                }
            };

            return new TextBlock
            {
                Text = text,
                FontSize = 20,
                Foreground = foreground,
                Background = gradientBrush,
                Margin = new Thickness(2, 0, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = rightAlign ? TextAlignment.Right : TextAlignment.Left
            };
        }

        public void UpdateOverlay(List<string> textList)
        {
            this.Dispatcher.Invoke(() =>
            {
                PlayerPanel.Children.Clear();
                EnemyPanel.Children.Clear();

                if (textList.Count > 13)
                {
                    // Colors
                    Brush FourBrush = (Brush)new BrushConverter().ConvertFrom("#FF73ffff");
                    Brush FourBrushBG = (Brush)new BrushConverter().ConvertFrom("#CC00ccaa");
                    Brush TwoBrush = (Brush)new BrushConverter().ConvertFrom("#FF95ffb7");
                    Brush TwoBrushBG = (Brush)new BrushConverter().ConvertFrom("#CC11cc33");
                    Brush HalfBrush = (Brush)new BrushConverter().ConvertFrom("#FFFF9584");
                    Brush HalfBrushBG = (Brush)new BrushConverter().ConvertFrom("#CCbb1100");
                    Brush QuarterBrush = (Brush)new BrushConverter().ConvertFrom("#FFea62d9");
                    Brush QuarterBrushBG = (Brush)new BrushConverter().ConvertFrom("#CC660055");
                    Brush ZeroBrush = (Brush)new BrushConverter().ConvertFrom("#FFc8c8c8");
                    Brush ZeroBrushBG = (Brush)new BrushConverter().ConvertFrom("#CC222222");

                    // Player = right-aligned
                    PlayerPanel.Children.Add(CreateEffTextBlock("Type: " + textList[0], Brushes.White, Brushes.DarkGray, false));
                    PlayerPanel.Children.Add(CreateEffTextBlock(textList[1], FourBrush, FourBrushBG, false));
                    PlayerPanel.Children.Add(CreateEffTextBlock(textList[2], TwoBrush, TwoBrushBG, false));
                    PlayerPanel.Children.Add(CreateEffTextBlock(textList[4], HalfBrush, HalfBrushBG, false));
                    PlayerPanel.Children.Add(CreateEffTextBlock(textList[5], QuarterBrush, QuarterBrushBG, false));
                    PlayerPanel.Children.Add(CreateEffTextBlock(textList[6], ZeroBrush, ZeroBrushBG, false));

                    // Enemy = left-aligned
                    EnemyPanel.Children.Add(CreateEffTextBlock("Type: " + textList[7], Brushes.White, Brushes.DarkGray, true));
                    EnemyPanel.Children.Add(CreateEffTextBlock(textList[8], FourBrush, FourBrushBG, true));
                    EnemyPanel.Children.Add(CreateEffTextBlock(textList[9], TwoBrush, TwoBrushBG, true));
                    EnemyPanel.Children.Add(CreateEffTextBlock(textList[11], HalfBrush, HalfBrushBG, true));
                    EnemyPanel.Children.Add(CreateEffTextBlock(textList[12], QuarterBrush, QuarterBrushBG, true));
                    EnemyPanel.Children.Add(CreateEffTextBlock(textList[13], ZeroBrush, ZeroBrushBG, true));
                }
                else
                {
                    PlayerPanel.Children.Add(new TextBlock { Text = "data retrieve error", Foreground = Brushes.Red });
                }
            });
        }
    }
}
