/**
 * Meet's "Meeting tools" panel — Record, Transcribe, Polls, Q&A, Breakout rooms,
 * Speech translation, Timer and Live streaming — plus the two side panels that sit
 * next to it.
 *
 * ADDED BY THIS FORK.
 *
 * These are not toolbar buttons. Each one lives as a card inside a side panel that has
 * to be opened first, so every action here is "open the panel, wait for it to render,
 * then press the thing". The wait is unavoidable: Meet builds the panel's contents
 * asynchronously and they are simply absent for a beat after the panel button is clicked.
 *
 * Cards are matched on their Material Symbols ligature, with the jsname as a fallback.
 * Both were read off a live call in August 2026. The ligature is preferred because it is
 * an icon identifier rather than UI copy, so it survives a change of display language,
 * where the card's visible title ("Record", "Umsatz aufzeichnen", …) would not.
 */
class MeetingToolsEventHandler extends SDEventHandler {

  /** The side panel holding the tool cards. */
  static ToolsPanelId = "10";

  static HostControlsPanelId = "16";

  static MeetingDetailsPanelId = "5";

  /** One card per tool. `icon` is the ligature; `jsname` is the fallback. */
  static Tools = {
    speechTranslation: { icon: "translate_spark", jsname: "CytQbf" },
    record: { icon: "radio_button_checked", jsname: "USEHud" },
    transcribe: { icon: "speech_to_text", jsname: "gtwlKb" },
    breakoutRooms: { icon: "gmail_rooms", jsname: "XQlDeb" },
    polls: { icon: "poll", jsname: "A3EOJ" },
    questions: { icon: "contact_support", jsname: "cFicBd" },
    timer: { icon: "timer", jsname: "Ex2Bmd" },
    liveStreaming: { icon: "youtube_live", jsname: "iUug8e" },
  };

  /**
   * Transcribe is the one tool whose start/stop control sits on the card itself rather
   * than inside a sub-panel, so it can be driven without navigating any further.
   */
  static TranscribeToggleSelector = 'button[jsname="SWbFGf"]';

  static CardSelector = '[jsname="lTgCnb"]';

  static PanelButtonSelector = (panelId) => `[jsname="A5il2e"][data-panel-id="${panelId}"]`;

  handleStreamDeckEvent = (message) => {
    switch (message.event) {
      case "openMeetingTool":
        this._openTool(message.tool);
        break;
      case "toggleMeetingTools":
        this._togglePanel(MeetingToolsEventHandler.ToolsPanelId);
        break;
      case "toggleHostControls":
        this._togglePanel(MeetingToolsEventHandler.HostControlsPanelId);
        break;
      case "toggleMeetingDetails":
        this._togglePanel(MeetingToolsEventHandler.MeetingDetailsPanelId);
        break;
      case "toggleTranscription":
        this._toggleTranscription();
        break;
    }
  }

  _togglePanel = (panelId) => {
    const button = document.querySelector(MeetingToolsEventHandler.PanelButtonSelector(panelId));
    if (!button) {
      throw new ControlsNotFoundError(`No side panel button found for panel ${panelId}!`);
    }
    button.click();
  }

  /**
   * Opens the tools panel if the cards are not already showing, and resolves once they
   * are. Pressing the panel button when it is already open would close it, so presence
   * of a card is what decides, not a remembered flag — the user may have opened or
   * closed the panel by hand at any point.
   */
  _ensureToolsPanel = async () => {
    if (document.querySelector(MeetingToolsEventHandler.CardSelector)) {
      return true;
    }

    this._togglePanel(MeetingToolsEventHandler.ToolsPanelId);
    return await MeetingToolsEventHandler.waitFor(
      () => document.querySelector(MeetingToolsEventHandler.CardSelector));
  }

  _findCard = (tool) => {
    for (const card of document.querySelectorAll(MeetingToolsEventHandler.CardSelector)) {
      if (card.querySelector("i")?.textContent.trim() === tool.icon) {
        return card;
      }
    }

    return document.querySelector(`[jsname="${tool.jsname}"]`)?.closest(MeetingToolsEventHandler.CardSelector) ?? null;
  }

  _openTool = async (toolName) => {
    const tool = MeetingToolsEventHandler.Tools[toolName];
    if (!tool) {
      console.error("Unknown meeting tool requested:", toolName);
      return;
    }

    if (!await this._ensureToolsPanel()) {
      throw new ControlsNotFoundError("The meeting tools panel did not open!");
    }

    const card = this._findCard(tool);
    if (!card) {
      // Expected for anything the account is not entitled to, and for tools Meet only
      // offers to the host — the card is absent rather than disabled in that case.
      console.error(
        `The "${toolName}" tool is not available in this meeting. Cards currently offered:`,
        [...document.querySelectorAll(MeetingToolsEventHandler.CardSelector)]
          .map((c) => c.querySelector("i")?.textContent.trim()));
      return;
    }

    if (card.querySelector('[aria-disabled="true"]')) {
      console.error(`The "${toolName}" tool is present but disabled — it likely needs a paid Workspace plan.`);
      return;
    }

    card.querySelector('[role="button"]')?.click();
  }

  /**
   * Starts or stops transcription. The button lives on the Transcribe card, so the panel
   * has to be open for it to exist at all — but it is a genuine one-press toggle once
   * there, unlike recording, which asks for confirmation in a sub-panel.
   */
  _toggleTranscription = async () => {
    if (!await this._ensureToolsPanel()) {
      throw new ControlsNotFoundError("The meeting tools panel did not open!");
    }

    const button = document.querySelector(MeetingToolsEventHandler.TranscribeToggleSelector);
    if (!button) {
      console.error("No transcription button found — this account may not offer transcripts.");
      return;
    }

    button.click();
  }

  /**
   * Polls a predicate until it is truthy or the budget runs out. Returns whether it ever
   * became true, so callers can report a miss instead of pressing into a panel that
   * never appeared.
   */
  static waitFor = async (predicate, timeoutMs = 3000, intervalMs = 100) => {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      if (predicate()) {
        return true;
      }
      await new Promise((resolve) => setTimeout(resolve, intervalMs));
    }
    return Boolean(predicate());
  }

}
