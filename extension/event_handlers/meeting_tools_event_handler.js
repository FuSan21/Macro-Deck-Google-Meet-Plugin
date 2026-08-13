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

  /**
   * One entry per card.
   *
   *   icon        the Material Symbols ligature on the card — how it is found
   *   jsname      fallback, in case Google renames the ligature
   *   primary     the tool's main button, inside its sub-panel
   *   cardPrimary same button when Meet renders it inline on the card instead, which it
   *               does for Transcribe in some layouts — saves opening the sub-panel
   *
   * All read off a live call on 2026-08-13. The sub-panel buttons have no icon of their
   * own, so unlike the cards they can only be addressed by jsname.
   */
  static Tools = {
    speechTranslation: {
      icon: "translate_spark", jsname: "CytQbf",
      primary: '[jsname="ZU5GHf"]', off: '[jsname="OUK46e"]',
    },
    record: { icon: "radio_button_checked", jsname: "USEHud", primary: '[jsname="A0ONe"]' },
    transcribe: {
      icon: "speech_to_text", jsname: "gtwlKb",
      primary: '[jsname="z0F4cd"]', cardPrimary: 'button[jsname="SWbFGf"]',
    },
    breakoutRooms: { icon: "gmail_rooms", jsname: "XQlDeb", primary: '[jsname="jjbqZd"]' },
    polls: { icon: "poll", jsname: "A3EOJ", primary: '[jsname="Jf6tn"]' },
    questions: { icon: "contact_support", jsname: "cFicBd", primary: '[jsname="pob3rf"]' },
    timer: { icon: "timer", jsname: "Ex2Bmd", primary: '[jsname="SPCnpb"]' },
    liveStreaming: { icon: "youtube_live", jsname: "iUug8e", primary: null },
  };

  /**
   * The buttons inside the Breakout rooms editor, and the three checkboxes Meet offers
   * that are worth a key of their own.
   *
   * Opening the editor is left to the Breakout rooms tool; its Cancel and room-timer
   * controls are deliberately absent, because you are looking at the form when you use
   * those. Shuffle assigns everyone at random, which is what turns "shuffle, then open"
   * into a two-key sequence that runs the whole feature.
   */
  static BreakoutActions = {
    edit: { selector: '[jsname="jjbqZd"]', editor: false },
    openRooms: { selector: '[jsname="vGYErf"]', editor: true },
    closeRooms: { selector: '[jsname="rFUlDe"]', editor: false },
    shuffle: { selector: '[jsname="ZvBrEb"]', editor: true },
    clear: { selector: '[jsname="uL0KOe"]', editor: true },
    cancelChanges: { selector: '[jsname="TLo5Gb"]', editor: true },
    returnToMainCall: { selector: '[jsname="Oogxpb"]', editor: false },
  };

  /** One per room, in the order Meet lists them. Only present while rooms are open. */
  static BreakoutJoinSelector = '[jsname="andTgb"]';

  /** Opens the "End breakout rooms after a set amount of time" dialog. */
  static BreakoutTimerSelector = '[jsname="bOBs5e"]';

  /** Anything that only exists while the editor is showing. */
  static BreakoutEditorSelector = '[jsname="TLo5Gb"]';

  static CardSelector = '[jsname="lTgCnb"]';

  static PanelButtonSelector = (panelId) => `[jsname="A5il2e"][data-panel-id="${panelId}"]`;

  handleStreamDeckEvent = (message) => {
    switch (message.event) {
      case "openMeetingTool":
        this._openTool(message.tool);
        break;
      case "startMeetingTool":
        this._startTool(message.tool);
        break;
      case "breakoutAction":
        this._breakout(message.action, message.room, message.minutes);
        break;
    }
  }

  /**
   * Answers whichever confirmation Meet is showing.
   *
   * Recording, transcription and turning translation off each open a dialog, and they do
   * not agree on the confirm button's action token — recording and transcription use
   * "A9Emjd", translation uses plain "ok". What they do agree on is that cancelling is
   * always "cancel", so the confirm button is defined as the one that is not it. That
   * holds whatever Google names the next one, and needs no visible text, so it works in
   * any language.
   */
  static visibleDialog = () =>
    [...document.querySelectorAll('[role="dialog"], [role="alertdialog"]')]
      .find((d) => d.getClientRects().length);

  /**
   * Writes a value into one of Meet's number boxes.
   *
   * Assigning to `.value` alone changes what is on screen and nothing else — Meet listens
   * for the `input` event, not for the property — so the write goes through the prototype's
   * own setter and the event is raised by hand.
   */
  static setInputValue = (input, value) => {
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
    input.focus();
    setter.call(input, String(value));
    input.dispatchEvent(new Event("input", { bubbles: true }));
    input.dispatchEvent(new Event("change", { bubbles: true }));
    input.blur();
  }

  static confirmDialog = () => {
    const dialog = MeetingToolsEventHandler.visibleDialog();
    if (!dialog) {
      return false;
    }

    const confirm = [...dialog.querySelectorAll("[data-mdc-dialog-action]")]
      .find((b) => b.getAttribute("data-mdc-dialog-action") !== "cancel");
    if (!confirm) {
      return false;
    }

    confirm.click();
    return true;
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
  /**
   * The cards, but only the ones actually on screen.
   *
   * Presence is not enough: once the tools panel has been opened, Meet leaves the cards
   * in the DOM for the rest of the call, so they are still found when the panel is
   * closed, or showing chat, or showing a tool's own sub-panel. Checking for a layout box
   * is what tells those apart — without it every later request would think the panel was
   * already open and click a card nobody can see.
   */
  static visibleCards = () =>
    [...document.querySelectorAll(MeetingToolsEventHandler.CardSelector)]
      .filter((c) => c.getClientRects().length);

  _ensureToolsPanel = async () => {
    if (MeetingToolsEventHandler.visibleCards().length) {
      return true;
    }

    // Inside a tool's sub-panel the card list is one step back, and pressing the panel
    // button there would close the whole side panel instead of returning to the list.
    const back = [...document.querySelectorAll('button, [role="button"]')]
      .find((b) => b.querySelector("i")?.textContent.trim() === "arrow_back" && b.getClientRects().length);
    if (back) {
      back.click();
      if (await MeetingToolsEventHandler.waitFor(
        () => MeetingToolsEventHandler.visibleCards().length, 1500)) {
        return true;
      }
    }

    this._togglePanel(MeetingToolsEventHandler.ToolsPanelId);
    return await MeetingToolsEventHandler.waitFor(
      () => MeetingToolsEventHandler.visibleCards().length);
  }

  _findCard = (tool) => {
    const cards = MeetingToolsEventHandler.visibleCards();

    for (const card of cards) {
      if (card.querySelector("i")?.textContent.trim() === tool.icon) {
        return card;
      }
    }

    const byJsname = document.querySelector(`[jsname="${tool.jsname}"]`)
      ?.closest(MeetingToolsEventHandler.CardSelector);
    return cards.includes(byJsname) ? byJsname : null;
  }

  /** Returns whether the tool's card was reached and pressed. */
  _openTool = async (toolName) => {
    const tool = MeetingToolsEventHandler.Tools[toolName];
    if (!tool) {
      console.error("Unknown meeting tool requested:", toolName);
      return false;
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
      return false;
    }

    if (card.querySelector('[aria-disabled="true"]')) {
      console.error(`The "${toolName}" tool is present but disabled — it likely needs a paid Workspace plan.`);
      return false;
    }

    card.querySelector('[role="button"]')?.click();
    return true;
  }

  /**
   * Presses a tool's main button — Start recording, Start transcription, Start a poll,
   * Ask a question, Set up breakout rooms, Enable translation for everyone.
   *
   * Navigating there is the bulk of the work: open the panel, open the card, wait for the
   * sub-panel to render. Transcribe is the exception, since Meet sometimes puts its button
   * straight on the card, and taking that shortcut avoids a redundant panel transition.
   *
   * What happens after the press is Meet's business. Starting a poll opens a composer;
   * starting a recording may ask for consent first. This gets you to the point where the
   * only thing left is the part that genuinely needs a human.
   */
  _startTool = async (toolName) => {
    // A dialog on screen means the previous press opened it, so this press answers it.
    // Recording and transcription both warn that doing either without everyone's consent
    // may be illegal; that warning should be dismissed by a deliberate second press, not
    // swallowed by the first — the same two-press shape Leave Call already uses.
    if (MeetingToolsEventHandler.confirmDialog()) {
      return;
    }

    const tool = MeetingToolsEventHandler.Tools[toolName];
    if (!tool) {
      console.error("Unknown meeting tool requested:", toolName);
      return;
    }

    // Speech translation is the one tool whose off switch is a different button from its
    // on switch, so the off one is preferred whenever it is on screen.
    if (tool.off && document.querySelector(tool.off)) {
      document.querySelector(tool.off).click();
      return;
    }

    if (!tool.primary) {
      console.error(`The "${toolName}" tool has no single button to press — open it instead.`);
      return;
    }

    if (!await this._ensureToolsPanel()) {
      throw new ControlsNotFoundError("The meeting tools panel did not open!");
    }

    if (tool.cardPrimary) {
      const inline = document.querySelector(tool.cardPrimary);
      if (inline) {
        inline.click();
        return;
      }
    }

    if (!document.querySelector(tool.primary)) {
      const card = this._findCard(tool);
      if (!card) {
        console.error(
          `The "${toolName}" tool is not available in this meeting. Cards currently offered:`,
          [...document.querySelectorAll(MeetingToolsEventHandler.CardSelector)]
            .map((c) => c.querySelector("i")?.textContent.trim()));
        return;
      }

      card.querySelector('[role="button"]')?.click();
      if (!await MeetingToolsEventHandler.waitFor(() => document.querySelector(tool.primary))) {
        console.error(
          `Opened "${toolName}" but its button (${tool.primary}) never appeared. ` +
          "Google may have renamed it, or the tool is already running.");
        return;
      }
    }

    document.querySelector(tool.primary).click();
  }

  /**
   * Drives the Breakout rooms editor. Everything except "set up" only exists once the
   * editor is open, so it is opened first when it is not already showing.
   */
  /**
   * Opens the Breakout rooms panel, and its editor when the action needs one.
   *
   * The two are separate places. The panel lists the rooms and offers Close rooms and Join;
   * the editor — behind the same button Meet labels "Set up breakout rooms" before there
   * are any and "Edit rooms" after — is where the room count, Shuffle, Clear, the timer and
   * Open rooms live. Pressing the editor button when you only wanted the panel would drop
   * the user into a form they did not ask for, so it is only pressed when needed.
   */
  _ensureBreakoutPanel = async (needsEditor) => {
    const inPanel = () =>
      document.querySelector('[jsname="jjbqZd"]')?.getClientRects().length ||
      document.querySelector(MeetingToolsEventHandler.BreakoutEditorSelector)?.getClientRects().length;

    if (!inPanel()) {
      if (!await this._openTool("breakoutRooms")) {
        return false;
      }
      if (!await MeetingToolsEventHandler.waitFor(inPanel)) {
        console.error("The breakout rooms panel did not open.");
        return false;
      }
    }

    if (!needsEditor) {
      return true;
    }

    if (document.querySelector(MeetingToolsEventHandler.BreakoutEditorSelector)?.getClientRects().length) {
      return true;
    }

    document.querySelector('[jsname="jjbqZd"]')?.click();
    if (!await MeetingToolsEventHandler.waitFor(
      () => document.querySelector(MeetingToolsEventHandler.BreakoutEditorSelector)?.getClientRects().length)) {
      console.error("The breakout rooms editor did not open.");
      return false;
    }

    return true;
  }

  _breakout = async (actionName, roomNumber, minutes) => {
    // Closing the rooms asks "Close all breakout rooms?" first, so a press with a dialog
    // already up is answering it — the same two-press shape recording uses.
    if (MeetingToolsEventHandler.confirmDialog()) {
      return;
    }

    if (actionName === "joinRoom") {
      return await this._breakoutJoin(roomNumber);
    }

    if (actionName === "setTimer") {
      return await this._breakoutTimer(minutes);
    }

    const action = MeetingToolsEventHandler.BreakoutActions[actionName];
    if (!action) {
      console.error("Unknown breakout action requested:", actionName);
      return;
    }

    if (!await this._ensureBreakoutPanel(action.editor)) {
      return;
    }

    const target = document.querySelector(action.selector);
    if (!target || !target.getClientRects().length) {
      console.error(
        `Breakout rooms offers no "${actionName}" right now — Open rooms and Close rooms ` +
        "each only exist in the opposite state, and Return to main call only while you are in a room.");
      return;
    }

    if (target.disabled || target.getAttribute("aria-disabled") === "true") {
      // Shuffle and Clear are greyed out until there is somebody other than you to move.
      console.error(`"${actionName}" is disabled — there may be nobody in the call to assign.`);
      return;
    }

    target.click();
  }

  /** Joins the nth room, counting the way Meet lists them, from 1. */
  _breakoutJoin = async (roomNumber) => {
    if (!await this._ensureBreakoutPanel(false)) {
      return;
    }

    const rooms = [...document.querySelectorAll(MeetingToolsEventHandler.BreakoutJoinSelector)]
      .filter((b) => b.getClientRects().length);

    if (!rooms.length) {
      console.error("No breakout rooms to join — they are not open yet.");
      return;
    }

    const index = (Number.isInteger(roomNumber) ? roomNumber : 1) - 1;
    if (index < 0 || index >= rooms.length) {
      console.error(`There is no room ${index + 1}; the call has ${rooms.length}.`);
      return;
    }

    // Joining navigates the tab to the room's own meeting, which tears this content script
    // down and loads a fresh one. The socket reconnects by itself a moment later.
    rooms[index].click();
  }

  /**
   * Sets the countdown that returns everyone to the main call, or clears it when given
   * nothing. Meet keeps this behind a dialog with a tick-box and a minutes field.
   */
  _breakoutTimer = async (minutes) => {
    if (!await this._ensureBreakoutPanel(true)) {
      return;
    }

    const button = document.querySelector(MeetingToolsEventHandler.BreakoutTimerSelector);
    if (!button) {
      console.error("No breakout timer button found.");
      return;
    }

    button.click();
    if (!await MeetingToolsEventHandler.waitFor(() => MeetingToolsEventHandler.visibleDialog())) {
      console.error("The breakout timer dialog did not open.");
      return;
    }

    const dialog = MeetingToolsEventHandler.visibleDialog();
    const tick = dialog.querySelector('input[type="checkbox"]');
    const field = dialog.querySelector('input[type="number"]');
    const wanted = Number.isInteger(minutes) && minutes > 0;

    if (tick && tick.checked !== wanted) {
      tick.click();
      await new Promise((resolve) => setTimeout(resolve, 250));
    }

    if (wanted && field) {
      MeetingToolsEventHandler.setInputValue(field, minutes);
    }

    MeetingToolsEventHandler.confirmDialog();
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
