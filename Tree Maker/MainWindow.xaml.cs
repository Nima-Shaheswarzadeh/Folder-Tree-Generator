// Explicit aliases to avoid WPF / WinForms ambiguity
using System.IO;
using System.Text;
using System.Windows;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;
using WinFormsFolderDialog = System.Windows.Forms.FolderBrowserDialog;
using WpfMessageBox = System.Windows.MessageBox;

namespace TreeMaker;

public partial class MainWindow : System.Windows.Window
{
    private string? _selectedFolder;

    // ── Select Folder ──────────────────────────────────────
    private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinFormsFolderDialog
        {
            Description = "Select a root folder",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() != WinFormsDialogResult.OK) return;
        _selectedFolder = dlg.SelectedPath;
        LoadFolder(_selectedFolder);
    }

    // ── Generate Tree File (async, with progress) ──────────
    private async Task GenerateTreeFileAsync(string root)
    {
        var lines = new List<string>();
        await Task.Run(() => CollectLines(root, "", isLast: true, lines));

        int total = lines.Count, written = 0;
        await Task.Run(async () =>
        {
            using var w = new StreamWriter(outputFile, false, Encoding.UTF8);
            foreach (var line in lines)
            {
                await w.WriteLineAsync(line);
                written++;
                if (written % 50 == 0)
                    await Dispatcher.InvokeAsync(() =>
                        MainProgressBar.Value = written * 100.0 / total);
            }
        });

        WpfMessageBox.Show("Tree file saved!", "Done",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}