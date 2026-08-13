using Newtonsoft.Json;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FuSan21.MacroDeck.GoogleMeet.UI
{
    /// <summary>
    /// The whole Host controls panel as one saved configuration, laid out in the same
    /// sections and order Meet uses so it reads as the same thing.
    ///
    /// Every row is three-way — On, Off, or <b>Leave as is</b>, which is the default. That
    /// third option is what makes a saved config usable: a button for locking down a
    /// webinar should say nothing about the settings it has no opinion on, rather than
    /// quietly resetting them.
    /// </summary>
    public class HostControlsConfig : ActionConfigControl
    {
        public class Settings
        {
            /// <summary>Only the controls the user actually decided on. Absent means "leave alone".</summary>
            public Dictionary<HostControl, bool> Controls { get; set; } = new Dictionary<HostControl, bool>();

            public MeetingAccess? Access { get; set; }
        }

        /// <summary>Wrap width for the descriptions and the closing note.</summary>
        private const int RowWidth = 580;

        /// <summary>
        /// Wide enough for the longest row Meet has ("Allow third-party apps to collect
        /// audio and video"), plus the four-space indent the nested ones carry.
        /// </summary>
        private const int LabelWidth = 400;

        private const int ChoiceWidth = 120;

        /// <summary>
        /// Rows are built with <see cref="Label.UseMnemonic"/> off. Meet has several
        /// settings with "Q&amp;A" in the name, and a WinForms label eats an ampersand as an
        /// accelerator prefix — so left on, "Allow questions in Q&amp;A" renders as
        /// "Allow questions in QA".
        /// </summary>
        private static Label RowLabel(string text) => new Label
        {
            Text = text,
            UseMnemonic = false,
            AutoSize = false,
            Size = new Size(LabelWidth, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 8, 0)
        };

        private readonly PluginAction _action;
        private readonly Dictionary<HostControl, RoundedComboBox> _rows =
            new Dictionary<HostControl, RoundedComboBox>();
        private readonly RoundedComboBox _access;

        public HostControlsConfig(PluginAction action)
        {
            _action = action;
            Dock = DockStyle.Fill;

            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            layout.Controls.Add(Intro(
                "Everything left on \"Leave as is\" is untouched, so one button can set just " +
                "the handful of settings it cares about."));

            Section(layout, "Meeting moderation");
            Row(layout, HostControl.HostManagement, "Host management",
                "The master switch. Most rows below are greyed out until it is on.");

            Section(layout, "Let contributors");
            Row(layout, HostControl.LetContributorsShareScreen, "Share their screen", null);
            Row(layout, HostControl.LetContributorsSendReactions, "Send reactions", null);
            Row(layout, HostControl.UseFullEmojiSet, "    Use the full set of emoji reactions",
                "Nested under Send reactions.");
            Row(layout, HostControl.LetContributorsUnmute, "Turn on their microphone",
                "Turning this off can drop people on older clients.");
            Row(layout, HostControl.LetContributorsTurnOnVideo, "Turn on their video",
                "Turning this off can drop people on older clients.");

            Section(layout, "Chat moderation");
            Row(layout, HostControl.LetParticipantsSendMessages, "Let participants send messages", null);

            Section(layout, "Gemini");
            Row(layout, HostControl.AskGemini, "Ask Gemini",
                "Whether people can ask Gemini about the call.");

            Section(layout, "Meeting access");
            _access = AccessRow(layout);
            Row(layout, HostControl.AnyoneWithLinkCanAsk, "    Anyone with the link can ask to join",
                "Nested under Trusted; ignored under the other two.");

            Section(layout, "Meeting activities");
            Row(layout, HostControl.AllowQuestions, "Allow questions in Q&A", null);
            Row(layout, HostControl.HideQuestionsUntilApproved, "    Hide each question until a host approves", null);
            Row(layout, HostControl.AllowAnonymousQuestions, "    Allow anonymous questions",
                "Askers can hide their name, including from the host.");
            Row(layout, HostControl.AllowQuestionsInLiveStream, "    Allow Q&A in live stream", null);
            Row(layout, HostControl.LetContributorsShareAddOns, "Let contributors share add-on activities",
                "When off, only a host can start an activity.");
            Row(layout, HostControl.AllowThirdPartyCapture, "Allow third-party apps to collect audio and video", null);

            layout.Controls.Add(Hint(
                "Meet's \"Continuous meeting chat\" is missing on purpose: it cannot be changed " +
                "during a call. Meeting access outlasts the call — Meet applies it to future " +
                "instances of the meeting too."));

            Controls.Add(layout);

            Restore(LoadConfig(_action.Configuration));
        }

        #region Layout helpers

        private static Control Intro(string text) => new Label
        {
            Text = text,
            UseMnemonic = false,
            AutoSize = true,
            MaximumSize = new Size(RowWidth, 0),
            ForeColor = Color.Gray,
            Margin = new Padding(0, 0, 0, 6)
        };

        private static Label Hint(string text) => new Label
        {
            Text = text,
            UseMnemonic = false,
            AutoSize = true,
            MaximumSize = new Size(RowWidth, 0),
            ForeColor = Color.Gray,
            Margin = new Padding(0, 12, 0, 0)
        };

        private static void Section(FlowLayoutPanel layout, string title)
        {
            layout.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(0, 14, 0, 4)
            });
        }

        private void Row(FlowLayoutPanel layout, HostControl control, string label, string description)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 2, 0, 0)
            };

            row.Controls.Add(RowLabel(label));

            var choice = new RoundedComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = ChoiceWidth
            };
            choice.Items.AddRange(new object[] { "Leave as is", "On", "Off" });
            choice.SelectedIndex = 0;
            row.Controls.Add(choice);

            _rows[control] = choice;
            layout.Controls.Add(row);

            if (!string.IsNullOrEmpty(description))
            {
                layout.Controls.Add(new Label
                {
                    Text = description,
                    UseMnemonic = false,
                    AutoSize = true,
                    MaximumSize = new Size(RowWidth, 0),
                    ForeColor = Color.Gray,
                    Margin = new Padding(0, 0, 0, 4)
                });
            }
        }

        private RoundedComboBox AccessRow(FlowLayoutPanel layout)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 2, 0, 0)
            };

            row.Controls.Add(RowLabel("Meeting access type"));

            var choice = new RoundedComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = ChoiceWidth
            };
            choice.Items.AddRange(new object[] { "Leave as is", "Open", "Trusted", "Restricted" });
            choice.SelectedIndex = 0;
            row.Controls.Add(choice);
            layout.Controls.Add(row);

            layout.Controls.Add(new Label
            {
                Text = "Open: nobody asks. Trusted: your organisation joins directly, " +
                       "others ask. Restricted: invited Google Accounts only.",
                UseMnemonic = false,
                AutoSize = true,
                MaximumSize = new Size(RowWidth, 0),
                ForeColor = Color.Gray,
                Margin = new Padding(0, 0, 0, 4)
            });

            return choice;
        }

        #endregion

        private void Restore(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (settings.Controls != null)
            {
                foreach (var pair in settings.Controls)
                {
                    if (_rows.TryGetValue(pair.Key, out var choice))
                    {
                        choice.SelectedIndex = pair.Value ? 1 : 2;
                    }
                }
            }

            _access.SelectedIndex = settings.Access.HasValue ? (int)settings.Access.Value + 1 : 0;
        }

        public override bool OnActionSave()
        {
            var settings = new Settings();

            foreach (var pair in _rows)
            {
                // 0 is "Leave as is", which means the control is simply not sent.
                if (pair.Value.SelectedIndex == 1)
                {
                    settings.Controls[pair.Key] = true;
                }
                else if (pair.Value.SelectedIndex == 2)
                {
                    settings.Controls[pair.Key] = false;
                }
            }

            if (_access.SelectedIndex > 0)
            {
                settings.Access = (MeetingAccess)(_access.SelectedIndex - 1);
            }

            _action.Configuration = JsonConvert.SerializeObject(settings);

            var count = settings.Controls.Count + (settings.Access.HasValue ? 1 : 0);
            _action.ConfigurationSummary = count == 0
                ? "Nothing set"
                : count == 1 ? "1 setting" : $"{count} settings";

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
