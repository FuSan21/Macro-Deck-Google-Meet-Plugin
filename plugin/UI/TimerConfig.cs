using Newtonsoft.Json;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FuSan21.MacroDeck.GoogleMeet.UI
{
    /// <summary>
    /// The Timer action's configuration: which command, and — for Start — how long.
    ///
    /// The duration is a free text field rather than two spinners because "5" and "1:30"
    /// are what people actually type, and a pair of numeric boxes for something usually
    /// set in whole minutes is more clicks for less.
    /// </summary>
    public class TimerConfig : ActionConfigControl
    {
        public class Settings
        {
            public TimerCommand Command { get; set; }

            /// <summary>
            /// As typed. Empty means "leave whatever duration Meet is showing", which is
            /// also what happens for every command other than Start.
            /// </summary>
            public string Duration { get; set; }
        }

        private readonly PluginAction _action;
        private readonly RoundedComboBox _command;
        private readonly RoundedTextBox _duration;
        private readonly Label _durationLabel;
        private readonly Label _hint;

        public TimerConfig(PluginAction action)
        {
            _action = action;
            Dock = DockStyle.Fill;

            _command = new RoundedComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            _command.Items.AddRange(Enum.GetNames(typeof(TimerCommand)));
            _command.SelectedIndex = 0;
            _command.SelectedIndexChanged += (s, e) => UpdateDurationVisibility();

            _durationLabel = new Label { Text = "Duration", AutoSize = true, Margin = new Padding(0, 10, 0, 3) };
            _duration = new RoundedTextBox
            {
                Width = 200,
                PlaceHolderText = "5  or  1:30"
            };

            _hint = new Label
            {
                Text = "Leave empty to use whatever duration Meet is already showing.",
                AutoSize = true,
                MaximumSize = new Size(260, 0),
                ForeColor = Color.Gray,
                Margin = new Padding(0, 2, 0, 0)
            };

            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            layout.Controls.Add(new Label { Text = "Timer action", AutoSize = true });
            layout.Controls.Add(_command);
            layout.Controls.Add(_durationLabel);
            layout.Controls.Add(_duration);
            layout.Controls.Add(_hint);
            Controls.Add(layout);

            var config = LoadConfig(_action.Configuration);
            if (config != null)
            {
                var index = _command.Items.IndexOf(config.Command.ToString());
                if (index >= 0)
                {
                    _command.SelectedIndex = index;
                }

                _duration.Text = config.Duration ?? string.Empty;
            }

            UpdateDurationVisibility();
        }

        /// <summary>The duration only means anything to Start, so it is hidden for the rest.</summary>
        private void UpdateDurationVisibility()
        {
            var isStart = _command.SelectedItem?.ToString() == nameof(TimerCommand.Start);
            _durationLabel.Visible = isStart;
            _duration.Visible = isStart;
            _hint.Visible = isStart;
        }

        public override bool OnActionSave()
        {
            if (!Enum.TryParse<TimerCommand>(_command.SelectedItem?.ToString(), out var command))
            {
                command = TimerCommand.Start;
            }

            var duration = (_duration.Text ?? string.Empty).Trim();
            if (command == TimerCommand.Start && duration.Length > 0 && !TryParseDuration(duration, out _, out _))
            {
                using var error = new SuchByte.MacroDeck.GUI.CustomControls.MessageBox();
                error.ShowDialog(
                    "Google Meet",
                    $"\"{duration}\" is not a duration. Use minutes (5), minutes and seconds (1:30), " +
                    "or leave it empty.",
                    MessageBoxButtons.OK);
                return false;
            }

            _action.Configuration = JsonConvert.SerializeObject(new Settings
            {
                Command = command,
                Duration = duration,
            });

            _action.ConfigurationSummary = command == TimerCommand.Start && duration.Length > 0
                ? $"{command} ({duration})"
                : command.ToString();

            return true;
        }

        public static Settings LoadConfig(string configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<Settings>(configuration);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads "5", "1:30" and ":45" as 5m, 1m30s and 45s. Minutes-only is the common
        /// case, so a bare number is minutes rather than seconds.
        ///
        /// Meet's own fields cap at 1440 minutes and 60 seconds, so anything past that is
        /// rejected here rather than silently clamped by the browser.
        /// </summary>
        public static bool TryParseDuration(string text, out int minutes, out int seconds)
        {
            minutes = 0;
            seconds = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Trim().Split(':');
            if (parts.Length > 2)
            {
                return false;
            }

            var minutesPart = parts[0].Trim();
            var secondsPart = parts.Length == 2 ? parts[1].Trim() : "0";

            // ":45" means three quarters of a minute, so an empty minutes half is zero.
            if (minutesPart.Length == 0)
            {
                minutesPart = "0";
            }
            if (secondsPart.Length == 0)
            {
                secondsPart = "0";
            }

            if (!int.TryParse(minutesPart, NumberStyles.None, CultureInfo.InvariantCulture, out minutes) ||
                !int.TryParse(secondsPart, NumberStyles.None, CultureInfo.InvariantCulture, out seconds))
            {
                return false;
            }

            return minutes >= 0 && minutes <= 1440 && seconds >= 0 && seconds <= 60 && (minutes > 0 || seconds > 0);
        }
    }
}
