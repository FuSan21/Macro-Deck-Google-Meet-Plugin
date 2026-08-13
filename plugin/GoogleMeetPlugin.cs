using FuSan21.MacroDeck.GoogleMeet.Actions;
using SuchByte.MacroDeck.Events;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Profiles;
using SuchByte.MacroDeck.Variables;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FuSan21.MacroDeck.GoogleMeet
{
    public static class PluginInstance
    {
        public static AppLogger Logger;
        public static GoogleMeetPlugin Plugin;
    }

    public class GoogleMeetPlugin : MacroDeckPlugin
    {
        public Configuration configuration;

        public override bool CanConfigure => true;

        private ContentSelectorButton statusButton = new ContentSelectorButton();
        private readonly ToolTip statusToolTip = new ToolTip();
        private MainWindow mainWindow;

        private readonly MeetingStateChangedEvent _stateChangedEvent = new MeetingStateChangedEvent();

        public class VariableState
        {
            public string Name { get; set; }
            public VariableType Type { get; set; } = VariableType.Bool;
            public object Value { get; set; } = false;
        }

        public GoogleMeetPlugin()
        {
            PluginInstance.Plugin ??= this;
            PluginInstance.Logger ??= new AppLogger();
            SuchByte.MacroDeck.MacroDeck.OnMainWindowLoad += MacroDeck_OnMainWindowLoad;
        }

        public override void Enable()
        {
            try
            {
                configuration ??= new Configuration(this);
                ResetVariables();

                Actions = new List<PluginAction>
                {
                    new ToggleMicrophoneAction(),
                    new MuteMicrophoneAction(),
                    new UnmuteMicrophoneAction(),
                    new ToggleCameraAction(),
                    new EnableCameraAction(),
                    new DisableCameraAction(),
                    new ToggleHandAction(),
                    new TogglePresentAction(),
                    new ToggleCaptionsAction(),
                    new ToggleChatAction(),
                    new ToggleParticipantsAction(),
                    new TogglePinPresentationAction(),
                    new ToggleZenModeAction(),
                    new SendReactionAction(),
                    new LeaveCallAction(),
                    new OpenMeetAction(),
                };

                EventManager.RegisterEvent(_stateChangedEvent);
                MeetHelper.AvailabilityChanged += MeetHelper_AvailabilityChanged;

                if (SuchByte.MacroDeck.MacroDeck.MainWindow != null && !SuchByte.MacroDeck.MacroDeck.MainWindow.IsDisposed)
                {
                    MacroDeck_OnMainWindowLoad(SuchByte.MacroDeck.MacroDeck.MainWindow, EventArgs.Empty);
                }

                ApplyEnabledState();
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }

        /// <summary>Opens or closes the listening socket to match the configured state.</summary>
        public void ApplyEnabledState()
        {
            if (configuration != null && configuration.Enabled)
            {
                MeetHelper.Start(configuration);
            }
            else
            {
                PluginInstance.Logger.Info("Google Meet integration is disabled");
                MeetHelper.Stop();
            }

            UpdateStatusButton();
        }

        #region Variables

        public void SetVariable(VariableState variableState)
        {
            VariableManager.SetValue($"meet_{variableState.Name}", variableState.Value, variableState.Type, this, null);
        }

        public void SetVariable(VariableState[] variableStates)
        {
            foreach (var state in variableStates)
            {
                SetVariable(state);
            }
        }

        public void ApplyState(MeetState previous, MeetState current)
        {
            if (current == null)
            {
                return;
            }

            var variables = new List<VariableState>();
            foreach (var flag in current.TrackedFlags())
            {
                variables.Add(new VariableState { Name = flag.Name, Value = flag.Value });
            }
            SetVariable(variables.ToArray());

            // The shared slot is only claimed when the call is a fact rather than an
            // inference. With the official extension an open Meet tab looks exactly like a
            // live call, and letting a background tab evict an in-progress Teams or Zoom
            // meeting from the shared variables would be worse than not taking part.
            if (current.InCallKnown && current.InMeeting)
            {
                SetSharedVariables(current);
            }
            else if (previous != null && previous.InCallKnown && previous.InMeeting)
            {
                ClearSharedVariables();
            }

            FireChangedEvents(previous, current);
        }

        private void SetSharedVariables(MeetState state)
        {
            VariableManager.SetValue("meeting_platform", "meet", VariableType.String, this, null);
            VariableManager.SetValue("meeting_in_meeting", true, VariableType.Bool, this, null);
            VariableManager.SetValue("meeting_is_muted", state.IsMuted, VariableType.Bool, this, null);
            VariableManager.SetValue("meeting_camera_on", state.IsVideoOn, VariableType.Bool, this, null);
        }

        private void ClearSharedVariables()
        {
            // Only surrender the shared slot if this plugin is the one holding it —
            // otherwise leaving a Meet call would clobber an in-progress Teams meeting.
            var platform = Array.Find(VariableManager.Variables, v => v.Name == "meeting_platform")?.Value;
            if (!string.IsNullOrEmpty(platform) && !platform.Equals("meet", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            VariableManager.SetValue("meeting_platform", "none", VariableType.String, this, null);
            VariableManager.SetValue("meeting_in_meeting", false, VariableType.Bool, this, null);
            VariableManager.SetValue("meeting_is_muted", false, VariableType.Bool, this, null);
            VariableManager.SetValue("meeting_camera_on", false, VariableType.Bool, this, null);
        }

        public void ResetVariables()
        {
            ApplyState(null, MeetState.Empty);
            ClearSharedVariables();
        }

        #endregion

        #region Events

        private void FireChangedEvents(MeetState previous, MeetState current)
        {
            if (previous == null)
            {
                return;
            }

            var before = new Dictionary<string, bool>();
            foreach (var flag in previous.TrackedFlags())
            {
                before[flag.Name] = flag.Value;
            }

            foreach (var flag in current.TrackedFlags())
            {
                if (before.TryGetValue(flag.Name, out var was) && was != flag.Value)
                {
                    _stateChangedEvent.Trigger(flag.Name);
                }
            }
        }

        public class MeetingStateChangedEvent : IMacroDeckEvent
        {
            public string Name => "Google Meet state changed";

            public EventHandler<MacroDeckEventArgs> OnEvent { get; set; }

            public List<string> ParameterSuggestions
            {
                get
                {
                    var suggestions = new List<string>();
                    foreach (var flag in MeetState.Empty.TrackedFlags())
                    {
                        suggestions.Add(flag.Name);
                    }
                    return suggestions;
                }
                set { }
            }

            public void Trigger(string parameter)
            {
                if (OnEvent == null)
                {
                    return;
                }

                try
                {
                    foreach (var profile in ProfileManager.Profiles)
                    {
                        foreach (var folder in profile.Folders)
                        {
                            if (folder.ActionButtons == null)
                            {
                                continue;
                            }

                            var buttons = folder.ActionButtons.FindAll(actionButton =>
                                actionButton.EventListeners != null &&
                                actionButton.EventListeners.Find(listener =>
                                    listener.EventToListen != null &&
                                    listener.EventToListen.Equals(Name)) != null);

                            foreach (var actionButton in buttons)
                            {
                                OnEvent(this, new MacroDeckEventArgs
                                {
                                    ActionButton = actionButton,
                                    Parameter = parameter,
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginInstance.Logger.Error("Failed to dispatch state changed event:\n{0}", ex);
                }
            }
        }

        #endregion

        #region Status button

        private void MacroDeck_OnMainWindowLoad(object sender, EventArgs e)
        {
            try
            {
                mainWindow = sender as MainWindow;
                statusButton = new ContentSelectorButton
                {
                    BackgroundImageLayout = ImageLayout.Zoom
                };
                statusButton.Click += StatusButton_Click;
                mainWindow?.contentButtonPanel.Controls.Add(statusButton);
                UpdateStatusButton();
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }

        private void MeetHelper_AvailabilityChanged(object sender, EventArgs e) => UpdateStatusButton();

        private void UpdateStatusButton()
        {
            try
            {
                if (mainWindow == null || !mainWindow.IsHandleCreated || statusButton == null || statusButton.IsDisposed)
                {
                    return;
                }

                mainWindow.BeginInvoke(new Action(() =>
                {
                    var enabled = configuration != null && configuration.Enabled;
                    statusButton.BackgroundImage = enabled ? Icons.Enabled : Icons.Disabled;

                    string tooltip;
                    if (!enabled)
                    {
                        tooltip = "Google Meet — disabled";
                    }
                    else if (MeetHelper.IsAvailable)
                    {
                        var tabs = MeetHelper.ConnectedTabs;
                        tooltip = tabs == 1
                            ? "Google Meet — 1 tab connected"
                            : $"Google Meet — {tabs} tabs connected";
                    }
                    else
                    {
                        tooltip = "Google Meet — waiting for the browser extension";
                    }

                    if (enabled && !string.IsNullOrWhiteSpace(MeetHelper.LastError))
                    {
                        tooltip += $" — {MeetHelper.LastError}";
                    }

                    statusToolTip.SetToolTip(statusButton, tooltip);
                }));
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }

        private void StatusButton_Click(object sender, EventArgs e) => OpenConfigurator();

        #endregion

        public override void OpenConfigurator()
        {
            using var configurator = new UI.ConfigurationForm(configuration);
            configurator.ShowDialog();
        }
    }
}
