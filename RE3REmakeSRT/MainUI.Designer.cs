namespace RE3REmakeSRT
{
    partial class MainUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.playerHealthStatus = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.EnableEnemy = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableStats = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableKills = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableCollectables = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableInventory = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableRankDifficulty = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableDARankPoints = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableDeathCounter = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableTimer = new System.Windows.Forms.ToolStripMenuItem();
            this.EnableMilliseconds = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.EnableMapID = new System.Windows.Forms.ToolStripMenuItem();
            this.inventoryPanel = new DoubleBuffered.DoubleBufferedPanel();
            this.statisticsPanel = new DoubleBuffered.DoubleBufferedPanel();
            ((System.ComponentModel.ISupportInitialize)(this.playerHealthStatus)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // playerHealthStatus
            // 
            this.playerHealthStatus.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.playerHealthStatus.ContextMenuStrip = this.contextMenuStrip1;
            this.playerHealthStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerHealthStatus.Image = global::RE3REmakeSRT.Properties.Resources.EMPTY;
            this.playerHealthStatus.InitialImage = global::RE3REmakeSRT.Properties.Resources.EMPTY;
            this.playerHealthStatus.Location = new System.Drawing.Point(12, 3);
            this.playerHealthStatus.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            this.playerHealthStatus.Name = "playerHealthStatus";
            this.playerHealthStatus.Size = new System.Drawing.Size(325, 161);
            this.playerHealthStatus.TabIndex = 0;
            this.playerHealthStatus.TabStop = false;
            this.playerHealthStatus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.playerHealthStatus_MouseDown);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EnableTimer,
            this.EnableRankDifficulty,
            this.EnableDARankPoints,
            this.EnableDeathCounter,
            this.EnableMapID,
            this.EnableEnemy,
            this.EnableStats,
            this.EnableInventory});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(217, 202);
            // 
            // EnableEnemy
            // 
            this.EnableEnemy.Name = "EnableEnemy";
            this.EnableEnemy.Size = new System.Drawing.Size(216, 22);
            this.EnableEnemy.Text = "Enable All Enemy HP";
            this.EnableEnemy.Click += new System.EventHandler(this.EnableEnemy_Click);
            // 
            // EnableStats
            // 
            this.EnableStats.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EnableKills,
            this.EnableCollectables});
            this.EnableStats.Name = "EnableStats";
            this.EnableStats.Size = new System.Drawing.Size(216, 22);
            this.EnableStats.Text = "Enable Stats";
            this.EnableStats.Click += new System.EventHandler(this.EnableStats_Click);
            // 
            // EnableKills
            // 
            this.EnableKills.Name = "EnableKills";
            this.EnableKills.Size = new System.Drawing.Size(193, 22);
            this.EnableKills.Text = "Show Kill Stats";
            this.EnableKills.Click += new System.EventHandler(this.EnableKills_Click);
            // 
            // EnableCollectables
            // 
            this.EnableCollectables.Name = "EnableCollectables";
            this.EnableCollectables.Size = new System.Drawing.Size(193, 22);
            this.EnableCollectables.Text = "Show Collectable Stats";
            this.EnableCollectables.Click += new System.EventHandler(this.EnableCollectables_Click);
            // 
            // EnableInventory
            // 
            this.EnableInventory.Checked = true;
            this.EnableInventory.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableInventory.Name = "EnableInventory";
            this.EnableInventory.Size = new System.Drawing.Size(216, 22);
            this.EnableInventory.Text = "Enable Inventory";
            this.EnableInventory.Click += new System.EventHandler(this.EnableInventory_Click);
            // 
            // EnableRankDifficulty
            // 
            this.EnableRankDifficulty.Checked = true;
            this.EnableRankDifficulty.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableRankDifficulty.Name = "EnableRankDifficulty";
            this.EnableRankDifficulty.Size = new System.Drawing.Size(216, 22);
            this.EnableRankDifficulty.Text = "Enable Rank and Difficulty";
            this.EnableRankDifficulty.Click += new System.EventHandler(this.EnableRankDifficulty_Click);
            // 
            // EnableDARankPoints
            // 
            this.EnableDARankPoints.Checked = true;
            this.EnableDARankPoints.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableDARankPoints.Name = "EnableDARankPoints";
            this.EnableDARankPoints.Size = new System.Drawing.Size(216, 22);
            this.EnableDARankPoints.Text = "Enable DA Rank and Points";
            this.EnableDARankPoints.Click += new System.EventHandler(this.EnableDARankPoints_Click);
            // 
            // EnableDeathCounter
            // 
            this.EnableDeathCounter.Checked = true;
            this.EnableDeathCounter.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableDeathCounter.Name = "EnableDeathCounter";
            this.EnableDeathCounter.Size = new System.Drawing.Size(216, 22);
            this.EnableDeathCounter.Text = "Enable Death Counter";
            this.EnableDeathCounter.Click += new System.EventHandler(this.EnableDeathCounter_Click);
            // 
            // EnableTimer
            // 
            this.EnableTimer.Checked = true;
            this.EnableTimer.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableTimer.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EnableMilliseconds});
            this.EnableTimer.Name = "EnableTimer";
            this.EnableTimer.Size = new System.Drawing.Size(216, 22);
            this.EnableTimer.Text = "Enable Timer";
            this.EnableTimer.Click += new System.EventHandler(this.EnableTimer_Click);
            // 
            // EnableMilliseconds
            // 
            this.EnableMilliseconds.Checked = true;
            this.EnableMilliseconds.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableMilliseconds.Name = "EnableMilliseconds";
            this.EnableMilliseconds.Size = new System.Drawing.Size(180, 22);
            this.EnableMilliseconds.Text = "Enable Milliseconds";
            this.EnableMilliseconds.Click += new System.EventHandler(this.EnableMilliseconds_Click);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.inventoryPanel, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.statisticsPanel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.playerHealthStatus, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 167F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 420F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(340, 732);
            this.tableLayoutPanel2.TabIndex = 4;
            // 
            // EnableMapID
            // 
            this.EnableMapID.Name = "EnableMapID";
            this.EnableMapID.Size = new System.Drawing.Size(216, 22);
            this.EnableMapID.Text = "EnableMapID";
            this.EnableMapID.Click += new System.EventHandler(this.EnableMapID_Click);
            // 
            // inventoryPanel
            // 
            this.inventoryPanel.BackColor = System.Drawing.Color.Blue;
            this.inventoryPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inventoryPanel.Location = new System.Drawing.Point(3, 315);
            this.inventoryPanel.Name = "inventoryPanel";
            this.inventoryPanel.Size = new System.Drawing.Size(334, 414);
            this.inventoryPanel.TabIndex = 3;
            this.inventoryPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.inventoryPanel_MouseDown);
            // 
            // statisticsPanel
            // 
            this.statisticsPanel.ContextMenuStrip = this.contextMenuStrip1;
            this.statisticsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statisticsPanel.Location = new System.Drawing.Point(3, 170);
            this.statisticsPanel.Name = "statisticsPanel";
            this.statisticsPanel.Size = new System.Drawing.Size(334, 139);
            this.statisticsPanel.TabIndex = 2;
            this.statisticsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.statisticsPanel_MouseDown);
            // 
            // MainUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Blue;
            this.ClientSize = new System.Drawing.Size(340, 732);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.tableLayoutPanel2);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(360, 1080);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(360, 630);
            this.Name = "MainUI";
            this.ShowIcon = false;
            this.Text = "RE3 (2020) SRT";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainUI_FormClosed);
            this.Load += new System.EventHandler(this.MainUI_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainUI_MouseDown);
            ((System.ComponentModel.ISupportInitialize)(this.playerHealthStatus)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox playerHealthStatus;
        private DoubleBuffered.DoubleBufferedPanel statisticsPanel;
        private DoubleBuffered.DoubleBufferedPanel inventoryPanel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem EnableEnemy;
        private System.Windows.Forms.ToolStripMenuItem EnableStats;
        private System.Windows.Forms.ToolStripMenuItem EnableKills;
        private System.Windows.Forms.ToolStripMenuItem EnableCollectables;
        private System.Windows.Forms.ToolStripMenuItem EnableInventory;
        private System.Windows.Forms.ToolStripMenuItem EnableRankDifficulty;
        private System.Windows.Forms.ToolStripMenuItem EnableDARankPoints;
        private System.Windows.Forms.ToolStripMenuItem EnableDeathCounter;
        private System.Windows.Forms.ToolStripMenuItem EnableTimer;
        private System.Windows.Forms.ToolStripMenuItem EnableMilliseconds;
        private System.Windows.Forms.ToolStripMenuItem EnableMapID;
    }
}