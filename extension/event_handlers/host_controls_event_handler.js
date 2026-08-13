/**
 * Meet's Host controls panel — the fourteen switches that decide what everyone else in
 * the call is allowed to do.
 *
 * ADDED BY THIS FORK.
 *
 * These are the one place in this extension where matching has to be done on visible
 * text, and it is worth being explicit about why. Every switch carries the same
 * `jsname="DMn7nd"`, so the attribute cannot tell them apart, and none of them has an
 * icon, so there is no ligature to use either. That leaves the `aria-label`, which is
 * real UI copy and therefore translated — so **this handler only works with Meet in
 * English**. The alternative, addressing them by position, would silently start toggling
 * the wrong setting the day Google inserts a fifteenth switch, which is a far worse
 * failure than not finding one at all.
 *
 * Read off a live call on 2026-08-13.
 */
class HostControlsEventHandler extends SDEventHandler {

  static PanelId = "16";

  static SwitchSelector = '[jsname="DMn7nd"][role="switch"]';

  /**
   * Keyed by the name the plugin sends; the value is the start of the switch's label.
   *
   * Meet offers fourteen switches. Only these seven are here — the ones a host might
   * reach for during a call. The rest (Ask Gemini, Q&A in live stream, add-on activities,
   * third-party capture, continuous chat, hide-until-approved, anonymous questions) are
   * all set once before a webinar starts, and carrying them would only bury these.
   */
  static Controls = {
    hostManagement: "Host management",
    shareScreen: "Let contributors share their screen",
    turnOnMicrophone: "Let contributors turn on their microphone",
    turnOnVideo: "Let contributors turn on their video",
    sendReactions: "Let contributors send reactions",
    sendMessages: "Let participants send messages",
    allowQuestions: "Allow questions in Q&A",
  };

  handleStreamDeckEvent = (message) => {
    if (message.event === "toggleHostControl") {
      this._toggle(message.control);
    }
  }

  /**
   * The switches only exist while the panel is open, so open it first. Presence of a
   * switch is what decides — pressing the panel button when it is already open would
   * close it, and the user may have opened it by hand.
   */
  _ensurePanel = async () => {
    if (document.querySelector(HostControlsEventHandler.SwitchSelector)) {
      return true;
    }

    const button = document.querySelector(
      `[jsname="A5il2e"][data-panel-id="${HostControlsEventHandler.PanelId}"]`);
    if (!button) {
      // Expected for anyone who is not the host: Meet does not render the panel button.
      console.error("No host controls panel — you are probably not the host of this meeting.");
      return false;
    }

    button.click();
    return await MeetingToolsEventHandler.waitFor(
      () => document.querySelector(HostControlsEventHandler.SwitchSelector));
  }

  _toggle = async (controlName) => {
    const label = HostControlsEventHandler.Controls[controlName];
    if (!label) {
      console.error("Unknown host control requested:", controlName);
      return;
    }

    if (!await this._ensurePanel()) {
      return;
    }

    const target = [...document.querySelectorAll(HostControlsEventHandler.SwitchSelector)]
      .find((el) => (el.getAttribute("aria-label") || "").startsWith(label));

    if (!target) {
      console.error(
        `No host control matching "${label}". Switches currently offered:`,
        [...document.querySelectorAll(HostControlsEventHandler.SwitchSelector)]
          .map((el) => el.getAttribute("aria-label")));
      return;
    }

    // Some switches are greyed out until the master "Host management" one is on.
    if (target.getAttribute("aria-disabled") === "true" || target.disabled) {
      console.error(`The "${label}" switch is disabled — turn on Host management first.`);
      return;
    }

    target.click();
  }

}
