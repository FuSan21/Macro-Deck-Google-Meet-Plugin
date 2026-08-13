using SuchByte.MacroDeck.GUI.CustomControls;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FuSan21.MacroDeck.GoogleMeet.UI
{
    public class ConfigurationForm : DialogForm
    {
        /// <summary>
        /// Wrap width for the wordy labels. Everything else sizes to content, so this is
        /// effectively what sets the dialog's width.
        /// </summary>
        private const int TextWidth = 460;

        private readonly Configuration _config;
        private readonly FlowLayoutPanel _layout;

        private readonly Label _status = new Label { AutoSize = true };
        private readonly Label _error = new Label { AutoSize = true, MaximumSize = new Size(TextWidth, 0), ForeColor = Color.OrangeRed };
        private readonly CheckBox _enabled = new CheckBox { Text = "Enable Google Meet integration", AutoSize = true };
        private readonly NumericUpDown _port = new NumericUpDown { Minimum = 1024, Maximum = 65535, Width = 100 };

        public ConfigurationForm(Configuration config)
        {
            _config = config;

            Text = "Google Meet Plugin";
            StartPosition = FormStartPosition.CenterParent;

            var layout = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(16),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            _layout = layout;

            layout.Controls.Add(_status);
            layout.Controls.Add(_error);
            layout.Controls.Add(Spacer());

            layout.Controls.Add(_enabled);
            layout.Controls.Add(Spacer());

            var portRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            portRow.Controls.Add(new Label { Text = "Port", AutoSize = true, Margin = new Padding(0, 6, 8, 0) });
            portRow.Controls.Add(_port);
            layout.Controls.Add(portRow);
            layout.Controls.Add(Hint(
                "The browser extension dials this port. Only change it if something else on this " +
                "machine already owns it — and change it in the extension to match."));
            layout.Controls.Add(Spacer());

            layout.Controls.Add(Hint(
                "Google Meet runs in a browser, so this plugin needs a browser extension to reach it. " +
                "Open the folder below, then load it in Chrome via chrome://extensions → Developer mode → " +
                "Load unpacked."));

            var openFolder = new ButtonPrimary { Text = "Open extension folder", AutoSize = true, Margin = new Padding(20, 4, 0, 0) };
            openFolder.Click += OpenFolder_Click;
            layout.Controls.Add(openFolder);
            layout.Controls.Add(Spacer());

            layout.Controls.Add(Hint(
                "ChrisRegado's published Stream Deck extension works too, minus the presenting and " +
                "in-call actions. It cannot run at the same time as his Stream Deck plugin — both want this port."));

            var save = new ButtonPrimary { Text = "Save", AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
            save.Click += Save_Click;
            layout.Controls.Add(save);

            Controls.Add(layout);

            _enabled.Checked = _config?.Enabled ?? true;
            _port.Value = _config?.Port ?? Configuration.DefaultPort;
            _enabled.CheckedChanged += (s, e) => UpdateStatus();

            MeetHelper.AvailabilityChanged += MeetHelper_AvailabilityChanged;
            UpdateStatus();
            FitToContent();
        }

        /// <summary>Matches the window to whatever the layout actually needs.</summary>
        private void FitToContent()
        {
            if (IsDisposed || _layout == null)
            {
                return;
            }

            var preferred = _layout.PreferredSize;
            ClientSize = new Size(preferred.Width + Padding.Horizontal, preferred.Height + Padding.Vertical);
        }

        private static Control Spacer() => new Label { Height = 8, Width = 1 };

        private static Label Hint(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(TextWidth - 20, 0),
            ForeColor = Color.Gray,
            Margin = new Padding(20, 0, 0, 4)
        };

        /// <summary>
        /// The extension is copied next to the plugin DLL at build time, so the folder to
        /// load unpacked is always the one belonging to the installed version.
        /// </summary>
        private void OpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                var pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var extension = Path.Combine(pluginDirectory ?? string.Empty, "extension");

                if (!Directory.Exists(extension))
                {
                    PluginInstance.Logger.Warning("The extension folder is missing from {0}", pluginDirectory);
                    extension = pluginDirectory;
                }

                Process.Start(new ProcessStartInfo(extension) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Could not open the extension folder:\n{0}", ex);
            }
        }

        private void MeetHelper_AvailabilityChanged(object sender, EventArgs e) => UpdateStatus();

        private void UpdateStatus()
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                var enabled = _enabled.Checked;
                _port.Enabled = enabled;

                if (!enabled)
                {
                    _status.Text = "Status: disabled";
                }
                else if (!MeetHelper.IsRunning)
                {
                    _status.Text = "Status: not listening";
                }
                else if (!MeetHelper.IsAvailable)
                {
                    _status.Text = "Status: listening — no Meet tab has connected yet";
                }
                else
                {
                    var state = MeetHelper.State;
                    _status.Text = state.InCallKnown && state.InMeeting
                        ? $"Status: in a call ({MeetHelper.ConnectedTabs} tab(s) connected)"
                        : $"Status: connected ({MeetHelper.ConnectedTabs} tab(s))";
                }

                var error = MeetHelper.LastError;
                _error.Visible = enabled && !string.IsNullOrWhiteSpace(error);
                _error.Text = string.IsNullOrWhiteSpace(error) ? string.Empty : $"Last error: {error}";

                FitToContent();
            }));
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (_config == null)
            {
                return;
            }

            var portChanged = _config.Port != (int)_port.Value;

            _config.Enabled = _enabled.Checked;
            _config.Port = (int)_port.Value;
            _config.Save();

            // The listener binds its port once, so a new port only takes effect after a
            // restart of the server — cheapest to always stop first.
            if (portChanged)
            {
                MeetHelper.Stop();
            }

            PluginInstance.Plugin?.ApplyEnabledState();
            UpdateStatus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            MeetHelper.AvailabilityChanged -= MeetHelper_AvailabilityChanged;
            base.OnFormClosed(e);
        }
    }
}
