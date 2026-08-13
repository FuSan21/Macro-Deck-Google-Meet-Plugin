using Newtonsoft.Json;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Windows.Forms;

namespace FuSan21.MacroDeck.GoogleMeet.UI
{
    /// <summary>
    /// A dropdown for picking one value of an enum. Currently only the reaction action
    /// needs one, but it is written generically because every other "which one?" choice
    /// has the same shape.
    /// </summary>
    public class ChoiceConfig<T> : ActionConfigControl where T : struct, Enum
    {
        public class Settings
        {
            public T Choice { get; set; }
        }

        private readonly PluginAction _action;
        private readonly RoundedComboBox _choice;

        public ChoiceConfig(PluginAction action, string label)
        {
            _action = action;
            Dock = DockStyle.Fill;

            _choice = new RoundedComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            _choice.Items.AddRange(Enum.GetNames(typeof(T)));
            _choice.SelectedIndex = 0;

            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            layout.Controls.Add(new Label { Text = label, AutoSize = true });
            layout.Controls.Add(_choice);
            Controls.Add(layout);

            var config = LoadConfig(_action.Configuration);
            if (config != null)
            {
                var index = _choice.Items.IndexOf(config.Choice.ToString());
                if (index >= 0)
                {
                    _choice.SelectedIndex = index;
                }
            }
        }

        public override bool OnActionSave()
        {
            if (!Enum.TryParse<T>(_choice.SelectedItem?.ToString(), out var choice))
            {
                choice = default;
            }

            _action.Configuration = JsonConvert.SerializeObject(new Settings { Choice = choice });
            _action.ConfigurationSummary = choice.ToString();
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
