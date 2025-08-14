namespace EDMXMigrationTool
{
    partial class frmCentralForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCentralForm));
            pnlTop = new TableLayoutPanel();
            lblFile = new Label();
            panel1 = new Panel();
            txtFile = new TextBox();
            bntFileSelection = new Button();
            panel2 = new Panel();
            txtDestination = new TextBox();
            btnFindDestination = new Button();
            lblDestination = new Label();
            pnlBottom = new Panel();
            btnRun = new Button();
            txtLog = new TextBox();
            bkWorker = new System.ComponentModel.BackgroundWorker();
            lblNamespace = new Label();
            txtNamespace = new TextBox();
            pnlTop.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.ColumnCount = 2;
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            pnlTop.ColumnStyles.Add(new ColumnStyle());
            pnlTop.Controls.Add(lblFile, 0, 0);
            pnlTop.Controls.Add(panel1, 1, 0);
            pnlTop.Controls.Add(panel2, 1, 1);
            pnlTop.Controls.Add(lblDestination, 0, 1);
            pnlTop.Controls.Add(lblNamespace, 0, 2);
            pnlTop.Controls.Add(txtNamespace, 1, 2);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Margin = new Padding(4);
            pnlTop.Name = "pnlTop";
            pnlTop.RowCount = 3;
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            pnlTop.Size = new Size(1235, 150);
            pnlTop.TabIndex = 0;
            // 
            // lblFile
            // 
            lblFile.AutoSize = true;
            lblFile.Dock = DockStyle.Fill;
            lblFile.Location = new Point(4, 0);
            lblFile.Margin = new Padding(4, 0, 4, 0);
            lblFile.Name = "lblFile";
            lblFile.Size = new Size(242, 50);
            lblFile.TabIndex = 0;
            lblFile.Text = "EDMX File";
            lblFile.TextAlign = ContentAlignment.MiddleRight;
            // 
            // panel1
            // 
            panel1.Controls.Add(txtFile);
            panel1.Controls.Add(bntFileSelection);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(254, 4);
            panel1.Margin = new Padding(4);
            panel1.Name = "panel1";
            panel1.Size = new Size(978, 42);
            panel1.TabIndex = 1;
            // 
            // txtFile
            // 
            txtFile.BackColor = SystemColors.Window;
            txtFile.Dock = DockStyle.Fill;
            txtFile.Location = new Point(0, 0);
            txtFile.Margin = new Padding(4);
            txtFile.Multiline = true;
            txtFile.Name = "txtFile";
            txtFile.ReadOnly = true;
            txtFile.Size = new Size(860, 42);
            txtFile.TabIndex = 0;
            txtFile.Text = "C:\\Users\\sqsan\\OneDrive\\Desktop\\TMSBD.edmx";
            // 
            // bntFileSelection
            // 
            bntFileSelection.Dock = DockStyle.Right;
            bntFileSelection.Location = new Point(860, 0);
            bntFileSelection.Margin = new Padding(4);
            bntFileSelection.Name = "bntFileSelection";
            bntFileSelection.Size = new Size(118, 42);
            bntFileSelection.TabIndex = 1;
            bntFileSelection.Text = "...";
            bntFileSelection.UseVisualStyleBackColor = true;
            bntFileSelection.Click += bntFileSelection_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(txtDestination);
            panel2.Controls.Add(btnFindDestination);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(254, 54);
            panel2.Margin = new Padding(4);
            panel2.Name = "panel2";
            panel2.Size = new Size(978, 42);
            panel2.TabIndex = 2;
            // 
            // txtDestination
            // 
            txtDestination.BackColor = SystemColors.Window;
            txtDestination.Dock = DockStyle.Fill;
            txtDestination.Location = new Point(0, 0);
            txtDestination.Margin = new Padding(4);
            txtDestination.Multiline = true;
            txtDestination.Name = "txtDestination";
            txtDestination.ReadOnly = true;
            txtDestination.Size = new Size(860, 42);
            txtDestination.TabIndex = 0;
            txtDestination.Text = "C:\\Users\\sqsan\\OneDrive\\Desktop\\Result";
            // 
            // btnFindDestination
            // 
            btnFindDestination.Dock = DockStyle.Right;
            btnFindDestination.Location = new Point(860, 0);
            btnFindDestination.Margin = new Padding(4);
            btnFindDestination.Name = "btnFindDestination";
            btnFindDestination.Size = new Size(118, 42);
            btnFindDestination.TabIndex = 1;
            btnFindDestination.Text = "...";
            btnFindDestination.UseVisualStyleBackColor = true;
            btnFindDestination.Click += btnFindDestination_Click;
            // 
            // lblDestination
            // 
            lblDestination.AutoSize = true;
            lblDestination.Dock = DockStyle.Fill;
            lblDestination.Location = new Point(4, 50);
            lblDestination.Margin = new Padding(4, 0, 4, 0);
            lblDestination.Name = "lblDestination";
            lblDestination.Size = new Size(242, 50);
            lblDestination.TabIndex = 3;
            lblDestination.Text = "Destination Folder";
            lblDestination.TextAlign = ContentAlignment.MiddleRight;
            // 
            // pnlBottom
            // 
            pnlBottom.Controls.Add(btnRun);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 738);
            pnlBottom.Margin = new Padding(4);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new Size(1235, 66);
            pnlBottom.TabIndex = 1;
            // 
            // btnRun
            // 
            btnRun.Dock = DockStyle.Right;
            btnRun.Location = new Point(907, 0);
            btnRun.Margin = new Padding(4);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(328, 66);
            btnRun.TabIndex = 0;
            btnRun.Text = "Convert";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Enabled = false;
            txtLog.Location = new Point(0, 150);
            txtLog.Margin = new Padding(4);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1235, 588);
            txtLog.TabIndex = 2;
            // 
            // bkWorker
            // 
            bkWorker.DoWork += bkWorker_DoWork;
            bkWorker.RunWorkerCompleted += bkWorker_RunWorkerCompleted;
            // 
            // lblNamespace
            // 
            lblNamespace.AutoSize = true;
            lblNamespace.Dock = DockStyle.Fill;
            lblNamespace.Location = new Point(3, 100);
            lblNamespace.Name = "lblNamespace";
            lblNamespace.Size = new Size(244, 50);
            lblNamespace.TabIndex = 4;
            lblNamespace.Text = "Namespace";
            lblNamespace.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtNamespace
            // 
            txtNamespace.Dock = DockStyle.Fill;
            txtNamespace.Location = new Point(253, 103);
            txtNamespace.Multiline = true;
            txtNamespace.Name = "txtNamespace";
            txtNamespace.Size = new Size(980, 44);
            txtNamespace.TabIndex = 5;
            txtNamespace.Text = "RJO.OrderService.Persistence.Database";
            // 
            // frmCentralForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1235, 804);
            Controls.Add(txtLog);
            Controls.Add(pnlBottom);
            Controls.Add(pnlTop);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            Name = "frmCentralForm";
            Text = "EDMX Migration Tool";
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel pnlTop;
        private Panel pnlBottom;
        private Label lblFile;
        private Panel panel1;
        private Panel panel2;
        private Label lblDestination;
        private Button btnRun;
        private OpenFileDialog fileDialog;
        private Button bntFileSelection;
        private TextBox txtFile;
        private Button btnFindDestination;
        private TextBox txtDestination;
        private TextBox txtLog;
        private System.ComponentModel.BackgroundWorker bkWorker;
        private Label lblNamespace;
        private TextBox txtNamespace;
    }
}
