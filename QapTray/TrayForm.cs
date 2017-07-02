﻿using NLog;
using Qap;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace QapTray
{
    public partial class TrayForm : Form
    {
        const string FullScreenPrefix = "FullScreen";
        const string ActiveWindowPrefix = "ActiveWindow";
        private const int ModeIndex = 0;
        private const int FileStatusIndex = 1;

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private QapSettings _qapSettings;
        private readonly AudioRecorder _audioRecorder = new AudioRecorder();

        public TrayForm()
        {
            InitializeComponent();
        }

        private void TrayForm_Resize(object sender, System.EventArgs e)
        {
            if (!toTrayCheckBox.Checked) return;
            if (FormWindowState.Minimized == WindowState)
            {
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(5);
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            notifyIcon.Visible = false;
            Show();
            WindowState = FormWindowState.Normal;

        }

        private void TrayForm_Load(object sender, System.EventArgs e)
        {
            _qapSettings = QapSettings.Load();
            toTrayCheckBox.Checked = _qapSettings.MinimizeToTray;
            startMinimizedCheckBox.Checked = _qapSettings.StartMinimized;
            capturePeriodInSeconds.Value = _qapSettings.CapturePeriod;
            captureActiveWindowCheckBox.Checked = _qapSettings.CaptureActiveWindow;
            UpdateModeStatus(captureActiveWindowCheckBox.Checked);
            UpdateFileStatus();
            if (startMinimizedCheckBox.Checked)
                WindowState = FormWindowState.Minimized;
        }

        private void TrayForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _qapSettings.MinimizeToTray = toTrayCheckBox.Checked;
            _qapSettings.StartMinimized = startMinimizedCheckBox.Checked;
            _qapSettings.CapturePeriod = (int)capturePeriodInSeconds.Value;
            _qapSettings.CaptureActiveWindow = captureActiveWindowCheckBox.Checked;
            QapSettings.Save(_qapSettings);
        }

        private void captureButton_Click(object sender, System.EventArgs e)
        {
            if (captureActiveWindowCheckBox.Checked)
            {
                CaptureActiveWindow();
            }
            else
            {
                CaptureFullScreen();
            }
        }

        private void CaptureFullScreen()
        {
            Image image = ScreenCapture.CaptureDesktop();
            if (image != null)
            {
                var captureFileName = GetCaptureFileName(true);
                _logger.Debug($"Saving full screen into: {captureFileName}");
                UpdateFileStatus(captureFileName);
                image.Save(captureFileName, ImageFormat.Png);
            }
        }

        private string GetCaptureFileName(string filePrefix, int cntWindow)
        {
            return $"{filePrefix}{cntWindow:0000}.png";
        }

        private string GetCaptureFileName(bool fullScreen)
        {
            string filePrefix = fullScreen ? FullScreenPrefix : ActiveWindowPrefix;
            var counter = fullScreen ? _qapSettings.IncrementFullScreenSaveCounter() : _qapSettings.IncrementWindowSaveCounter();
            return GetCaptureFileName(filePrefix, counter);
        }

        private void CaptureActiveWindow()
        {
            Bitmap bmp = ScreenCapture.CaptureActiveWindow();
            if (bmp != null)
            {
                var captureFileName = GetCaptureFileName(false);
                _logger.Debug($"Saving active window into: {captureFileName}");
                UpdateFileStatus(captureFileName);
                bmp.Save(captureFileName, ImageFormat.Png);
            }
        }


        private void captureTimer_Tick(object sender, System.EventArgs e)
        {
            if (captureActiveWindowCheckBox.Checked)
                CaptureActiveWindow();
            else
                CaptureFullScreen();
        }

        private void autoRecordButton_Click(object sender, System.EventArgs e)
        {
            if (!captureTimer.Enabled)
            {
                captureTimer.Interval = (int)capturePeriodInSeconds.Value * 1000;
                autoRecordButton.Text = "Stop AutoRecord";
                autoRecordButton.ForeColor = Color.Blue;
            }
            else
            {
                autoRecordButton.Text = "Start AutoRecord";
                autoRecordButton.ForeColor = Color.Red;
            }
            captureTimer.Enabled = !captureTimer.Enabled;
        }

        private void captureActiveWindowCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            UpdateModeStatus(captureActiveWindowCheckBox.Checked);
        }


        private void UpdateModeStatus(bool activeWindow)
        {
            var mode = activeWindow ? ActiveWindowPrefix : FullScreenPrefix;
            statusStrip.Items[ModeIndex].Text = $@"Mode: {mode} | ";
        }

        private void UpdateFileStatus(string captureFileName = "")
        {
            statusStrip.Items[FileStatusIndex].Text = $"File: {captureFileName}";
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            _audioRecorder.Start();
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            _audioRecorder.Stop();
        }

    }
}