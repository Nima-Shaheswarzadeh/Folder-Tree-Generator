using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Animation;

// ── Aliases to eliminate WPF ↔ WinForms ambiguity ──
using WpfMessageBox = System.Windows.MessageBox;
using WpfApplication = System.Windows.Application;
using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;

namespace TreeMaker
{
    /// <summary>
    /// Fluent-styled main window – folder tree visualiser & exporter.
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private string? _selectedFolderPath;
        private string? _generatedFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ═══════════════════════════════════════════════
        //  Custom Title-Bar Handlers
        // ═══════════════════════════════════════════════
        private void TitleBar_MouseLeftButtonDown(object sender,
            System.Windows.Input.MouseButtonEventArgs e) => DragMove();

        private void MinimizeButton_Click(object sender,
            System.Windows.RoutedEventArgs e) =>
            WindowState = System.Windows.WindowState.Minimized;

        private void MaximizeButton_Click(object sender,
            System.Windows.RoutedEventArgs e) =>
            WindowState = WindowState == System.Windows.WindowState.Maximized
                ? System.Windows.WindowState.Normal
                : System.Windows.WindowState.Maximized;

        private void CloseButton_Click(object sender,
            System.Windows.RoutedEventArgs e) => Close();

        // ═══════════════════════════════════════════════
        //  Select Folder
        // ═══════════════════════════════════════════════
        private void SelectFolderButton_Click(object sender,
            System.Windows.RoutedEventArgs e)
        {
            using var dialog = new WinFormsFolderBrowserDialog
            {
                Description = "Choose a folder to visualise",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() != WinFormsDialogResult.OK) return;

            _selectedFolderPath = dialog.SelectedPath;
            FolderPathText.Text = _selectedFolderPath;
            FolderPathText.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter
                        .ConvertFromString("#E4E4E7"));

            GenerateButton.IsEnabled = true;

            PopulateTreeView(_selectedFolderPath);
            StatusText.Text = $"Loaded — {_selectedFolderPath}";

            // Auto-expand the tree panel
            TreeToggle.IsChecked = true;
        }

        // ═══════════════════════════════════════════════
        //  Open Generated File
        // ═══════════════════════════════════════════════
        private void OpenFileButton_Click(object sender,
            System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_generatedFilePath) ||
                !File.Exists(_generatedFilePath))
            {
                WpfMessageBox.Show(
                    "No file has been generated yet.\n" +
                    "Select a folder and click Generate first.",
                    "Tree Maker",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _generatedFilePath,
                UseShellExecute = true
            });
        }

        // ═══════════════════════════════════════════════
        //  Collapsible Panel Toggle
        // ═══════════════════════════════════════════════
        private void TreeToggle_Changed(object sender,
            System.Windows.RoutedEventArgs e)
        {
            bool open = TreeToggle.IsChecked == true;

            var anim = new DoubleAnimation
            {
                To = open ? 90 : 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase
                { EasingMode = EasingMode.EaseInOut }
            };
            ArrowRotation.BeginAnimation(
                System.Windows.Media.RotateTransform.AngleProperty, anim);

            TreePanel.Visibility = open
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════
        //  Populate TreeView
        // ═══════════════════════════════════════════════
        private void PopulateTreeView(string rootPath)
        {
            FolderTreeView.Items.Clear();

            var rootDir = new DirectoryInfo(rootPath);
            var rootNode = CreateTreeNode(rootDir);
            FolderTreeView.Items.Add(rootNode);
            rootNode.IsExpanded = true;
        }

        private TreeViewItem CreateTreeNode(DirectoryInfo dir)
        {
            var node = new TreeViewItem
            {
                Header = $"📁 {dir.Name}",
                Tag = dir.FullName,
                IsExpanded = false
            };

            try
            {
                foreach (var sub in dir.GetDirectories())
                    node.Items.Add(CreateTreeNode(sub));

                foreach (var file in dir.GetFiles())
                {
                    string icon = IconFor(
                        file.Extension.ToLowerInvariant());
                    node.Items.Add(new TreeViewItem
                    {
                        Header = $"{icon} {file.Name}",
                        Tag = file.FullName
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                node.Items.Add(new TreeViewItem
                {
                    Header = "⛔ Access Denied",
                    Foreground = System.Windows.Media.Brushes.Gray
                });
            }
            catch (IOException ex)
            {
                node.Items.Add(new TreeViewItem
                {
                    Header = $"⚠️ {ex.Message}",
                    Foreground = System.Windows.Media.Brushes.Gray
                });
            }

            return node;
        }

        private static string IconFor(string ext) => ext switch
        {
            ".cs" => "🟣",
            ".xaml" => "🔵",
            ".json" => "🟡",
            ".xml" => "🟠",
            ".txt" => "📝",
            ".md" => "📖",
            ".sln" => "🏗️",
            ".csproj" => "🔧",
            ".exe" => "⚙️",
            ".dll" => "📦",
            ".png" or ".jpg" or ".jpeg"
                or ".gif" or ".bmp" => "🖼️",
            _ => "📄"
        };

        // ═══════════════════════════════════════════════
        //  Generate Tree File  (async + animated progress)
        // ═══════════════════════════════════════════════
        private async void GenerateButton_Click(object sender,
            System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath) ||
                !Directory.Exists(_selectedFolderPath))
            {
                WpfMessageBox.Show(
                    "Please select a valid folder first.",
                    "Tree Maker",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            GenerateButton.IsEnabled = false;
            SelectFolderButton.IsEnabled = false;
            OpenFileButton.IsEnabled = false;
            StatusText.Text = "Generating…";
            AnimateProgress(0);

            string outPath = Path.Combine(
                _selectedFolderPath, "Folder_Structure.txt");

            try
            {
                int total = await Task.Run(
                    () => CountItems(_selectedFolderPath));
                int done = 0;

                var sb = new StringBuilder();
                sb.AppendLine(
                    $"Folder Structure: {_selectedFolderPath}");
                sb.AppendLine(new string('═', 60));
                sb.AppendLine();

                var progress = new Progress<string>(name =>
                {
                    done++;
                    double pct = total > 0
                        ? (double)done / total * 100 : 0;
                    AnimateProgress(pct);
                    StatusText.Text = $"Processing: {name}";
                });

                await Task.Run(() =>
                    BuildTree(sb, _selectedFolderPath,
                              "", true, progress));

                await File.WriteAllTextAsync(outPath, sb.ToString());

                _generatedFilePath = outPath;
                AnimateProgress(100);
                StatusText.Text = $"✅ Saved → {outPath}";

                OpenFileButton.IsEnabled = true;

                WpfMessageBox.Show(
                    $"Tree file created!\n\n{outPath}",
                    "Tree Maker",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"Error:\n{ex.Message}", "Tree Maker",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusText.Text = "Error occurred.";
            }
            finally
            {
                GenerateButton.IsEnabled = true;
                SelectFolderButton.IsEnabled = true;
            }
        }

        // ═══════════════════════════════════════════════
        //  Animated progress helper
        // ═══════════════════════════════════════════════
        private void AnimateProgress(double percent)
        {
            double maxWidth = ProgressFill.Parent is
                System.Windows.FrameworkElement parent
                    ? parent.ActualWidth : 400;

            var anim = new DoubleAnimation
            {
                To = maxWidth * (percent / 100.0),
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase
                { EasingMode = EasingMode.EaseOut }
            };
            ProgressFill.BeginAnimation(
                System.Windows.FrameworkElement.WidthProperty, anim);
        }

        // ═══════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════
        private static int CountItems(string path)
        {
            int n = 0;
            try
            {
                n += Directory.GetFiles(path).Length;
                foreach (string d in Directory.GetDirectories(path))
                { n++; n += CountItems(d); }
            }
            catch { }
            return n;
        }

        private static void BuildTree(
            StringBuilder sb, string path, string indent,
            bool isRoot, IProgress<string> progress)
        {
            var dir = new DirectoryInfo(path);
            if (isRoot) sb.AppendLine($"{dir.Name}/");

            try
            {
                var items = new List<FileSystemInfo>();
                items.AddRange(dir.GetDirectories());
                items.AddRange(dir.GetFiles());

                for (int i = 0; i < items.Count; i++)
                {
                    bool last = i == items.Count - 1;
                    string con = last ? "└── " : "├── ";
                    string ci = indent + (last ? "    " : "│   ");

                    if (items[i] is DirectoryInfo sub)
                    {
                        sb.AppendLine($"{indent}{con}{sub.Name}/");
                        progress.Report(sub.Name);
                        BuildTree(sb, sub.FullName, ci,
                                  false, progress);
                    }
                    else if (items[i] is FileInfo f)
                    {
                        sb.AppendLine($"{indent}{con}{f.Name}");
                        progress.Report(f.Name);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                sb.AppendLine($"{indent}└── [Access Denied]");
            }
            catch (IOException ex)
            {
                sb.AppendLine($"{indent}└── [Error: {ex.Message}]");
            }
        }
    }
}