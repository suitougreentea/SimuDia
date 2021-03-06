﻿using System;
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

namespace Suitougreentea.SimuDia
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

        private void UpdateVisibility()
        {
            renderer.lineVisibility.Clear();
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                renderer.lineVisibility.Add(ListLine.Items[i].Checked);
            }
        }

        private void Redraw()
        {
            var bitmap = renderer.Render();
            MainPicture.Image = bitmap;
            MainPicture.Size = new Size(bitmap.Width, bitmap.Height);
        }

        private void SetupComponents()
        {
            ignoreCheckEvent = true;
            string previousSelectedLineName = null;
            List<string> previousCheckedLineName = new List<string>();
            List<string> previousUncheckedLineName = new List<string>();
            ListView.SelectedIndexCollection selectedIndices = ListLine.SelectedIndices;
            if (selectedIndices.Count > 0 && selectedIndices[0] > 0) previousSelectedLineName = ListLine.Items[selectedIndices[0]].Text;
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                if (ListLine.Items[i].Checked) previousCheckedLineName.Add(ListLine.Items[i].Text);
                else previousUncheckedLineName.Add(ListLine.Items[i].Text);
            }

            ListLine.SmallImageList = new ImageList();
            ListLine.Items.Clear();
            ListLine.Items.Add("(General / All Lines)");
            for (var i = 0; i < diagram.lines.Count; i++)
            {
                var l = diagram.lines[i];
                var icon = new Bitmap(16, 16);
                var g = Graphics.FromImage(icon);
                g.Clear(Color.White);
                g.DrawLine(new Pen(l.color, l.width), 0f, 8f, 16f, 8f);
                ListLine.SmallImageList.Images.Add(icon);
                ListLine.Items.Add(new ListViewItem(new string[] { l.name, new TimeSpan(diagram.lineTimes[i].wholeTripTime).ToString() }, i));
                var previousChecked = previousCheckedLineName.IndexOf(l.name) >= 0;
                var previousUnchecked = previousUncheckedLineName.IndexOf(l.name) >= 0;
                ListLine.Items[i + 1].Checked = (previousChecked || !previousUnchecked);
            }
            var selectedIndex = diagram.lines.FindIndex(it => it.name == previousSelectedLineName);
            if (selectedIndex >= 0) ListLine.SelectedIndices.Add(selectedIndex + 1);
            else ListLine.SelectedIndices.Add(0);

            ignoreCheckEvent = false;
            ListLine.Enabled = true;
            ButtonZoomInH.Enabled = true;
            ButtonZoomOutH.Enabled = true;
            ButtonZoomInV.Enabled = true;
            ButtonZoomOutV.Enabled = true;

            UpdateVisibility();
            UpdateGlobalCheck();
        }

        private void UpdateGlobalCheck()
        {
            var allChecked = true;
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                var state = ListLine.CheckedIndices.IndexOf(i) >= 0;
                allChecked &= state;
            }
            ignoreCheckEvent = true;
            ListLine.Items[0].Checked = allChecked;
            ignoreCheckEvent = false;
        }
        
        private void LoadAndSetupDiagram()
        {
            error = false;
            try
            {
                loader = new DiagramLoader(path);
                diagram = loader.Load();
                renderer = new Renderer(diagram);
                SetupComponents();
                Redraw();
            }
            catch (DiagramLoadingError e)
            {
                error = true;
                ListLine.Enabled = false;
                ButtonZoomInH.Enabled = false;
                ButtonZoomOutH.Enabled = false;
                ButtonZoomInV.Enabled = false;
                ButtonZoomOutV.Enabled = false;
                TextInfo.Clear();
                TextInfo.SelectedText = "*** Error Loading Diagram ***\n";
                TextInfo.SelectedText = $"At line {e.line}: {e.message}";
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Count() <= 1)
            {
                OpenFileDialog dialog = new OpenFileDialog()
                {
                    Filter = "SimuDia Input File (*.simudia)|*.simudia",
                    Title = "Open File",
                    RestoreDirectory = true,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = Path.GetFullPath(dialog.FileName);
                }
                else
                {
                    Application.Exit();
                }
            }
            else path = Path.GetFullPath(args[1]);

            LoadAndSetupDiagram();

            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path))
            {
                Filter = Path.GetFileName(path),
                SynchronizingObject = this,
                IncludeSubdirectories = false
            };
            watcher.Changed += (_, __) =>
            {
                // TODO: Dirty workaround to avoid locking issue
                System.Threading.Thread.Sleep(100);
                LoadAndSetupDiagram();
            };
            watcher.EnableRaisingEvents = true;
        }

        private void ButtonZoomInH_Click(object sender, EventArgs e)
        {
            renderer.ZoomInHorizontal();
            Redraw();
        }

        private void ButtonZoomOutH_Click(object sender, EventArgs e)
        {
            renderer.ZoomOutHorizontal();
            Redraw();
        }

        private void ButtonZoomInV_Click(object sender, EventArgs e)
        {
            renderer.ZoomInVertical();
            Redraw();
        }

        private void ButtonZoomOutV_Click(object sender, EventArgs e)
        {
            renderer.ZoomOutVertical();
            Redraw();
        }

        private void ListLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndices = ListLine.SelectedIndices;
            if (selectedIndices.Count == 0) return;
            TextInfo.Clear();
            if (selectedIndices[0] == 0)
            {

            }
            else
            {
                var index = selectedIndices[0] - 1;
                var l = diagram.lines[index];
                var t = diagram.lineTimes[index];
                TextInfo.SelectedText = $"Name: {l.name}\n";
                TextInfo.SelectedText = $"Estimated trip time: {new TimeSpan(t.wholeTripTime)}\n\n";
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

        private void ListLine_Click(object sender, EventArgs e)
        {
            ListLine_SelectedIndexChanged(sender, e);
        }

        private void ListLine_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (ignoreCheckEvent) return;

            if (e.Item.Index == 0)
            {
                ignoreCheckEvent = true;
                for (var i = 1; i < ListLine.Items.Count; i++)
                {
                    ListLine.Items[i].Checked = e.Item.Checked;
                }
                ignoreCheckEvent = false;
            }
            else
            {
                UpdateGlobalCheck();
            }
            UpdateVisibility();
            Redraw();
        }
    }
}
