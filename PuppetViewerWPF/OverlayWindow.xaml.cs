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

        public OverlayWindow(string windowTitle)
        {
            InitializeComponent();
            // Try to find the external window

            //targetHwnd = FindWindowByTitle(targetWindowTitle);
            //if (targetHwnd == IntPtr.Zero)
            //{
            //    MessageBox.Show("Target window not found.");
            //    Close();
            //    return;
            //}
            
            // Set up a timer to follow the target window
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (s, e) => UpdateOverlayPosition();
            timer.Start();

            this.targetWindowTitle = windowTitle;
        }
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private bool isOverlayVisible = false;

        private void UpdateOverlayPosition()
        {
            // Bail out early if the window is closing or has been closed
            if (!this.IsLoaded) //|| !this.IsVisible)
                return;

            IntPtr newHwnd = FindWindowByTitle(targetWindowTitle);
            if (newHwnd == IntPtr.Zero)
            {
                this.Visibility = Visibility.Hidden;
                isOverlayVisible = false;
                return;
            }

            targetHwnd = newHwnd;

            if (!GetWindowRect(targetHwnd, out RECT rect))
                return;

            // Check if target window is currently focused
            IntPtr foreground = GetForegroundWindow();

            if (foreground == targetHwnd)
            {
                // Make overlay visible if hidden
                if (!isOverlayVisible)
                {
                    this.Visibility = Visibility.Visible;
                    isOverlayVisible = true;
                }

                // Always follow position
                this.Left = rect.Left;
                this.Top = rect.Top;
                this.Width = rect.Right - rect.Left;
                this.Height = rect.Bottom - rect.Top;

                // Ensure overlay is topmost
                this.Topmost = true;
            }
            else
            {
                // Hide overlay if not focused
                if (isOverlayVisible)
                {
                    this.Visibility = Visibility.Hidden;
                    isOverlayVisible = false;
                }
            }
        }

        private IntPtr FindWindowByTitle(string title)
        {
            foreach (Process proc in Process.GetProcesses())
            {
                if (!string.IsNullOrEmpty(proc.MainWindowTitle) && proc.MainWindowTitle.Contains(title))
                    return proc.MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        // Win32 declarations
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;



        internal static class NativeMethods
        {
            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TRANSPARENT = 0x20;
            public const int WS_EX_LAYERED = 0x80000;

            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        }

        Color GetColorFromBrush(Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
                return solidColorBrush.Color;
            else
                return Color.FromArgb(160,0,0,0);
        }

        private TextBlock CreateEffTextBlock(string text, Brush foreground, Brush background, bool rightAlign)
        {
            rightAlign = !rightAlign;
            Color bgColor = GetColorFromBrush(background);
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = rightAlign ? new Point(1, 0) : new Point(0, 0), // left or right start
                EndPoint = rightAlign ? new Point(0, 0) : new Point(1, 0),   // left to right or right to left
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(bgColor, 0.3),
                    new GradientStop(Color.FromArgb(0, bgColor.R, bgColor.G, bgColor.B), 0.9)
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
