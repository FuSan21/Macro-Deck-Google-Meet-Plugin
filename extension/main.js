/**
 * This extension monitors your Meet call to send state updates to Macro Deck, and clicks
 * the Meet buttons for you when you press a key on your deck.
 *
 * Forked from ChrisRegado/streamdeck-googlemeet (MIT — see LICENSE.upstream). The wire
 * protocol is unchanged, so this extension also drives his Stream Deck plugin and his
 * extension also drives ours; the last two handlers are the only additions.
 *
 * Our extension is loaded only after the window is loaded as per the extension's
 * manifest.json, so we are free to initialize right away.
 */

const connectionManager = new StreamDeckConnectionMananger();

const eventHandlers = [
  new MicEventHandler(connectionManager),
  new CameraEventHandler(connectionManager),
  new LeaveCallEventHandler(connectionManager),
  new ChatEventHandler(connectionManager),
  new ParticipantsEventHandler(connectionManager),
  new PinPresentationEventHandler(connectionManager),
  new HandEventHandler(connectionManager),
  new CaptionsEventHandler(connectionManager),
  new EmojiReactEventHandler(connectionManager),
  new ZenModeEventHandler(connectionManager),
  new MeetingStateEventHandler(connectionManager),
  new PresentEventHandler(connectionManager),
  new MeetingToolsEventHandler(connectionManager),
  new HostControlsEventHandler(connectionManager),
  new TimerEventHandler(connectionManager),
];

connectionManager.initialize();
eventHandlers.forEach((handler) => connectionManager.registerEventHandler(handler));
eventHandlers.forEach((handler) => handler.initialize());
