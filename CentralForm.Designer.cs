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
            txtDbName = new TextBox();
            label2 = new Label();
            label1 = new Label();
            lblFile = new Label();
            panel1 = new Panel();
            txtFile = new TextBox();
            bntFileSelection = new Button();
            panel2 = new Panel();
            txtDestination = new TextBox();
            btnFindDestination = new Button();
            lblDestination = new Label();
            lblNamespace = new Label();
            txtNamespace = new TextBox();
            panel3 = new Panel();
            chkCreateConfigurations = new CheckBox();
            chkCreateModels = new CheckBox();
            chkCreateRepositories = new CheckBox();
            chkCreateDBContext = new CheckBox();
            pnlBottom = new Panel();
            btnRun = new Button();
            txtLog = new TextBox();
            bkWorker = new System.ComponentModel.BackgroundWorker();
            pnlTop.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.ColumnCount = 2;
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            pnlTop.ColumnStyles.Add(new ColumnStyle());
            pnlTop.Controls.Add(txtDbName, 1, 4);
            pnlTop.Controls.Add(label2, 0, 4);
            pnlTop.Controls.Add(label1, 0, 3);
            pnlTop.Controls.Add(lblFile, 0, 0);
            pnlTop.Controls.Add(panel1, 1, 0);
            pnlTop.Controls.Add(panel2, 1, 1);
            pnlTop.Controls.Add(lblDestination, 0, 1);
            pnlTop.Controls.Add(lblNamespace, 0, 2);
            pnlTop.Controls.Add(txtNamespace, 1, 2);
            pnlTop.Controls.Add(panel3, 1, 3);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Name = "pnlTop";
            pnlTop.RowCount = 5;
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            pnlTop.Size = new Size(988, 170);
            pnlTop.TabIndex = 0;
            // 
            // txtDbName
            // 
            txtDbName.BackColor = SystemColors.Window;
            txtDbName.Dock = DockStyle.Fill;
            txtDbName.Location = new Point(203, 139);
            txtDbName.Multiline = true;
            txtDbName.Name = "txtDbName";
            txtDbName.Size = new Size(783, 28);
            txtDbName.TabIndex = 10;
            txtDbName.Text = "TMSBD";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(2, 136);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(196, 34);
            label2.TabIndex = 9;
            label2.Text = "Db Context Name";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(2, 102);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(196, 34);
            label1.TabIndex = 7;
            label1.Text = "Options";
            label1.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblFile
            // 
            lblFile.AutoSize = true;
            lblFile.Dock = DockStyle.Fill;
            lblFile.Location = new Point(3, 0);
            lblFile.Name = "lblFile";
            lblFile.Size = new Size(194, 34);
            lblFile.TabIndex = 0;
            lblFile.Text = "EDMX File";
            lblFile.TextAlign = ContentAlignment.MiddleRight;
            // 
            // panel1
            // 
            panel1.Controls.Add(txtFile);
            panel1.Controls.Add(bntFileSelection);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(203, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(783, 28);
            panel1.TabIndex = 1;
            // 
            // txtFile
            // 
            txtFile.BackColor = SystemColors.Window;
            txtFile.Dock = DockStyle.Fill;
            txtFile.Location = new Point(0, 0);
            txtFile.Multiline = true;
            txtFile.Name = "txtFile";
            txtFile.Size = new Size(689, 28);
            txtFile.TabIndex = 0;
            txtFile.Text = "C:\\EDMX\\TMSBD.edmx";
            // 
            // bntFileSelection
            // 
            bntFileSelection.Dock = DockStyle.Right;
            bntFileSelection.Location = new Point(689, 0);
            bntFileSelection.Name = "bntFileSelection";
            bntFileSelection.Size = new Size(94, 28);
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
            panel2.Location = new Point(203, 37);
            panel2.Name = "panel2";
            panel2.Size = new Size(783, 28);
            panel2.TabIndex = 2;
            // 
            // txtDestination
            // 
            txtDestination.BackColor = SystemColors.Window;
            txtDestination.Dock = DockStyle.Fill;
            txtDestination.Location = new Point(0, 0);
            txtDestination.Multiline = true;
            txtDestination.Name = "txtDestination";
            txtDestination.Size = new Size(689, 28);
            txtDestination.TabIndex = 0;
            txtDestination.Text = "C:\\EDMX\\Result";
            // 
            // btnFindDestination
            // 
            btnFindDestination.Dock = DockStyle.Right;
            btnFindDestination.Location = new Point(689, 0);
            btnFindDestination.Name = "btnFindDestination";
            btnFindDestination.Size = new Size(94, 28);
            btnFindDestination.TabIndex = 1;
            btnFindDestination.Text = "...";
            btnFindDestination.UseVisualStyleBackColor = true;
            btnFindDestination.Click += btnFindDestination_Click;
            // 
            // lblDestination
            // 
            lblDestination.AutoSize = true;
            lblDestination.Dock = DockStyle.Fill;
            lblDestination.Location = new Point(3, 34);
            lblDestination.Name = "lblDestination";
            lblDestination.Size = new Size(194, 34);
            lblDestination.TabIndex = 3;
            lblDestination.Text = "Destination Folder";
            lblDestination.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblNamespace
            // 
            lblNamespace.AutoSize = true;
            lblNamespace.Dock = DockStyle.Fill;
            lblNamespace.Location = new Point(2, 68);
            lblNamespace.Margin = new Padding(2, 0, 2, 0);
            lblNamespace.Name = "lblNamespace";
            lblNamespace.Size = new Size(196, 34);
            lblNamespace.TabIndex = 4;
            lblNamespace.Text = "Namespace";
            lblNamespace.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtNamespace
            // 
            txtNamespace.Dock = DockStyle.Fill;
            txtNamespace.Location = new Point(202, 70);
            txtNamespace.Margin = new Padding(2);
            txtNamespace.Multiline = true;
            txtNamespace.Name = "txtNamespace";
            txtNamespace.Size = new Size(785, 30);
            txtNamespace.TabIndex = 5;
            txtNamespace.Text = "TMS.Solution.Core.Data";
            // 
            // panel3
            // 
            panel3.Controls.Add(chkCreateConfigurations);
            panel3.Controls.Add(chkCreateModels);
            panel3.Controls.Add(chkCreateRepositories);
            panel3.Controls.Add(chkCreateDBContext);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(202, 104);
            panel3.Margin = new Padding(2);
            panel3.Name = "panel3";
            panel3.Size = new Size(785, 30);
            panel3.TabIndex = 8;
            // 
            // chkCreateConfigurations
            // 
            chkCreateConfigurations.AutoSize = true;
            chkCreateConfigurations.Checked = true;
            chkCreateConfigurations.CheckState = CheckState.Checked;
            chkCreateConfigurations.Location = new Point(306, 1);
            chkCreateConfigurations.Margin = new Padding(2);
            chkCreateConfigurations.Name = "chkCreateConfigurations";
            chkCreateConfigurations.Size = new Size(128, 24);
            chkCreateConfigurations.TabIndex = 3;
            chkCreateConfigurations.Text = "Configurations";
            chkCreateConfigurations.UseVisualStyleBackColor = true;
            // 
            // chkCreateModels
            // 
            chkCreateModels.AutoSize = true;
            chkCreateModels.Checked = true;
            chkCreateModels.CheckState = CheckState.Checked;
            chkCreateModels.Location = new Point(222, 1);
            chkCreateModels.Margin = new Padding(2);
            chkCreateModels.Name = "chkCreateModels";
            chkCreateModels.Size = new Size(80, 24);
            chkCreateModels.TabIndex = 2;
            chkCreateModels.Text = "Models";
            chkCreateModels.UseVisualStyleBackColor = true;
            // 
            // chkCreateRepositories
            // 
            chkCreateRepositories.AutoSize = true;
            chkCreateRepositories.Checked = true;
            chkCreateRepositories.CheckState = CheckState.Checked;
            chkCreateRepositories.Location = new Point(106, 1);
            chkCreateRepositories.Margin = new Padding(2);
            chkCreateRepositories.Name = "chkCreateRepositories";
            chkCreateRepositories.Size = new Size(113, 24);
            chkCreateRepositories.TabIndex = 1;
            chkCreateRepositories.Text = "Repositories";
            chkCreateRepositories.UseVisualStyleBackColor = true;
            // 
            // chkCreateDBContext
            // 
            chkCreateDBContext.AutoSize = true;
            chkCreateDBContext.Checked = true;
            chkCreateDBContext.CheckState = CheckState.Checked;
            chkCreateDBContext.Location = new Point(2, 1);
            chkCreateDBContext.Margin = new Padding(2);
            chkCreateDBContext.Name = "chkCreateDBContext";
            chkCreateDBContext.Size = new Size(100, 24);
            chkCreateDBContext.TabIndex = 0;
            chkCreateDBContext.Text = "dbContext";
            chkCreateDBContext.UseVisualStyleBackColor = true;
            // 
            // pnlBottom
            // 
            pnlBottom.Controls.Add(btnRun);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 590);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new Size(988, 53);
            pnlBottom.TabIndex = 1;
            // 
            // btnRun
            // 
            btnRun.Dock = DockStyle.Right;
            btnRun.Location = new Point(726, 0);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(262, 53);
            btnRun.TabIndex = 0;
            btnRun.Text = "Convert";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Enabled = false;
            txtLog.Location = new Point(0, 170);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(988, 420);
            txtLog.TabIndex = 2;
            // 
            // bkWorker
            // 
            bkWorker.DoWork += bkWorker_DoWork;
            bkWorker.RunWorkerCompleted += bkWorker_RunWorkerCompleted;
            // 
            // frmCentralForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(988, 643);
            Controls.Add(txtLog);
            Controls.Add(pnlBottom);
            Controls.Add(pnlTop);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmCentralForm";
            Text = "EDMX Migration Tool";
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
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
        private Label label1;
        private Panel panel3;
        private CheckBox chkCreateModels;
        private CheckBox chkCreateRepositories;
        private CheckBox chkCreateDBContext;
        private CheckBox chkCreateConfigurations;
        private Label label2;
        private TextBox txtDbName;
    }
}
