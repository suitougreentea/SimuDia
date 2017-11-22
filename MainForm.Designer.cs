namespace Suitougreentea.SimuDia
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.MainPicture = new System.Windows.Forms.PictureBox();
            this.MainSplit = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.ButtonZoomOutV = new System.Windows.Forms.Button();
            this.ButtonZoomInV = new System.Windows.Forms.Button();
            this.ButtonZoomOutH = new System.Windows.Forms.Button();
            this.ButtonZoomInH = new System.Windows.Forms.Button();
            this.PanelPicture = new System.Windows.Forms.Panel();
            this.SubSplit = new System.Windows.Forms.SplitContainer();
            this.ListLine = new System.Windows.Forms.ListView();
            this.NameHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.TimeHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.TextInfo = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.MainPicture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).BeginInit();
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.PanelPicture.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SubSplit)).BeginInit();
            this.SubSplit.Panel1.SuspendLayout();
            this.SubSplit.Panel2.SuspendLayout();
            this.SubSplit.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainPicture
            // 
            this.MainPicture.Location = new System.Drawing.Point(0, 0);
            this.MainPicture.Margin = new System.Windows.Forms.Padding(0);
            this.MainPicture.Name = "MainPicture";
            this.MainPicture.Size = new System.Drawing.Size(101, 59);
            this.MainPicture.TabIndex = 0;
            this.MainPicture.TabStop = false;
            // 
            // MainSplit
            // 
            this.MainSplit.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplit.Location = new System.Drawing.Point(0, 0);
            this.MainSplit.Name = "MainSplit";
            // 
            // MainSplit.Panel1
            // 
            this.MainSplit.Panel1.Controls.Add(this.tableLayoutPanel1);
            this.MainSplit.Panel1.Controls.Add(this.PanelPicture);
            // 
            // MainSplit.Panel2
            // 
            this.MainSplit.Panel2.Controls.Add(this.SubSplit);
            this.MainSplit.Size = new System.Drawing.Size(507, 437);
            this.MainSplit.SplitterDistance = 354;
            this.MainSplit.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.ButtonZoomOutV, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.ButtonZoomInV, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.ButtonZoomOutH, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.ButtonZoomInH, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 406);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(350, 28);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // ButtonZoomOutV
            // 
            this.ButtonZoomOutV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtonZoomOutV.Location = new System.Drawing.Point(261, 0);
            this.ButtonZoomOutV.Margin = new System.Windows.Forms.Padding(0);
            this.ButtonZoomOutV.Name = "ButtonZoomOutV";
            this.ButtonZoomOutV.Size = new System.Drawing.Size(89, 28);
            this.ButtonZoomOutV.TabIndex = 2;
            this.ButtonZoomOutV.TabStop = false;
            this.ButtonZoomOutV.Text = "↕-";
            this.ButtonZoomOutV.UseVisualStyleBackColor = true;
            this.ButtonZoomOutV.Click += new System.EventHandler(this.ButtonZoomOutV_Click);
            // 
            // ButtonZoomInV
            // 
            this.ButtonZoomInV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtonZoomInV.Location = new System.Drawing.Point(174, 0);
            this.ButtonZoomInV.Margin = new System.Windows.Forms.Padding(0);
            this.ButtonZoomInV.Name = "ButtonZoomInV";
            this.ButtonZoomInV.Size = new System.Drawing.Size(87, 28);
            this.ButtonZoomInV.TabIndex = 2;
            this.ButtonZoomInV.TabStop = false;
            this.ButtonZoomInV.Text = "↕+";
            this.ButtonZoomInV.UseVisualStyleBackColor = true;
            this.ButtonZoomInV.Click += new System.EventHandler(this.ButtonZoomInV_Click);
            // 
            // ButtonZoomOutH
            // 
            this.ButtonZoomOutH.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtonZoomOutH.Location = new System.Drawing.Point(87, 0);
            this.ButtonZoomOutH.Margin = new System.Windows.Forms.Padding(0);
            this.ButtonZoomOutH.Name = "ButtonZoomOutH";
            this.ButtonZoomOutH.Size = new System.Drawing.Size(87, 28);
            this.ButtonZoomOutH.TabIndex = 2;
            this.ButtonZoomOutH.TabStop = false;
            this.ButtonZoomOutH.Text = "↔-";
            this.ButtonZoomOutH.UseVisualStyleBackColor = true;
            this.ButtonZoomOutH.Click += new System.EventHandler(this.ButtonZoomOutH_Click);
            // 
            // ButtonZoomInH
            // 
            this.ButtonZoomInH.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtonZoomInH.Location = new System.Drawing.Point(0, 0);
            this.ButtonZoomInH.Margin = new System.Windows.Forms.Padding(0);
            this.ButtonZoomInH.Name = "ButtonZoomInH";
            this.ButtonZoomInH.Size = new System.Drawing.Size(87, 28);
            this.ButtonZoomInH.TabIndex = 2;
            this.ButtonZoomInH.TabStop = false;
            this.ButtonZoomInH.Text = "↔+";
            this.ButtonZoomInH.UseVisualStyleBackColor = true;
            this.ButtonZoomInH.Click += new System.EventHandler(this.ButtonZoomInH_Click);
            // 
            // PanelPicture
            // 
            this.PanelPicture.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelPicture.AutoScroll = true;
            this.PanelPicture.Controls.Add(this.MainPicture);
            this.PanelPicture.Location = new System.Drawing.Point(0, 0);
            this.PanelPicture.Margin = new System.Windows.Forms.Padding(0);
            this.PanelPicture.Name = "PanelPicture";
            this.PanelPicture.Size = new System.Drawing.Size(350, 406);
            this.PanelPicture.TabIndex = 4;
            // 
            // SubSplit
            // 
            this.SubSplit.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.SubSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubSplit.Location = new System.Drawing.Point(0, 0);
            this.SubSplit.Name = "SubSplit";
            this.SubSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // SubSplit.Panel1
            // 
            this.SubSplit.Panel1.Controls.Add(this.ListLine);
            // 
            // SubSplit.Panel2
            // 
            this.SubSplit.Panel2.Controls.Add(this.TextInfo);
            this.SubSplit.Size = new System.Drawing.Size(149, 437);
            this.SubSplit.SplitterDistance = 111;
            this.SubSplit.TabIndex = 0;
            // 
            // ListLine
            // 
            this.ListLine.CheckBoxes = true;
            this.ListLine.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.NameHeader,
            this.TimeHeader});
            this.ListLine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListLine.FullRowSelect = true;
            this.ListLine.HideSelection = false;
            this.ListLine.LabelWrap = false;
            this.ListLine.Location = new System.Drawing.Point(0, 0);
            this.ListLine.MultiSelect = false;
            this.ListLine.Name = "ListLine";
            this.ListLine.ShowGroups = false;
            this.ListLine.Size = new System.Drawing.Size(145, 107);
            this.ListLine.TabIndex = 1;
            this.ListLine.UseCompatibleStateImageBehavior = false;
            this.ListLine.View = System.Windows.Forms.View.Details;
            this.ListLine.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.ListLine_ItemChecked);
            this.ListLine.SelectedIndexChanged += new System.EventHandler(this.ListLine_SelectedIndexChanged);
            this.ListLine.Click += new System.EventHandler(this.ListLine_Click);
            // 
            // NameHeader
            // 
            this.NameHeader.Text = "Name";
            this.NameHeader.Width = 200;
            // 
            // TimeHeader
            // 
            this.TimeHeader.Text = "Time";
            this.TimeHeader.Width = 120;
            // 
            // TextInfo
            // 
            this.TextInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextInfo.Location = new System.Drawing.Point(0, 0);
            this.TextInfo.Name = "TextInfo";
            this.TextInfo.ReadOnly = true;
            this.TextInfo.Size = new System.Drawing.Size(145, 318);
            this.TextInfo.TabIndex = 1;
            this.TextInfo.Text = "";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 437);
            this.Controls.Add(this.MainSplit);
            this.Name = "MainForm";
            this.Text = "SimuDia Diagram Viewer 0.7.5";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.MainPicture)).EndInit();
            this.MainSplit.Panel1.ResumeLayout(false);
            this.MainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).EndInit();
            this.MainSplit.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.PanelPicture.ResumeLayout(false);
            this.SubSplit.Panel1.ResumeLayout(false);
            this.SubSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SubSplit)).EndInit();
            this.SubSplit.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox MainPicture;
        private System.Windows.Forms.SplitContainer MainSplit;
        private System.Windows.Forms.SplitContainer SubSplit;
        private System.Windows.Forms.Panel PanelPicture;
        private System.Windows.Forms.Button ButtonZoomInV;
        private System.Windows.Forms.Button ButtonZoomOutV;
        private System.Windows.Forms.Button ButtonZoomInH;
        private System.Windows.Forms.Button ButtonZoomOutH;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.RichTextBox TextInfo;
        private System.Windows.Forms.ListView ListLine;
        private System.Windows.Forms.ColumnHeader NameHeader;
        private System.Windows.Forms.ColumnHeader TimeHeader;
    }
}

