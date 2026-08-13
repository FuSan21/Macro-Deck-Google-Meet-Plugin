/**
 * Meet's Host controls panel — everything a host can restrict, plus the meeting's access
 * type.
 *
 * ADDED BY THIS FORK.
 *
 * This is the one place in the extension where matching has to be done on visible text,
 * and it is worth being explicit about why. Every switch carries the same
 * `jsname="DMn7nd"` and every checkbox and radio the same `jsname="YPqjbf"`, so the
 * attribute cannot tell them apart, and none of them has an icon, so there is no ligature
 * to use either. That leaves the label, which Meet translates — so **this handler only
 * works with Meet in English**. Addressing them by position instead would silently start
 * flipping the wrong setting the day Google inserts a row, which is a far worse failure
 * than not finding one at all.
 *
 * The panel is not flat. Some rows are switches, one is a checkbox nested under a switch,
 * and the access type is a radio group with its own nested checkbox — hence the `kind` on
 * each entry below.
 *
 * Read off a live call on 2026-08-13.
 */
class HostControlsEventHandler extends SDEventHandler {

  static PanelId = "16";

  /** Switches carry one jsname, checkboxes and radios another. Both are generic. */
  static SwitchSelector = '[jsname="DMn7nd"][role="switch"]';

  static BoxSelector = 'input[type="checkbox"], input[type="radio"], [role="radio"]';

  /**
   * Keyed by the name the plugin sends.
   *
   *   kind "switch"   a labelled toggle — matched on its aria-label
   *   kind "box"      a nested checkbox — some carry an aria-label, some only row text,
   *                   so both are tried
   *
   * `Continuous meeting chat` is deliberately absent: Meet disables it for the duration of
   * a call, so a key bound to it could never do anything.
   */
  static Controls = {
    hostManagement: { kind: "switch", label: "Host management" },
    shareScreen: { kind: "switch", label: "Let contributors share their screen" },
    sendReactions: { kind: "switch", label: "Let contributors send reactions" },
    fullEmojiSet: { kind: "box", label: "Use the full set of emoji reactions" },
    turnOnMicrophone: { kind: "switch", label: "Let contributors turn on their microphone" },
    turnOnVideo: { kind: "switch", label: "Let contributors turn on their video" },
    sendMessages: { kind: "switch", label: "Let participants send messages" },
    askGemini: { kind: "switch", label: "Ask Gemini" },
    allowQuestions: { kind: "switch", label: "Allow questions in Q&A" },
    moderateQuestions: { kind: "switch", label: "Hide each question until a host approves" },
    anonymousQuestions: { kind: "switch", label: "Allow anonymous questions" },
    questionsInLiveStream: { kind: "switch", label: "Allow Q&A in live stream" },
    shareAddOns: { kind: "switch", label: "Let contributors share add-on activities" },
    thirdPartyCapture: { kind: "switch", label: "Allow third-party apps to collect audio and video" },
    anyoneWithLinkCanAsk: { kind: "box", label: "Anyone with the meeting link can ask to join" },
  };

  /** The Meeting access radio group. Selected, never toggled. */
  static AccessTypes = {
    open: "Open",
    trusted: "Trusted",
    restricted: "Restricted",
  };

  /**
   * The order settings are applied in, which matters because some rows only become
   * available once another is set. Host management ungreys most of the panel, the full
   * emoji set is nested under Send reactions, and the join checkbox only applies while
   * access is Trusted — so parents go before their children, with the access radio in
   * between.
   */
  static ApplyOrder = [
    "hostManagement",
    "shareScreen",
    "sendReactions",
    "fullEmojiSet",
    "turnOnMicrophone",
    "turnOnVideo",
    "sendMessages",
    "askGemini",
    "allowQuestions",
    "moderateQuestions",
    "anonymousQuestions",
    "questionsInLiveStream",
    "shareAddOns",
    "thirdPartyCapture",
  ];

  handleStreamDeckEvent = (message) => {
    if (message.event === "applyHostControls") {
      this._apply(message.controls || {}, message.access);
    }
  }

  /**
   * Applies a whole saved configuration in one go.
   *
   * Every setting is *set*, never toggled: the current state is read first and the row is
   * only clicked when it disagrees. That is what makes a deck key idempotent — pressing it
   * twice, or pressing it on a meeting somebody else has already half-configured, lands on
   * the same result either way.
   *
   * Anything absent from `controls` is left alone, which is the point of the "Leave as is"
   * option: a button for locking down a webinar should not quietly reset the settings it
   * has no opinion about.
   */
  _apply = async (controls, access) => {
    if (!await this._ensurePanel()) {
      return;
    }

    for (const name of HostControlsEventHandler.ApplyOrder) {
      if (typeof controls[name] !== "boolean") {
        continue;
      }

      // Access has to be Trusted before its nested checkbox means anything, so it is
      // applied once Host management is on and before everything downstream of it.
      if (name === "shareScreen" && access) {
        await this._setAccess(access);
      }

      await this._set(name, controls[name]);
    }

    if (access && typeof controls.hostManagement !== "boolean") {
      await this._setAccess(access);
    }

    if (typeof controls.anyoneWithLinkCanAsk === "boolean") {
      await this._set("anyoneWithLinkCanAsk", controls.anyoneWithLinkCanAsk);
    }
  }

  /** Reads a row's current state, whether it is a switch or a checkbox. */
  static _isOn = (element) => {
    const pressed = element.getAttribute("aria-checked");
    return pressed !== null ? pressed === "true" : element.checked === true;
  }

  _set = async (controlName, wanted) => {
    const control = HostControlsEventHandler.Controls[controlName];
    if (!control) {
      console.error("Unknown host control requested:", controlName);
      return;
    }

    const target = this._find(control);
    if (!target) {
      console.error(`No host control matching "${control.label}"; leaving it alone.`);
      return;
    }

    if (target.getAttribute("aria-disabled") === "true" || target.disabled) {
      console.error(`The "${control.label}" control is disabled; leaving it alone.`);
      return;
    }

    if (HostControlsEventHandler._isOn(target) === wanted) {
      return;
    }

    target.click();
    await new Promise((resolve) => setTimeout(resolve, 250));
  }

  /**
   * The controls only exist while the panel is open, so open it first. Presence of a
   * switch is what decides — pressing the panel button when it is already open would
   * close it, and the user may have opened it by hand.
   */
  _ensurePanel = async () => {
    if (document.querySelector(HostControlsEventHandler.SwitchSelector)?.getClientRects().length) {
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
      () => document.querySelector(HostControlsEventHandler.SwitchSelector)?.getClientRects().length);
  }

  /**
   * The text of the row an element sits in. Used for the controls Meet leaves unlabelled —
   * the access radios have no aria-label at all, and neither does the checkbox nested
   * under Trusted.
   */
  static _rowText = (element) => {
    let node = element;
    for (let i = 0; i < 6 && node; i++) {
      node = node.parentElement;
      const text = (node?.innerText || "").trim().replace(/\s+/g, " ");
      if (text.length > 2) {
        return text;
      }
    }
    return "";
  }

  static _visible = (selector) =>
    [...document.querySelectorAll(selector)].filter((e) => e.getClientRects().length);

  _find = (control) => {
    const selector = control.kind === "switch"
      ? HostControlsEventHandler.SwitchSelector
      : HostControlsEventHandler.BoxSelector;

    return HostControlsEventHandler._visible(selector).find((el) =>
      (el.getAttribute("aria-label") || "").startsWith(control.label) ||
      HostControlsEventHandler._rowText(el).startsWith(control.label));
  }

  /**
   * Picks one of Open, Trusted or Restricted. A radio is set rather than toggled, so
   * pressing the same key twice leaves the meeting where it already is.
   *
   * Note that Meet applies this to future instances of the meeting too, not just this
   * call — it is a lasting change, unlike everything else in this panel.
   */
  _setAccess = async (accessName) => {
    const label = HostControlsEventHandler.AccessTypes[accessName];
    if (!label) {
      console.error("Unknown meeting access type requested:", accessName);
      return;
    }

    const radios = HostControlsEventHandler._visible('[role="radio"], input[type="radio"]');
    const target = radios.find((el) => HostControlsEventHandler._rowText(el).startsWith(label));

    if (!target) {
      console.error(
        `No meeting access type matching "${label}". Options currently offered:`,
        radios.map((el) => HostControlsEventHandler._rowText(el).slice(0, 30)));
      return;
    }

    if (HostControlsEventHandler._isOn(target)) {
      return;
    }

    target.click();
    await new Promise((resolve) => setTimeout(resolve, 250));
  }

}
