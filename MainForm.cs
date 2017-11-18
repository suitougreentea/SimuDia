using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace simutrans_diagram
{
    public partial class MainForm : Form
    {
        string path;
        DiagramLoader loader;
        Diagram diagram;
        Renderer renderer;
        bool ignoreCheckEvent = false;
        bool error = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void updateVisibility()
        {
            renderer.lineVisibility.Clear();
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                renderer.lineVisibility.Add(ListLine.GetItemChecked(i));
            }
        }

        private void redraw()
        {
            var bitmap = renderer.render();
            MainPicture.Image = bitmap;
            MainPicture.Size = new Size(bitmap.Width, bitmap.Height);
        }

        private void setupComponents()
        {
            ignoreCheckEvent = true;
            string previousSelectedLineName = null;
            List<string> previousCheckedLineName = new List<string>();
            List<string> previousUncheckedLineName = new List<string>();
            if (ListLine.SelectedIndex > 0) previousSelectedLineName = ListLine.Items[ListLine.SelectedIndex].ToString();
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                if (ListLine.GetItemChecked(i)) previousCheckedLineName.Add(ListLine.Items[i].ToString());
                else previousUncheckedLineName.Add(ListLine.Items[i].ToString());
            }
            
            ListLine.Items.Clear();
            ListLine.Items.Add("(General / All Lines)");
            for (var i = 0; i < diagram.lines.Count; i++)
            {
                var l = diagram.lines[i];
                ListLine.Items.Add(l.name);
                var previousChecked = previousCheckedLineName.IndexOf(l.name) >= 0;
                var previousUnchecked = previousUncheckedLineName.IndexOf(l.name) >= 0;
                ListLine.SetItemCheckState(i + 1, (previousChecked || !previousUnchecked) ? CheckState.Checked : CheckState.Unchecked);
            }
            var selectedIndex = diagram.lines.FindIndex(it => it.name == previousSelectedLineName);
            if (selectedIndex >= 0) ListLine.SetSelected(selectedIndex + 1, true);
            else ListLine.SetSelected(0, true);

            ignoreCheckEvent = false;
            ListLine.Enabled = true;
            ButtonZoomInH.Enabled = true;
            ButtonZoomOutH.Enabled = true;
            ButtonZoomInV.Enabled = true;
            ButtonZoomOutV.Enabled = true;

            updateVisibility();
            updateGlobalCheck();
        }

        private void updateGlobalCheck()
        {
            var allChecked = true;
            var allUnchecked = true;
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                var state = ListLine.GetItemChecked(i);
                allChecked &= state;
                allUnchecked &= !state;
            }
            ignoreCheckEvent = true;
            if (allChecked) ListLine.SetItemCheckState(0, CheckState.Checked);
            else if (allUnchecked) ListLine.SetItemCheckState(0, CheckState.Unchecked);
            else ListLine.SetItemCheckState(0, CheckState.Indeterminate);
            ignoreCheckEvent = false;
        }
        
        private void loadAndSetupDiagram()
        {
            error = false;
            try
            {
                loader = new DiagramLoader(path);
                diagram = loader.load();
                renderer = new Renderer(diagram);
                setupComponents();
                redraw();
            }
            catch (DiagramLoadingError e)
            {
                error = true;
                ListLine.Enabled = false;
                ButtonZoomInH.Enabled = false;
                ButtonZoomOutH.Enabled = false;
                ButtonZoomInV.Enabled = false;
                ButtonZoomOutV.Enabled = false;
                TextInfo.SelectedText = "*** Error Loading Diagram ***";
                TextInfo.SelectedText = $"At line {e.line}: {e.message}";
            }
        }

        private void onLoad(object sender, EventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            path = Path.GetFullPath(args[1]);

            loadAndSetupDiagram();

            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path));
            watcher.Filter = Path.GetFileName(path);
            watcher.SynchronizingObject = this;
            watcher.IncludeSubdirectories = false;
            watcher.Changed += (_, __) =>
            {
                System.Threading.Thread.Sleep(100);
                loadAndSetupDiagram();
            };
            watcher.EnableRaisingEvents = true;
        }

        private void ListLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListLine.SelectedIndex == -1) return;
            TextInfo.Clear();
            if (ListLine.SelectedIndex == 0)
            {

            }
            else
            {
                var index = ListLine.SelectedIndex - 1;
                var l = diagram.lines[index];
                var t = diagram.lineTimes[index];
                TextInfo.SelectedText = $"Name: {l.name}\n";
                TextInfo.SelectedText = $"Estimated trip time: {new TimeSpan(t.tripTime)}\n\n";
                for (var i = 0; i < t.list.Count; i++)
                {
                    var le = l.stations[i];
                    var te = t.list[i];
                    if (i != 0) TextInfo.SelectedText = $"{new TimeSpan(te.arrival)}-";
                    TextInfo.SelectedText = $"{new TimeSpan(te.departure)} {le.station.name}";
                    if (te.shiftNum != null) TextInfo.SelectedText = $" [shift: {te.shiftNum}]";
                    TextInfo.SelectedText = $"\n";
                    TextInfo.SelectedText = $"  | {new TimeSpan(te.tripTime)}\n";
                }
                TextInfo.SelectedText = $"{new TimeSpan(t.list.Last().departure + t.list.Last().tripTime)} {l.stations[0].station.name}";
            }
        }

        private void ListLine_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (ignoreCheckEvent) return;

            BeginInvoke(new Action(() =>
            {
                if (e.Index == 0)
                {
                    ignoreCheckEvent = true;
                    for (var i = 1; i < ListLine.Items.Count; i++)
                    {
                        ListLine.SetItemCheckState(i, e.NewValue);
                    }
                    ignoreCheckEvent = false;
                }
                else
                {
                    updateGlobalCheck();
                }
                updateVisibility();
                redraw();
            }));
        }

        private void ButtonZoomInH_Click(object sender, EventArgs e)
        {
            renderer.zoomInHorizontal();
            redraw();
        }

        private void ButtonZoomOutH_Click(object sender, EventArgs e)
        {
            renderer.zoomOutHorizontal();
            redraw();
        }

        private void ButtonZoomInV_Click(object sender, EventArgs e)
        {
            renderer.zoomInVertical();
            redraw();
        }

        private void ButtonZoomOutV_Click(object sender, EventArgs e)
        {
            renderer.zoomOutVertical();
            redraw();
        }
    }
}
