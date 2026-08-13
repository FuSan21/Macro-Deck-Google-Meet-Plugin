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
    /// The Breakout Rooms action's configuration: which command, plus the one number two of
    /// them need — a room to join, or a countdown to set.
    /// </summary>
    public class BreakoutConfig : ActionConfigControl
    {
        public class Settings
        {
            public BreakoutAction Action { get; set; }

            /// <summary>Which room to join, counting from 1 the way Meet lists them.</summary>
            public int Room { get; set; } = 1;

            /// <summary>Minutes before everyone is returned to the main call. 0 clears it.</summary>
            public int Minutes { get; set; } = 30;
        }

        private const int TextWidth = 420;

        private readonly PluginAction _action;
        private readonly RoundedComboBox _choice;
        private readonly RoundedTextBox _number;
        private readonly Label _numberLabel;
        private readonly Label _description;

        public BreakoutConfig(PluginAction action)
        {
            _action = action;
            Dock = DockStyle.Fill;

            _choice = new RoundedComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220
            };
            _choice.Items.AddRange(Enum.GetNames(typeof(BreakoutAction)));
            _choice.SelectedIndex = 0;
            _choice.SelectedIndexChanged += (s, e) => UpdateForAction();

            _description = new Label
            {
                UseMnemonic = false,
                AutoSize = true,
                MaximumSize = new Size(TextWidth, 0),
                ForeColor = Color.Gray,
                Margin = new Padding(0, 4, 0, 0)
            };

            _numberLabel = new Label
            {
                UseMnemonic = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 3)
            };
            _number = new RoundedTextBox { Width = 100 };

            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            layout.Controls.Add(new Label { Text = "Breakout action", AutoSize = true });
            layout.Controls.Add(_choice);
            layout.Controls.Add(_description);
            layout.Controls.Add(_numberLabel);
            layout.Controls.Add(_number);
            Controls.Add(layout);

            var config = LoadConfig(_action.Configuration);
            if (config != null)
            {
                var index = _choice.Items.IndexOf(config.Action.ToString());
                if (index >= 0)
                {
                    _choice.SelectedIndex = index;
                }

                _number.Text = (config.Action == BreakoutAction.JoinRoom
                    ? config.Room
                    : config.Minutes).ToString(CultureInfo.InvariantCulture);
            }

            UpdateForAction();
        }

        private BreakoutAction Selected =>
            Enum.TryParse<BreakoutAction>(_choice.SelectedItem?.ToString(), out var a) ? a : BreakoutAction.Shuffle;

        /// <summary>
        /// Shows the number box only for the two commands that take one, and relabels it
        /// for whichever it is.
        /// </summary>
        private void UpdateForAction()
        {
            var action = Selected;
            _description.Text = Describe(action);

            var needsRoom = action == BreakoutAction.JoinRoom;
            var needsMinutes = action == BreakoutAction.SetTimer;

            _numberLabel.Visible = needsRoom || needsMinutes;
            _number.Visible = needsRoom || needsMinutes;

            if (needsRoom)
            {
                _numberLabel.Text = "Room number";
                _number.PlaceHolderText = "1";
            }
            else if (needsMinutes)
            {
                _numberLabel.Text = "Minutes (0 to clear)";
                _number.PlaceHolderText = "30";
            }
        }

        /// <summary>
        /// One line each. The states matter more than the names here: half of these only
        /// exist while the rooms are open, and the other half only while they are not.
        /// </summary>
        private static string Describe(BreakoutAction action)
        {
            switch (action)
            {
                case BreakoutAction.OpenRooms:
                    return "Opens the rooms and moves everyone into them.";

                case BreakoutAction.CloseRooms:
                    return "Closes the rooms. Meet asks first, so press again to confirm.";

                case BreakoutAction.JoinRoom:
                    return "Joins a room yourself. The tab moves to that room's own call.";

                case BreakoutAction.ReturnToMainCall:
                    return "Leaves the room you are in and goes back to the main call.";

                case BreakoutAction.SetTimer:
                    return "Sets the countdown that returns everyone to the main call.";

                case BreakoutAction.Clear:
                    return "Empties the room assignments. Needs people in the call.";

                case BreakoutAction.EditRooms:
                    return "Opens the editor, where rooms are named and people assigned.";

                case BreakoutAction.CancelChanges:
                    return "Discards edits without opening the rooms.";

                default:
                    return "Assigns everyone to rooms at random. Needs people in the call.";
            }
        }

        public override bool OnActionSave()
        {
            var action = Selected;
            var settings = LoadConfig(_action.Configuration) ?? new Settings();
            settings.Action = action;

            var text = (_number.Text ?? string.Empty).Trim();
            if (action == BreakoutAction.JoinRoom || action == BreakoutAction.SetTimer)
            {
                var min = action == BreakoutAction.JoinRoom ? 1 : 0;
                if (text.Length > 0 &&
                    (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                     number < min || number > 999))
                {
                    using var error = new SuchByte.MacroDeck.GUI.CustomControls.MessageBox();
                    error.ShowDialog(
                        "Google Meet",
                        $"\"{text}\" is not a whole number between {min} and 999.",
                        MessageBoxButtons.OK);
                    return false;
                }

                var value = text.Length > 0
                    ? int.Parse(text, CultureInfo.InvariantCulture)
                    : (action == BreakoutAction.JoinRoom ? 1 : 30);

                if (action == BreakoutAction.JoinRoom)
                {
                    settings.Room = value;
                }
                else
                {
                    settings.Minutes = value;
                }
            }

            _action.Configuration = JsonConvert.SerializeObject(settings);

            _action.ConfigurationSummary = action == BreakoutAction.JoinRoom
                ? $"Join room {settings.Room}"
                : action == BreakoutAction.SetTimer
                    ? (settings.Minutes == 0 ? "Clear timer" : $"Timer {settings.Minutes} min")
                    : action.ToString();

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
    }
}
