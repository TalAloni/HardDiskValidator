namespace HardDiskValidator
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBoxMap = new System.Windows.Forms.PictureBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.comboDisks = new System.Windows.Forms.ComboBox();
            this.chkRead = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkWriteVerify = new System.Windows.Forms.RadioButton();
            this.chkReadWriteVerifyRestore = new System.Windows.Forms.RadioButton();
            this.chkReadRewriteVerify = new System.Windows.Forms.RadioButton();
            this.lblSerialNumber = new System.Windows.Forms.Label();
            this.btnCopyLog = new System.Windows.Forms.Button();
            this.lblPosition = new System.Windows.Forms.Label();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.groupProgress = new System.Windows.Forms.GroupBox();
            this.pictureBoxLegend = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMap)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupProgress.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLegend)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBoxMap
            // 
            this.pictureBoxMap.Location = new System.Drawing.Point(8, 35);
            this.pictureBoxMap.Name = "pictureBoxMap";
            this.pictureBoxMap.Size = new System.Drawing.Size(351, 301);
            this.pictureBoxMap.TabIndex = 0;
            this.pictureBoxMap.TabStop = false;
            this.pictureBoxMap.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxMap_Paint);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(370, 159);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 6;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // comboDisks
            // 
            this.comboDisks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDisks.Enabled = false;
            this.comboDisks.FormattingEnabled = true;
            this.comboDisks.Location = new System.Drawing.Point(8, 8);
            this.comboDisks.Name = "comboDisks";
            this.comboDisks.Size = new System.Drawing.Size(351, 21);
            this.comboDisks.TabIndex = 2;
            this.comboDisks.SelectedIndexChanged += new System.EventHandler(this.comboDisks_SelectedIndexChanged);
            // 
            // chkRead
            // 
            this.chkRead.AutoSize = true;
            this.chkRead.Checked = true;
            this.chkRead.Location = new System.Drawing.Point(6, 20);
            this.chkRead.Name = "chkRead";
            this.chkRead.Size = new System.Drawing.Size(51, 17);
            this.chkRead.TabIndex = 3;
            this.chkRead.TabStop = true;
            this.chkRead.Text = "Read";
            this.chkRead.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkWriteVerify);
            this.groupBox1.Controls.Add(this.chkReadWriteVerifyRestore);
            this.groupBox1.Controls.Add(this.chkReadRewriteVerify);
            this.groupBox1.Controls.Add(this.chkRead);
            this.groupBox1.Location = new System.Drawing.Point(370, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(185, 122);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Select Test:";
            // 
            // chkWriteVerify
            // 
            this.chkWriteVerify.AutoSize = true;
            this.chkWriteVerify.Location = new System.Drawing.Point(6, 92);
            this.chkWriteVerify.Name = "chkWriteVerify";
            this.chkWriteVerify.Size = new System.Drawing.Size(88, 17);
            this.chkWriteVerify.TabIndex = 7;
            this.chkWriteVerify.TabStop = true;
            this.chkWriteVerify.Text = "Write + Verify";
            this.chkWriteVerify.UseVisualStyleBackColor = true;
            // 
            // chkReadWriteVerifyRestore
            // 
            this.chkReadWriteVerifyRestore.AutoSize = true;
            this.chkReadWriteVerifyRestore.Location = new System.Drawing.Point(6, 68);
            this.chkReadWriteVerifyRestore.Name = "chkReadWriteVerifyRestore";
            this.chkReadWriteVerifyRestore.Size = new System.Drawing.Size(175, 17);
            this.chkReadWriteVerifyRestore.TabIndex = 5;
            this.chkReadWriteVerifyRestore.TabStop = true;
            this.chkReadWriteVerifyRestore.Text = "Read + Write + Verify + Restore";
            this.chkReadWriteVerifyRestore.UseVisualStyleBackColor = true;
            // 
            // chkReadRewriteVerify
            // 
            this.chkReadRewriteVerify.AutoSize = true;
            this.chkReadRewriteVerify.Location = new System.Drawing.Point(6, 44);
            this.chkReadRewriteVerify.Name = "chkReadRewriteVerify";
            this.chkReadRewriteVerify.Size = new System.Drawing.Size(175, 17);
            this.chkReadRewriteVerify.TabIndex = 4;
            this.chkReadRewriteVerify.TabStop = true;
            this.chkReadRewriteVerify.Text = "Read + Wipe Damaged + Read";
            this.chkReadRewriteVerify.UseVisualStyleBackColor = true;
            // 
            // lblSerialNumber
            // 
            this.lblSerialNumber.AutoSize = true;
            this.lblSerialNumber.Location = new System.Drawing.Point(367, 11);
            this.lblSerialNumber.Name = "lblSerialNumber";
            this.lblSerialNumber.Size = new System.Drawing.Size(30, 13);
            this.lblSerialNumber.TabIndex = 5;
            this.lblSerialNumber.Text = "S/N:";
            // 
            // btnCopyLog
            // 
            this.btnCopyLog.Location = new System.Drawing.Point(488, 158);
            this.btnCopyLog.Name = "btnCopyLog";
            this.btnCopyLog.Size = new System.Drawing.Size(67, 24);
            this.btnCopyLog.TabIndex = 10;
            this.btnCopyLog.Text = "Copy Log";
            this.btnCopyLog.UseVisualStyleBackColor = true;
            this.btnCopyLog.Click += new System.EventHandler(this.btnCopyLog_Click);
            // 
            // lblPosition
            // 
            this.lblPosition.AutoSize = true;
            this.lblPosition.Location = new System.Drawing.Point(3, 38);
            this.lblPosition.Name = "lblPosition";
            this.lblPosition.Size = new System.Drawing.Size(47, 13);
            this.lblPosition.TabIndex = 7;
            this.lblPosition.Text = "Position:";
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(3, 16);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(41, 13);
            this.lblSpeed.TabIndex = 8;
            this.lblSpeed.Text = "Speed:";
            // 
            // groupProgress
            // 
            this.groupProgress.Controls.Add(this.lblSpeed);
            this.groupProgress.Controls.Add(this.lblPosition);
            this.groupProgress.Location = new System.Drawing.Point(370, 183);
            this.groupProgress.Name = "groupProgress";
            this.groupProgress.Size = new System.Drawing.Size(185, 64);
            this.groupProgress.TabIndex = 9;
            this.groupProgress.TabStop = false;
            // 
            // pictureBoxLegend
            // 
            this.pictureBoxLegend.Location = new System.Drawing.Point(6, 13);
            this.pictureBoxLegend.Name = "pictureBoxLegend";
            this.pictureBoxLegend.Size = new System.Drawing.Size(172, 66);
            this.pictureBoxLegend.TabIndex = 10;
            this.pictureBoxLegend.TabStop = false;
            this.pictureBoxLegend.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxLegend_Paint);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pictureBoxLegend);
            this.groupBox2.Location = new System.Drawing.Point(370, 251);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(184, 86);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Legend";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(562, 344);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnCopyLog);
            this.Controls.Add(this.groupProgress);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.lblSerialNumber);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.comboDisks);
            this.Controls.Add(this.pictureBoxMap);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(570, 371);
            this.MinimumSize = new System.Drawing.Size(570, 371);
            this.Name = "MainForm";
            this.Text = "Hard Disk Validator";
            this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMap)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupProgress.ResumeLayout(false);
            this.groupProgress.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLegend)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxMap;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.ComboBox comboDisks;
        private System.Windows.Forms.RadioButton chkRead;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton chkReadRewriteVerify;
        private System.Windows.Forms.Label lblSerialNumber;
        private System.Windows.Forms.Button btnCopyLog;
        private System.Windows.Forms.Label lblPosition;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.GroupBox groupProgress;
        private System.Windows.Forms.RadioButton chkReadWriteVerifyRestore;
        private System.Windows.Forms.PictureBox pictureBoxLegend;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton chkWriteVerify;
    }
}

