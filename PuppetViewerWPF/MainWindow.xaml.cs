using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppetViewerWPF;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Formats.Asn1.AsnWriter;

namespace PuppetViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemWatcher _watcher;
        private JObject _jsonObject;
        private List<string[]> _oldEnemyData;
        private string _player;
        private OverlayWindow _overlayWindow;

        public MainWindow()
        {
            InitializeComponent();
        }


        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Define the directory and file to monitor
            string fullPath = ReadCsvPathFromConfig();
            string directoryPath = System.IO.Path.GetDirectoryName(fullPath);
            string fileName = System.IO.Path.GetFileName(fullPath);

            _oldEnemyData = new List<string[]>();
            _player = "Enemy";

            try
            {
                string filePath = "DollData.json";
                // Read the entire JSON file into a string
                string json = File.ReadAllText(filePath);

                // Parse the JSON string into a JObject
                _jsonObject = JObject.Parse(json);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"File DollData.json not found.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading JSON file: {ex.Message}");
            }

            ShowOverlay();


            // Create a new FileSystemWatcher and set its properties
            _watcher = new FileSystemWatcher
            {
                Path = directoryPath,
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite // Monitor changes to the file's LastWrite time
            };

            // Add event handlers
            _watcher.Changed += OnChanged;

            // Begin watching
            _watcher.EnableRaisingEvents = true;

            Console.WriteLine($"Monitoring changes to: {System.IO.Path.Combine(directoryPath, fileName)}");
        }

        // Define the non-static event handler
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            updateList();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Clean up the watcher when the window is closed
            if (_watcher != null)
            {
                _watcher.Changed -= OnChanged;
                _watcher.Dispose();
            }

            // Clean up hotkey
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            base.OnClosed(e);
        }

        class TextToggleState
        {
            public Inline[] StateA { get; set; }
            public Inline[] StateB { get; set; }
            public bool IsExpanded { get; set; }
        }

        private static readonly Dictionary<string, Color> TypeColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "Void",     Color.FromRgb(255, 153, 204) },  // light pink
            { "Fire",     Color.FromRgb(255,  85,  85) },  // red
            { "Water",    Color.FromRgb( 85,  85, 255) },  // blue
            { "Nature",   Color.FromRgb( 85, 255,  85) },  // green
            { "Earth",    Color.FromRgb(170, 136, 102) },  // brown
            { "Steel",    Color.FromRgb(136, 136, 136) },  // grey
            { "Wind",     Color.FromRgb(204, 255, 102) },  // light yellow-green
            { "Electric", Color.FromRgb(255, 153,   0) },  // orange
            { "Light",    Color.FromRgb(255, 255, 102) },  // yellow
            { "Dark",     Color.FromRgb( 85,  85,  85) },  // dark grey
            { "Nether",   Color.FromRgb(119,  85, 119) },  // muted purple
            { "Poison",   Color.FromRgb(255, 102, 255) },  // pink-purple
            { "Fighting", Color.FromRgb(255, 153,  51) },  // orange-brown
            { "Illusion", Color.FromRgb(255, 102, 102) },  // soft red
            { "Sound",    Color.FromRgb(255, 204,  51) },  // gold
            { "Warped",   Color.FromRgb( 85, 136, 255) },  // medium blue
            { "Dream",    Color.FromRgb(255,  85, 136) },  // red-pink
            { "None",     Colors.Transparent }
        };

        private void ShowOverlay()
        {
            if (_overlayWindow == null || !_overlayWindow.IsVisible)
            {
                string targetWindowTitle = "TPDP"; // must match part of the window title
                _overlayWindow = new OverlayWindow(targetWindowTitle);
                _overlayWindow.Show();
                updateList();
            }
            else
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }
        }

        //Shortcut Functionality
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        // You can also use MOD_ALT = 0x0001, MOD_WIN = 0x0008

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook); // hook into window messages

            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)KeyInterop.VirtualKeyFromKey(Key.C));
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ShowOverlay();
                handled = true;
            }

            return IntPtr.Zero;
        }


        private Brush GetTypeGradientBrush(string type1, string type2)
        {
            var color1 = TypeColors.ContainsKey(type1) ? TypeColors[type1] : Colors.Gray;
            var color2 = TypeColors.ContainsKey(type2) ? TypeColors[type2] : Colors.Gray;
            var colorMid = Color.FromRgb(17, 17, 34);

            if (type2.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                //return new SolidColorBrush(color1);
                return new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                {
                    //new GradientStop(color1, 0.65),
                    //new GradientStop(colorMid, 1.0)
                    new GradientStop(Color.FromArgb(111, color1.R, color1.G, color1.B), 0.65),
                    new GradientStop(colorMid, 1.0)
                }
                };
            }

            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    //new GradientStop(color1, 0.15),
                    //new GradientStop(color2, 0.65),
                    //new GradientStop(colorMid, 1.0)
                    new GradientStop(Color.FromArgb(111, color1.R, color1.G, color1.B), 0.15),
                    new GradientStop(Color.FromArgb(111, color2.R, color2.G, color2.B), 0.65),
                    new GradientStop(colorMid, 1.0)
                }
            };
        }

        private Brush GetTypeGradientBrushText(string type1, string type2)
        {
            var color1 = TypeColors.ContainsKey(type1) ? TypeColors[type1] : Colors.Gray;
            var color2 = TypeColors.ContainsKey(type2) ? TypeColors[type2] : Colors.Gray;
            var colorMid = Color.FromRgb(17, 17, 34);

            if (type2.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(color1);
            }

            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(color1, 0.2),
                    new GradientStop(color2, 0.8)
                }
            };
        }

        private int GetPuppetActiveIndex(List<string[]> csv, int player)
        {
            return int.Parse(csv[17 + player][0]);
        }

        private string GetPuppetAbilityName(int ability_index)
        {
            var lines = File.ReadAllLines(@"AbilityData.csv");

            if (ability_index < 0 || ability_index >= lines.Length)
                return null; // or throw exception

            string line = lines[ability_index]; // index matches line number
            string[] parts = line.Split(',');

            if (parts.Length >= 2)
                return parts[1]; // second column is name

            return null;
        }

        private string GetPuppetAbilityDesc(int ability_index)
        {
            var lines = File.ReadAllLines(@"AbilityData.csv");

            if (ability_index < 0 || ability_index >= lines.Length)
                return null; // or throw exception

            string line = lines[ability_index]; // index matches line number
            string[] parts = line.Split(',');

            if (parts.Length >= 2)
                return parts[2]; // third column is desc

            return null;
        }
        private List<int> GetPuppetAbilityIndi(string puppet_id, string style_index)
        {
            string id = puppet_id;
            int index = int.Parse(style_index);

            // Get the styles array for the specified id
            JToken puppet = _jsonObject["puppets"]?.FirstOrDefault(p => (string)p["id"] == id);

            if (puppet != null)
            {
                JArray styles = (JArray)puppet["styles"];
                if (index >= 0 && index < styles.Count)
                {
                    JArray abilityIndi = (JArray)styles[index]["abilities"];
                    return abilityIndi.ToObject<List<int>>();
                }
                else
                {
                    MessageBox.Show("Style index out of range.");
                    return null;
                }
            }
            else
            {
                MessageBox.Show("Puppet not found.");
                return null;
            }
        }
        private List<int> GetPuppetBaseStats(string puppet_id, string style_index)
        {
            string id = puppet_id;
            int index = int.Parse(style_index);

            // Get the styles array for the specified id
            JToken puppet = _jsonObject["puppets"]?.FirstOrDefault(p => (string)p["id"] == id);

            if (puppet != null)
            {
                JArray styles = (JArray)puppet["styles"];
                if (index >= 0 && index < styles.Count)
                {
                    JArray baseStats = (JArray)styles[index]["base_stats"];
                    return baseStats.ToObject<List<int>>();
                }
                else
                {
                    MessageBox.Show("Style index out of range.");
                    return null;
                }
            }
            else
            {
                MessageBox.Show("Puppet not found.");
                return null;
            }
        }
        private string[] GetPuppetData(string puppet_id, string style_index)
        {
            string[] result = new string[2];
            
            string id = puppet_id;
            int index = int.Parse(style_index);

            // Get the styles array for the specified id
            JArray styles = (JArray)_jsonObject["puppets"].FirstOrDefault(p => (string)p["id"] == id)?["styles"];

            if (styles != null && index < styles.Count)
            {
                // Get element1 and element2 from the style at the specified index
                string element1 = (string)styles[index]["element1"];
                string element2 = (string)styles[index]["element2"];
                result = [element1, element2];
            }
            else
            {
                MessageBox.Show("Style not found for the given id and index.");
            }
            return result;
        }

        private void ClearRows()
        {
            this.Dispatcher.Invoke(() =>
            {
                ContentGrid.Children.Clear();
                ContentGrid.RowDefinitions.Clear();
            });
        }

        Color ColorFromHSV(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60.0 % 2) - 1));
            double m = value - c;

            double r1 = 0, g1 = 0, b1 = 0;

            if (hue < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (hue < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (hue < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (hue < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (hue < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)((r1 + m) * 255);
            byte g = (byte)((g1 + m) * 255);
            byte b = (byte)((b1 + m) * 255);

            return Color.FromRgb(r, g, b);
        }

        SolidColorBrush GetStrengthBrush(int value)
        {
            // Clamp between 1 and 250
            value = Math.Max(1, Math.Min(250, value));

            // Map value (1–250) to hue (240 to 0 degrees)
            double hue = 50 + (value*2);
            double saturation = Math.Clamp((value-25) / 125.0, 0, 1);
            double brightness = 1.0;

            Color rgb = ColorFromHSV(hue, saturation, brightness);
            return new SolidColorBrush(rgb);
        }

        private void GenerateRows(List<string[]> PuppetData)
        {
            this.Dispatcher.Invoke(() =>
            {
                ClearRows();

                // Ensure the grid has the necessary columns
                if (ContentGrid.ColumnDefinitions.Count == 0)
                {
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // for image
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // for text
                }

                if (PuppetData.Count > 0)
                {
                    InitText.Visibility = Visibility.Collapsed;

                    int currentRow = 0;

                    for (int i = 0; i < PuppetData.Count; i++)
                    {
                        string[] types = GetPuppetData(PuppetData[i][0], PuppetData[i][1]);
                        Brush typeGrad = GetTypeGradientBrush(types[0], types[1]);
                        Brush typeGradText = GetTypeGradientBrushText(types[0], types[1]);

                        // === Row container with background ===
                        ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        Border rowBorder = new Border
                        {  
                            Background = new SolidColorBrush(Color.FromRgb(18, 18, 36)),
                            Padding = new Thickness(5)
                        };

                        Grid rowGrid = new Grid();
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        string imgPath = System.IO.Path.Combine(exeDir, "img", PuppetData[i][0].PadLeft(3, '0') + "_00.png");

                        //rowGrid.Tag = PuppetData[i];

                        Border imageBorder = new Border
                        {
                            Background = typeGrad,
                        };

                        Image image = new Image
                        {
                            Height = 150,
                            Width = 150,
                            Margin = new Thickness(5)
                        };
                        try
                        {
                            image.Source = new BitmapImage(new Uri(imgPath, UriKind.Absolute));
                        }
                        catch
                        {
                            image.Source = new BitmapImage(new Uri(System.IO.Path.Combine(exeDir, "img", "000_00.png"), UriKind.Absolute));
                        }

                        imageBorder.Child = image;
                        Grid.SetColumn(image, 0);
                        rowGrid.Children.Add(imageBorder);

                        // TextBlock with formatted Runs
                        TextBlock textBlock = new TextBlock
                        {
                            FontSize = 24,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5),
                            TextWrapping = TextWrapping.Wrap
                        };

                        Run Name = new Run(PuppetData[i][4] + "\n")
                        {
                            FontSize = 32,
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF66A5DF"))
                        };
                        textBlock.Inlines.Add(Name);

                        string typesText = "";
                        if (types[1] == "None") { typesText = types[0] + "\n\n"; }
                        else { typesText = types[0] + ", " + types[1] + "\n\n"; }

                        Run Types = new Run(typesText)
                        {
                            FontStyle = FontStyles.Italic,
                            FontSize = 22,
                            //Foreground = (Brush)(new BrushConverter().ConvertFrom("#FFdddddd"))
                            Foreground = typeGradText
                        };
                        textBlock.Inlines.Add(Types);

                        //Type effectiveness
                        var grouped = TypeEffectiveness.GroupAttackTypesByEffectiveness(types[0], types[1]);
                        string[] results = new string[6];
                        for (int j = 0; j < grouped.Count; j++)
                        {
                            results[j] = string.Join(", ", grouped[j]);
                            if (results[j] != "") { results[j] = " " + results[j] + " "; }
                        }
                        string fourX = results[0];
                        string twoX = results[1];
                        string oneX = results[2];
                        string halfX = results[3];
                        string quarterX = results[4];
                        string zeroX = results[5];


                        Run eff4 = new Run(fourX)
                        {
                            FontSize = 20,
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF22ffdd")),
                            Background = (Brush)(new BrushConverter().ConvertFrom("#3322ffdd"))
                        };
                        textBlock.Inlines.Add(eff4);

                        Run eff2 = new Run(twoX)
                        {
                            FontSize = 20,
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF44ff66")),
                            Background = (Brush)(new BrushConverter().ConvertFrom("#3344ff66"))
                        };
                        textBlock.Inlines.Add(eff2);

                        Run eff05 = new Run(halfX)
                        {
                            FontSize = 20,
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FFEE4433")),
                            Background = (Brush)(new BrushConverter().ConvertFrom("#33EE4433"))
                        };
                        textBlock.Inlines.Add(eff05);

                        Run eff025 = new Run(quarterX)
                        {
                            FontSize = 20,
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF991188")),
                            Background = (Brush)(new BrushConverter().ConvertFrom("#33991188"))
                        };
                        textBlock.Inlines.Add(eff025);

                        Run eff0 = new Run(zeroX)
                        {
                            FontSize = 20,
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF666666")),
                            Background = (Brush)(new BrushConverter().ConvertFrom("#33666666"))
                        };
                        textBlock.Inlines.Add(eff0);


                        
                        
                        List<Inline> stateBruns = new List<Inline>();

                        string statHeadings = "HP".PadRight(5) + "FOA".PadRight(5) + "FOD".PadRight(5) + "SPA".PadRight(5) + "SPD".PadRight(5) + "SPE";

                        Run statHeadingRun = new Run(statHeadings + "\n")
                        {
                            FontSize = 24,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = (Brush)(new BrushConverter().ConvertFrom("#FFDDDDDD"))
                        };
                        stateBruns.Add(statHeadingRun);

                        
                        List<int> baseStats = GetPuppetBaseStats(PuppetData[i][0], PuppetData[i][1]);
                        for (int j = 0; j < baseStats.Count; j++)
                        {
                            SolidColorBrush strengthColour = GetStrengthBrush(baseStats[j]);
                            SolidColorBrush bgStrengthColour = new SolidColorBrush(Color.FromArgb(120, strengthColour.Color.R, strengthColour.Color.G, strengthColour.Color.B));
                            Run stat = new Run(baseStats[j].ToString().PadRight(5))
                            {
                                FontSize=24,
                                FontFamily = new FontFamily("Consolas"),
                                Foreground = strengthColour,
                                Background = bgStrengthColour
                            };
                            stateBruns.Add(stat);
                        }

                        stateBruns.Add(new Run("\n\n") { FontSize = 20 });


                        List<int> abilitiesIndex = GetPuppetAbilityIndi(PuppetData[i][0], PuppetData[i][1]);
                        for (int j = 0; j < abilitiesIndex.Count(); j++)
                        {
                            string abilityText = "";
                            if(j < abilitiesIndex.Count - 1)
                            {
                                abilityText = GetPuppetAbilityName(abilitiesIndex[j]) + "\t";
                            }
                            else
                            {
                                abilityText = GetPuppetAbilityName(abilitiesIndex[j]);
                            }

                            Run abilityRun = new Run(abilityText);
                            Hyperlink abilityLink = new Hyperlink(abilityRun)
                            {
                                Foreground = Brushes.White,
                                ToolTip = GetPuppetAbilityDesc(abilitiesIndex[j]),
                                TextDecorations = null,
                                FontStyle = FontStyles.Italic
                                
                            };
                            stateBruns.Add(abilityLink);
                        }

                        Inline[] statsInline = new Inline[stateBruns.Count];
                        for (int j = 0; j < stateBruns.Count(); j++)
                        {
                            statsInline[j] = stateBruns[j];
                        }

                        textBlock.Tag = new TextToggleState
                        {
                            StateA = new Inline[] { Name, Types, eff4, eff2, eff05, eff025, eff0 },
                            StateB = statsInline,
                            IsExpanded = false
                        };

                        rowGrid.MouseLeftButtonUp += (sender, e) =>
                        {
                            // Get the TextBlock from the row
                            var row = (Grid)sender;
                            var textBlock = row.Children.OfType<TextBlock>().FirstOrDefault();
                            if (textBlock != null && textBlock.Tag is TextToggleState tag)
                            {
                                textBlock.Inlines.Clear();

                                if (tag.IsExpanded)
                                {
                                    foreach (Inline inline in tag.StateA)
                                        textBlock.Inlines.Add(inline);
                                }
                                else
                                {
                                    foreach (Inline inline in tag.StateB)
                                        textBlock.Inlines.Add(inline);
                                }

                                tag.IsExpanded = !tag.IsExpanded;
                            }

                            e.Handled = true;
                        };


                        Grid.SetColumn(textBlock, 1);
                        rowGrid.Children.Add(textBlock);

                        rowBorder.Child = rowGrid;

                        Grid.SetRow(rowBorder, currentRow);
                        Grid.SetColumnSpan(rowBorder, 2); // Span across both columns
                        ContentGrid.Children.Add(rowBorder);

                        currentRow++;

                        // === Border between rows ===
                        if (i < PuppetData.Count - 1)
                        {
                            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });

                            Border separator = new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(6, 6, 17)),
                                Height = 10,
                                HorizontalAlignment = HorizontalAlignment.Stretch
                            };
                            Grid.SetRow(separator, currentRow);
                            Grid.SetColumnSpan(separator, 2);
                            ContentGrid.Children.Add(separator);

                            currentRow++;
                        }
                    }
                }
                else
                {
                    InitText.Visibility = Visibility.Visible;
                }
            });
        }



        private string ReadCsvPathFromConfig()
        {
            string configPath = "config.ini";

            if (!File.Exists(configPath))
            {
                MessageBox.Show("config.ini not found.");
                return null;
            }

            // Read all lines and find the one that starts with "csv_path="
            foreach (string line in File.ReadAllLines(configPath))
            {
                if (line.StartsWith("csv_path="))
                {
                    return line.Substring("csv_path=".Length);
                }
            }

            MessageBox.Show("csv_path not found in config.ini.");
            return null;
        }

        private List<string[]> LoadCSV()
        {
            string filePath = ReadCsvPathFromConfig();
            if (filePath == null)
            {
                return null;
            }

            // List to store arrays representing each row in the CSV
            List<string[]> csvData = new List<string[]>();

            // Read the CSV file line by line
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line into fields based on comma delimiter
                        string[] fields = line.Split(',');
                        csvData.Add(fields);
                    }
                }
                return csvData;
            }
            catch
            {
                return null;
            }
        }

        private List<string[]> LoadCSVEnemyData(string player)
        {
            string filePath = ReadCsvPathFromConfig();
            if (filePath == null)
            {
                return null;
            }

            List<string[]> rows = new List<string[]>();
            bool inEnemySection = false;
            try
            {

                foreach (var line in File.ReadLines(filePath))
                {
                    if (line.Trim() == player)
                    {
                        inEnemySection = true;
                        continue;
                    }

                    if (!inEnemySection)
                        continue;

                    if (string.IsNullOrWhiteSpace(line))
                        break;

                    string[] fields = line.Split(',');
                    if (fields[0].Trim() == "0")
                        continue;

                    rows.Add(fields);
                }

                return rows;
            }
            catch
            {
                return null;
            }
        }

        private void updateList()
        {
            List<string[]> puppetsData = LoadCSVEnemyData(_player);
            if (puppetsData == null)
            {
                puppetsData = _oldEnemyData;
            }
            else if (puppetsData == _oldEnemyData)
            {
                return;
            }
            else
            {
                _oldEnemyData = puppetsData;
                GenerateRows(puppetsData);

                List<string[]> csv = LoadCSV();
                List<string[]> playerData = LoadCSVEnemyData("Player");
                List<string[]> enemyData = LoadCSVEnemyData("Enemy");
                if (_overlayWindow != null && csv != null && playerData != null && enemyData != null)
                {
                    _overlayWindow.UpdateOverlay(GetOverlayData(csv, enemyData, playerData));
                }
            }
        }

        private List<string> GetOverlayData(List<string[]> csv, List<string[]> enemyData, List<string[]> playerData)
        {
            List<string> result = new List<string>();
            
            int playerActivePuppetIndex = GetPuppetActiveIndex(csv, 0);
            int enemyActivePuppetIndex = GetPuppetActiveIndex(csv, 1);

            string[] playerTypes = GetPuppetData(playerData[playerActivePuppetIndex][0], playerData[playerActivePuppetIndex][1]);
            string[] enemyTypes = GetPuppetData(enemyData[enemyActivePuppetIndex][0], enemyData[enemyActivePuppetIndex][1]);

            string playerTypesText = "";
            string enemyTypesText = "";

            if (playerTypes[1] == "None") { playerTypesText = playerTypes[0]; }
            else { playerTypesText = playerTypes[0] + ", " + playerTypes[1]; }
            if (enemyTypes[1] == "None") { enemyTypesText = enemyTypes[0]; }
            else { enemyTypesText = enemyTypes[0] + ", " + enemyTypes[1]; }

            // Player type effectiveness
            var playerGrouped = TypeEffectiveness.GroupAttackTypesByEffectiveness(playerTypes[0], playerTypes[1]);
            string[] playerEffResults = new string[6];
            for (int j = 0; j < playerGrouped.Count; j++)
            {
                playerEffResults[j] = string.Join(", ", playerGrouped[j]);
                if (playerEffResults[j] != "") { playerEffResults[j] = " " + playerEffResults[j] + " "; }
            }
            result.Add(playerTypesText);
            foreach (string s in playerEffResults) { result.Add(s); }

            // Enemy type effectiveness
            var enemyGrouped = TypeEffectiveness.GroupAttackTypesByEffectiveness(enemyTypes[0], enemyTypes[1]);
            string[] enemyEffResults = new string[6];
            for (int j = 0; j < enemyGrouped.Count; j++)
            {
                enemyEffResults[j] = string.Join(", ", enemyGrouped[j]);
                if (enemyEffResults[j] != "") { enemyEffResults[j] = " " + enemyEffResults[j] + " "; }
            }
            result.Add(enemyTypesText);
            foreach (string s in enemyEffResults) { result.Add(s); }


            return result;
            //String list should be formatted like this:

            //player type text
            //player 4x
            //player 2x
            //player 1x
            //player 0.5x
            //player 0.25x
            //player 0x
            
            //enemy type text
            //enemy 4x
            //enemy 2x
            //enemy 1x
            //enemy 0.5x
            //enemy 0.25x
            //enemy 0x
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            updateList();
        }

        /*
        private void old_O_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";
            openFileDialog.Title = "Select your puppets.csv file";

            if (openFileDialog.ShowDialog() == true)
            {
                string csvFilePath = openFileDialog.FileName;

                // Path to your config.ini file (same directory as the executable)
                string configPath = "config.ini";

                // Write the path to the file (overwrite or create)
                File.WriteAllText(configPath, $"csv_path={csvFilePath}");

                MessageBox.Show($"CSV path saved to config.ini:\n{csvFilePath}");
            }
        }
        */

        private void O_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "exe files (*.exe)|*.exe";
            openFileDialog.Title = "Select TPDP Executable";

            if (openFileDialog.ShowDialog() == true)
            {
                string exeFilePath = openFileDialog.FileName;
                string csvFilePath = System.IO.Path.GetDirectoryName(exeFilePath) + "\\puppets.csv";

                // Path to your config.ini file (same directory as the executable)
                string configPath = "config.ini";

                // Write the path to the file (overwrite or create)
                File.WriteAllText(configPath, $"csv_path={csvFilePath}");

                //MessageBox.Show($"CSV path saved to config.ini:\n{csvFilePath}");
                MessageBox.Show("Game path updated");
            }
        }

        private void Switch_Click(object sender, RoutedEventArgs e)
        {
            if(_player == "Enemy")
            {
                _player = "Player";
                updateList();
            }
            else
            {
                _player = "Enemy";
                updateList();
            }
        }
    }
}