/**
 * Meet's countdown timer, which lives two levels deep: the tools panel, then the Timer
 * card's own sub-panel.
 *
 * ADDED BY THIS FORK.
 *
 * Every action navigates there first if it needs to, so a deck key works from anywhere
 * in the call rather than only when the right panel already happens to be open. The
 * navigation is skipped entirely when the controls are already present, so repeatedly
 * pressing start/pause does not keep reopening panels.
 *
 * Selectors read off a live call in August 2026. Unlike the tool cards these have no
 * icon to match on — they are plain buttons whose only stable handle is the jsname — so
 * this is the one handler here that will break silently if Google reshuffles its
 * minifier output. It fails by finding nothing and logging, never by clicking something
 * else.
 */
class TimerEventHandler extends SDEventHandler {

  /** Start, or pause a running timer. Meet uses one button for both. */
  static StartPauseSelector = '[jsname="SPCnpb"]';

  static CancelSelector = '[jsname="Fq2ped"]';

  static AddMinuteSelector = '[jsname="xLroh"]';

  /** Whether the timer makes a noise when it runs out. Carries aria-pressed. */
  static AlarmSelector = '[jsname="EAB7Kc"]';

  handleStreamDeckEvent = (message) => {
    switch (message.event) {
      case "timerStartPause":
        this._press(TimerEventHandler.StartPauseSelector, "start/pause");
        break;
      case "timerCancel":
        this._press(TimerEventHandler.CancelSelector, "cancel");
        break;
      case "timerAddMinute":
        this._press(TimerEventHandler.AddMinuteSelector, "add a minute");
        break;
      case "timerToggleAlarm":
        this._press(TimerEventHandler.AlarmSelector, "toggle the alarm");
        break;
    }
  }

  /**
   * The timer sub-panel is showing when its start/pause button exists. Getting there is
   * two clicks with a render wait after each, which is why this is worth doing once and
   * then short-circuiting.
   */
  _ensureTimerPanel = async () => {
    if (document.querySelector(TimerEventHandler.StartPauseSelector)) {
      return true;
    }

    const tools = new MeetingToolsEventHandler(this._connectionManager);
    if (!await tools._ensureToolsPanel()) {
      return false;
    }

    const card = tools._findCard(MeetingToolsEventHandler.Tools.timer);
    if (!card) {
      return false;
    }

    card.querySelector('[role="button"]')?.click();
    return await MeetingToolsEventHandler.waitFor(
      () => document.querySelector(TimerEventHandler.StartPauseSelector));
  }

  _press = async (selector, description) => {
    if (!await this._ensureTimerPanel()) {
      console.error(`Could not open Meet's timer panel to ${description}.`);
      return;
    }

    const button = document.querySelector(selector);
    if (!button) {
      console.error(`No timer button found to ${description} (selector ${selector}).`);
      return;
    }

    button.click();
  }

}
