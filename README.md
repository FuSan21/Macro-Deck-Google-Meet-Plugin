# Macro Deck Google Meet Plugin

Control Google Meet from Macro Deck — microphone, camera, hand, captions, chat, participants,
reactions, presenting and leave — with **live state** on your buttons.

Your buttons reflect what Meet is actually doing, including changes you make in the Meet tab
itself. The tab is never focused and never brought to the front, so this works while Meet sits
in a background window.

Meet is a web page, so this plugin has two halves:

```
[Macro Deck plugin]  ws://127.0.0.1:2394  <——  [content script on meet.google.com]
   listens                                        reads the DOM, clicks the real buttons
```

The browser dials the desktop, not the other way round. That is what avoids a native-messaging
host, a registry manifest, a second executable and a service worker to keep alive.

## Requirements

- Macro Deck 2.15.0 or newer
- Chrome, Edge or another Chromium browser
- The browser extension in [`extension/`](extension), loaded once

## Installing the extension

The extension travels with the plugin, so the copy you load is always the one matching the
installed version.

1. Macro Deck → the Google Meet icon in the toolbar → **Open extension folder**
2. Chrome → `chrome://extensions` → turn on **Developer mode** → **Load unpacked**
3. Pick the folder that opened in step 1
4. Open or reload a Meet tab

The plugin's status icon turns from "waiting for the browser extension" to a tab count once it
connects. The extension retries every 2 seconds, so the order you start things in does not
matter.

### Using ChrisRegado's extension instead

This extension is a fork of [ChrisRegado/streamdeck-googlemeet][upstream] and the wire protocol
is unchanged, so his published Chrome Web Store extension drives this plugin too. Everything
works except the two events this fork adds:

| | This fork | Official extension |
|---|---|---|
| Mic, camera, hand, captions, chat, participants, pin, zen, reactions, leave | ✅ | ✅ |
| `meet_in_meeting` means "in a call" | ✅ | ❌ means "a Meet tab is connected" |
| Toggle Presenting / `meet_is_presenting` | ✅ | ❌ |
| Shared `meeting_*` variables | ✅ | ❌ not claimed |

His extension cannot run alongside his own Stream Deck plugin, because both want port 2394.

[upstream]: https://github.com/ChrisRegado/streamdeck-googlemeet

## Actions

Actions that carry a bindable variable offer it when you add them: Macro Deck asks whether to
bind it to the button's state, so the key lights up to match Meet without any extra setup.

| Action | Configurable | Bindable variable | Needs this fork |
|---|---|---|---|
| Toggle Microphone | | `meet_is_muted` | |
| Mute Microphone | | `meet_is_muted` | |
| Unmute Microphone | | `meet_is_muted` | |
| Toggle Camera | | `meet_is_video_on` | |
| Turn Camera On | | `meet_is_video_on` | |
| Turn Camera Off | | `meet_is_video_on` | |
| Toggle Raised Hand | | `meet_is_hand_raised` | |
| Toggle Captions | | `meet_are_captions_on` | |
| Toggle Chat | | | |
| Toggle Participants | | | |
| Toggle Pinned Presentation | | `meet_is_presentation_pinned` | |
| Toggle Zen Mode | | | |
| Send Reaction | which emoji | | |
| Toggle Presenting | | `meet_is_presenting` | ✅ |
| Open Meeting Tool | which tool | | ✅ |
| Toggle Transcription | | | ✅ |
| Timer | start/pause, cancel, +1 min | | ✅ |
| Toggle Meeting Tools | | | ✅ |
| Toggle Host Controls | | | ✅ |
| Toggle Meeting Details | | | ✅ |
| Leave Call | | `meet_in_meeting` | |
| Open Google Meet | | | no extension at all |

**Send Reaction** covers Meet's nine: Heart, Thumbs up, Celebrate, Clap, Joy, Surprised, Sad,
Thinking, Thumbs down. If the palette is closed the extension opens it, presses the emoji and
lets Meet close it again.

**Leave Call** needs a second press when Meet asks whether to leave or end the call for
everyone — the first press opens that dialog, the second answers "just leave".

**Toggle Presenting** can only *open* Chrome's screen picker, never choose for you: a page is
not allowed to hand itself a screen. Stopping a share needs no confirmation and completes on
the single press.

**Toggle Zen Mode** hides Meet's toolbars by setting `display: none` on them. It is not a Meet
feature, and it is [borrowed from google-meet-true-full-screen][zen] via upstream.

**Open Google Meet** opens the landing page in your default browser and is the one action that
does not need the extension — there is no call to talk to yet, which is rather the point.

### Meeting tools

Record, Transcribe, Polls, Q&A, Breakout rooms, Speech translation, Timer and Live streaming
are not toolbar buttons. They are cards inside a side panel that has to be opened first, and
most of them then present a sub-panel where the actual work happens. So:

- **Open Meeting Tool** gets you *to* any of the eight in one press. It does not stand in for
  the tool — Polls still needs you to write the poll.
- **Toggle Transcription** is a real one-press start/stop, because Transcribe is the one tool
  whose control sits on the card itself rather than behind a sub-panel.
- **Timer** reaches two levels deep on its own, so start/pause, cancel and +1 minute work from
  anywhere in the call. Setting the duration is still typing — that is two text fields, not a
  button.

Which cards a meeting offers depends on the host's Workspace plan and on whether you are the
host; a personal account sees only Speech translation and Timer. Asking for a tool that is not
on offer logs the miss to the browser console along with the cards that *were* there, and
presses nothing.

Recording has no single-press action. Meet puts Record's start behind its sub-panel with a
confirmation, so **Open Meeting Tool → Record** is as far as one key goes.

[zen]: https://github.com/verlok/google-meet-true-full-screen

## Variables

`meet_connected`, `meet_in_meeting`, `meet_is_muted`, `meet_is_video_on`, `meet_is_hand_raised`,
`meet_are_captions_on`, `meet_is_presentation_pinned`, `meet_is_presenting`

`meet_connected` is true while at least one Meet tab has a socket open — that is, while buttons
will actually do something.

These are pushed, not polled: the extension sends one event per control the moment it changes,
and re-sends all of them whenever a tab connects. When the last tab disconnects everything is
cleared rather than left at its last value, because at that point every flag is a guess.

### Shared meeting variables

While you are in a call the plugin also writes a platform-neutral set, so one button layout can
serve Meet, Teams and Zoom:

`meeting_platform` (`meet`), `meeting_in_meeting`, `meeting_is_muted`, `meeting_camera_on`

These are only claimed when the call is a fact rather than an inference, which means **only with
this fork's extension**. With the official one an idle Meet tab is indistinguishable from a live
call, and letting a background tab evict an in-progress Teams meeting from the shared variables
would be worse than not taking part. They are also only cleared if Meet is the platform
currently holding them.

## Events

**Google Meet state changed** — fires when a tracked field changes, with the field name as the
event parameter (`connected`, `in_meeting`, `is_muted`, `is_video_on`, `is_hand_raised`,
`are_captions_on`, `is_presentation_pinned`, `is_presenting`).

## Turning it off

The configuration has an **Enable Google Meet integration** switch. Turning it off closes the
socket and clears the variables. The toolbar icon is full colour while enabled and grey while
off.

The port is configurable there too, for the case where something else on the machine already
owns 2394 — most likely ChrisRegado's own Stream Deck plugin. Change it in
`extension/stream_deck_connection_manager.js` to match, then reload the extension.

## How it works

### The server

The plugin listens on `127.0.0.1` and speaks the WebSocket handshake by hand: a
`TcpListener`, one header parse, one SHA-1, then `WebSocket.CreateFromStream`. The obvious
alternative, `HttpListener`, routes through http.sys, which reserves URL prefixes machine-wide —
binding one needs either an elevated process or a `netsh http add urlacl` run once as
administrator. Neither is acceptable for a plugin, and the handshake is thirty lines.

Connections are refused unless the `Origin` header is `https://meet.google.com` or an extension
origin. The browser sets that header itself and a page cannot forge it, so this is what stops
any website you happen to visit from opening a socket to this port and hanging up your calls.

Several Meet tabs can be open at once and each runs its own copy of the content script, so the
server is a broadcast bus: commands go to every tab, any tab may report state.

### Knowing you are in a call

None of upstream's state events can tell you this. The microphone and camera buttons exist on
the green room / "Ask to join" screen exactly as they do in a call, so their presence proves
only that a Meet page is loaded. The one control that appears when the call starts and
disappears the moment it ends is the leave button, so its presence is the signal — the same
selector upstream already clicks to leave, which is what keeps the two in step.

### Finding controls

Meet's DOM offers three ways to identify a button, and they are not equally good:

- **`jsname` attributes** are minifier output. Meet has changed the microphone's twice, which is
  why upstream's mic handler carries three selectors. Fine, but expect to fix them.
- **`aria-label`s** are real UI copy and are translated, so matching them works in English and
  silently fails everywhere else. Avoided.
- **Material Symbols ligatures** — the text inside `<i class="google-material-icons">` — are
  icon *names*. They look like English but they are identifiers in Google's icon font, not
  strings shown to the user, so they do not translate.

Most handlers are upstream's and use `jsname`. Presenting uses the ligature, as upstream's pin
handler already does.

### On "muted"

Upstream models every toggle as a mute, so on the wire `muted: true` means the microphone is
silenced, the camera is off, the hand is down, captions are off and the presentation is
unpinned. The plugin flips the four whose natural reading is the other way round before
publishing variables, so `meet_is_video_on` means what it says.

## When Meet changes its icons

Presenting is matched on the ligature `computer_arrow_up` for start and
`cancel_presentation` / `stop_screen_share` for stop, confirmed against Meet in August 2026.
If Google renames one, the handler finds no button: nothing is clicked and
`meet_is_presenting` stops moving, rather than something wrong being pressed.

To see what is actually on the page, run this in the console of a Meet call — it lists every
ligature with the button it belongs to and whether that button is visible:

```js
PresentEventHandler.diagnose()
```

Add the new name to `StartIcons` or `StopIcons` in
`extension/event_handlers/present_event_handler.js`, then reload the extension.

## Credits

The browser extension is a fork of [ChrisRegado/streamdeck-googlemeet][upstream], MIT licensed —
see [`extension/LICENSE.upstream`](extension/LICENSE.upstream). All the Meet selectors and the
protocol are his work. This fork changes the manifest branding and adds two handlers:
`meeting_state_event_handler.js` and `present_event_handler.js`.

## Licence

MIT — see [LICENSE](LICENSE).
