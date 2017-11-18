/* Copyright (C) 2016 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DiskAccessLibrary;
using Utilities;

namespace HardDiskValidator
{
    public partial class MainForm : Form
    {
        public const int HorizontalBlocks = 50;
        public const int VerticalBlocks = 50;

        private DateTime m_startTime;
        private BlockStatus[] m_blocks = new BlockStatus[HorizontalBlocks * VerticalBlocks];
        private bool m_isBusy = false;
        private DiskTester m_diskTester;
        private bool m_isClosing = false;
        private List<string> m_log = new List<string>();
        
        public MainForm()
        {
            InitializeComponent();
            this.Text += " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            PopulateDiskList(comboDisks);
        }

        private void pictureBoxMap_Paint(object sender, PaintEventArgs e)
        {
            DrawDiskMap(e.Graphics);
        }

        private void pictureBoxLegend_Paint(object sender, PaintEventArgs e)
        {
            DrawLegend(e.Graphics);
        }

        private void DrawDiskMap(Graphics graphics)
        {
            const int BlockWidth = 6;
            const int BlockHeight = 5;

            Pen pen = new Pen(Color.Black, 1);
            int width = HorizontalBlocks * (BlockWidth + 1);
            int height = VerticalBlocks * (BlockHeight + 1);
            for (int index = 0; index <= HorizontalBlocks; index++)
            {
                int x = (BlockWidth + 1) * index;
                graphics.DrawLine(pen, x, 0, x, height);
            }

            for (int index = 0; index <= VerticalBlocks; index++)
            {
                int y = (BlockHeight + 1) * index;
                graphics.DrawLine(pen, 0, y, width, y);
            }

            for (int index = 0; index < m_blocks.Length; index++)
            {
                int hIndex = index % HorizontalBlocks;
                int vIndex = index / HorizontalBlocks;

                int x = 1 + hIndex * (1 + BlockWidth);
                int y = 1 + vIndex * (1 + BlockHeight);
                SolidBrush brush = new SolidBrush(UIHelper.GetColor(m_blocks[index]));
                graphics.FillRectangle(brush, x, y, BlockWidth, BlockHeight);
            }
        }

        private void DrawLegend(Graphics graphics)
        {
            DrawLegendEntry(graphics, 1, 8, BlockStatus.OK, "OK");
            DrawLegendEntry(graphics, 1, 24, BlockStatus.OverwriteOK, "Overwrite OK");
            DrawLegendEntry(graphics, 1, 40, BlockStatus.Damaged, "Damaged");
            DrawLegendEntry(graphics, 1, 56, BlockStatus.IOError, "IO Error");
        }

        private void DrawLegendEntry(Graphics graphics, float x, float y, BlockStatus status, string text)
        {
            const int BlockWidth = 6;
            const int BlockHeight = 5;

            Pen pen = new Pen(Color.Black, 1);
            SolidBrush brush = new SolidBrush(UIHelper.GetColor(status));
            graphics.DrawRectangle(pen, x, y, BlockWidth + 1, BlockHeight + 1);
            graphics.FillRectangle(brush, x + 1, y + 1, BlockWidth, BlockHeight);
            Font font = new Font(FontFamily.GenericSansSerif, 8);
            graphics.DrawString(text, font, Brushes.Black, x + 12, y - 3);
        }

        private void comboDisks_SelectedIndexChanged(object sender, EventArgs e)
        {
            int physicalDiskIndex = ((KeyValuePair<int, string>)comboDisks.SelectedItem).Key;
            PhysicalDisk disk = new PhysicalDisk(physicalDiskIndex);
            lblSerialNumber.Text = "S/N: " + disk.SerialNumber;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_isBusy)
            {
                e.Cancel = true;
                m_diskTester.Abort = true;
                m_isClosing = true;
            }
        }

        private void btnCopyLog_Click(object sender, EventArgs e)
        {
            if (m_log.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                foreach (string message in m_log)
                {
                    builder.AppendLine(message);
                }
                Clipboard.SetText(builder.ToString());
            }
        }

        private void PopulateDiskList(ComboBox comboBox)
        {
            Thread thread = new Thread(delegate()
            {
                List<PhysicalDisk> disks = PhysicalDiskHelper.GetPhysicalDisks();
                this.Invoke((MethodInvoker)delegate
                {
                    comboBox.Items.Clear();
                    comboBox.DisplayMember = "Value";
                    comboBox.ValueMember = "Key";
                    foreach (PhysicalDisk disk in disks)
                    {
                        string title = String.Format("[{0}] {1} ({2})", disk.PhysicalDiskIndex, disk.Description,  UIHelper.GetSizeString(disk.Size));
                        comboBox.Items.Add(new KeyValuePair<int, string>(disk.PhysicalDiskIndex, title));
                    }
                    comboBox.SelectedIndex = 0;
                    comboBox.Enabled = true;
                });
            });
            thread.Start();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (m_isBusy)
            {
                btnStart.Enabled = false;
                btnStart.Text = "Stopping";
                Thread thread = new Thread(delegate()
                {
                    m_diskTester.Abort = true;
                    while (m_isBusy)
                    {
                        Thread.Sleep(100);
                    }
                    if (!m_isClosing)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            btnStart.Text = "Start";
                            btnStart.Enabled = true;
                            comboDisks.Enabled = true;
                            chkRead.Enabled = true;
                            chkReadRewriteVerify.Enabled = true;
                            chkReadWriteVerifyRestore.Enabled = true;
                            chkWriteVerify.Enabled = true;
                        });
                    }
                });
                thread.Start();
            }
            else
            {
                if (comboDisks.SelectedItem == null)
                {
                    return;
                }
                int physicalDiskIndex = ((KeyValuePair<int, string>)comboDisks.SelectedItem).Key;
                TestName testName = TestName.Read;
                if (chkReadRewriteVerify.Checked)
                {
                    testName = TestName.ReadWipeDamagedRead;
                }
                else if (chkReadWriteVerifyRestore.Checked)
                {
                    testName = TestName.ReadWriteVerifyRestore;
                }
                else if (chkWriteVerify.Checked)
                {
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        testName = TestName.Verify;
                    }
                    else
                    {
                        testName = TestName.WriteVerify;
                        DialogResult dialogResult = MessageBox.Show("This test will erase all existing data on the selected disk, Are you sure?", "Warning", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.No)
                        {
                            return;
                        }
                    }
                }
                btnStart.Text = "Stop";
                comboDisks.Enabled = false;
                chkRead.Enabled = false;
                chkReadRewriteVerify.Enabled = false;
                chkReadWriteVerifyRestore.Enabled = false;
                chkWriteVerify.Enabled = false;
                Thread thread = new Thread(delegate()
                {
                    m_isBusy = true;
                    PerformTest(physicalDiskIndex, testName);
                    m_isBusy = false;
                    if (m_isClosing)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            this.Close();
                        });
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            btnStart.Text = "Start";
                            comboDisks.Enabled = true;
                            chkRead.Enabled = true;
                            chkReadRewriteVerify.Enabled = true;
                            chkReadWriteVerifyRestore.Enabled = true;
                            chkWriteVerify.Enabled = true;
                        });
                    }
                });
                thread.Start();
            }
        }

        private void PerformTest(int physicalDiskIndex, TestName testName)
        {
            PhysicalDisk disk = new PhysicalDisk(physicalDiskIndex);
            m_startTime = DateTime.Now;
            ClearLog();
            AddToLog("Hard Disk Validator {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            AddToLog("Starting {0} Test", GetTestTitle(testName));
            AddToLog("Disk: {0}, S/N: {1}", disk.Description, disk.SerialNumber);
            AddToLog("Disk size: {0} ({1:###,###,###,###,###} sectors, {2} bytes per sector)", UIHelper.GetSizeString(disk.Size), disk.TotalSectors, disk.BytesPerSector);
            if (!(testName == TestName.Read || testName == TestName.Verify))
            {
                bool success = disk.ExclusiveLock();
                if (!success)
                {
                    MessageBox.Show("Failed to lock the disk.");
                    return;
                }

                if (Environment.OSVersion.Version.Major >= 6)
                {
                    success = disk.SetOnlineStatus(false, false);
                    if (!success)
                    {
                        disk.ReleaseLock();
                        MessageBox.Show("Failed to take the disk offline.");
                        return;
                    }
                }
            }

            m_diskTester = new DiskTester(testName, disk);
            m_diskTester.OnStatusUpdate += delegate(long currentPosition)
            {
                UpdateStatus(currentPosition);
            };
            m_diskTester.OnLogUpdate += delegate(string format, object[] args)
            {
                AddToLog(format, args);
            };
            // The last segment might be bigger than the others
            long uiBlockSize = disk.TotalSectors / (HorizontalBlocks * VerticalBlocks);
            m_blocks = new BlockStatus[HorizontalBlocks * VerticalBlocks];
            this.Invoke((MethodInvoker)delegate
            {
                pictureBoxMap.Invalidate();
                pictureBoxMap.Update();
            });
            for (int uiBlockIndex = 0; uiBlockIndex < m_blocks.Length; uiBlockIndex++)
            {
                long sectorIndex = uiBlockIndex * uiBlockSize;
                long sectorCount = uiBlockSize;
                if (uiBlockIndex == m_blocks.Length - 1)
                {
                    sectorCount = disk.TotalSectors - ((m_blocks.Length - 1) * uiBlockSize);
                }
                BlockStatus blockStatus = m_diskTester.PerformTest(sectorIndex, sectorCount);
                m_blocks[uiBlockIndex] = blockStatus;
                this.Invoke((MethodInvoker)delegate
                {
                    pictureBoxMap.Invalidate();
                    pictureBoxMap.Update();
                });
                if (m_diskTester.Abort)
                {
                    break;
                }
            }
            
            if (testName != TestName.Read)
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    disk.SetOnlineStatus(true, false);
                }
                disk.ReleaseLock();
                disk.UpdateProperties();
            }

            if (m_diskTester.Abort)
            {
                AddToLog("Test Aborted");
            }
            else
            {
                AddToLog("Test Completed");
            }
            m_diskTester = null;
        }

        private void UpdateStatus(long currentPosition)
        {
            if (InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus(currentPosition);
                });
            }
            else
            {
                int totalSeconds = (int)((TimeSpan)(DateTime.Now - m_startTime)).TotalSeconds;
                totalSeconds = Math.Max(totalSeconds, 1);
                long speed = currentPosition / totalSeconds;
                lblSpeed.Text = String.Format("Speed: {0}/s", UIHelper.GetSizeString(speed));
                string progressText = String.Format("Position: {0:###,###,###,###,###}", currentPosition);
                lblPosition.Text = progressText;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                chkWriteVerify.Text = GetTestTitle(TestName.Verify);
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            chkWriteVerify.Text = GetTestTitle(TestName.WriteVerify);
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            chkWriteVerify.Text = GetTestTitle(TestName.WriteVerify);
        }

        private void ClearLog()
        {
            m_log = new List<string>();
        }

        private void AddToLog(string format, params object[] args)
        {
            string message = String.Format(format, args);
            AddToLog(message);
        }

        private void AddToLog(string message)
        {
            string messageFormatted = String.Format("{0}: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message);
            m_log.Add(messageFormatted);
        }

        private static string GetTestTitle(TestName testName)
        {
            if (testName == TestName.Read)
            {
                return "Read";
            }
            else if (testName == TestName.ReadWipeDamagedRead)
            {
                return "Read + Wipe Damaged + Read";
            }
            else if (testName == TestName.ReadWriteVerifyRestore)
            {
                return "Read + Write + Verify + Restore";
            }
            else if (testName == TestName.WriteVerify)
            {
                return "Write + Verify";
            }
            else
            {
                return "Verify";
            }
        }
    }
}