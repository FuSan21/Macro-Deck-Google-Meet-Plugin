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
| Start Meeting Tool | which tool | | ✅ |
| Breakout Rooms | shuffle, open/close, join, timer… | | ✅ |
| Timer | start, pause, stop, +1 min, alarm | | ✅ |
| Toggle Host Control | which switch | | ✅ |
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
are not toolbar buttons. Each is a card inside a side panel that has to be opened first, and
each then presents a sub-panel where the actual work happens. Two actions cover all of them:

- **Open Meeting Tool** navigates to a tool and stops there.
- **Start Meeting Tool** goes one step further and presses that tool's main button:

  | Tool | Button pressed |
  |---|---|
  | Record | Start recording |
  | Transcribe | Start transcription |
  | Polls | Start a poll |
  | Q&A | Ask a question |
  | Breakout rooms | Set up breakout rooms |
  | Speech translation | Enable translation for everyone |
  | Timer | start, or pause a running timer |

  Live streaming has no single button, so it is Open-only.

#### Recording and transcription take two presses

Both warn that doing it without everyone's consent may be illegal, and Meet will not start
until that dialog is answered. So the first press opens the panel and presses Start; the
second answers the dialog — the same shape Leave Call already uses, and the consent gate
stays a deliberate act rather than something a single key press slips past.

Stopping works identically, dialog and all. The Start/Stop control is the same button, so one
button bound to *Start Meeting Tool → Record* both starts and stops.

Meet also offers three tick-boxes on the Recording panel before a recording begins: include
captions, also start a transcript, and **also start Take Notes with Gemini, which is on by
default** — so an untouched recording produces a Gemini notes document as well as the video.
Those are settings rather than actions, so they are not bound to keys; set them once in the
panel.

### Breakout rooms

The controls sit in two places — the room list, and the editor behind "Set up"/"Edit rooms" —
and the action navigates to whichever one the chosen command needs:

| | Where | Notes |
|---|---|---|
| Shuffle | editor | assigns everyone at random; greyed out if you are alone |
| Clear | editor | empties the assignments |
| Open rooms | editor | moves everyone in |
| Close rooms | room list | asks first, so press again to confirm |
| Join room *n* | room list | the tab moves to that room's own call |
| Return to main call | inside a room | |
| Set timer | editor | minutes before everyone is returned; 0 clears it |
| Edit rooms / Cancel changes | either | |

**Shuffle** then **Open rooms** runs the whole feature from two keys. Assigning specific people
to specific rooms is drag-and-drop and stays a mouse job.

Several commands only exist in one state — Open and Close are opposites, Return only while you
are in a room — and asking for one that is not available is reported to the browser console
rather than pressed.

Joining a room **navigates the tab to that room's own meeting**, so the extension reloads and
the socket reconnects a moment later. That is Meet's design, not a fault.

#### What still needs a human

A poll needs its question and options typed. A Q&A question needs writing. Setting the main
timer's duration is two text fields.

Speech translation is a true toggle, but its off switch is a different button from its on
switch, and turning it off asks for confirmation too. It also prompts every participant to
choose their language.

**Timer** works from anywhere in the call, and does it without disturbing your side panel.

Meet drives start, pause and resume from one button, but the action keeps **Start** and
**Pause** separate: each does nothing when the timer is already in the state it would produce,
so a key labelled Start never pauses. That matters when you cannot see the panel, or when
somebody else started the timer.

Once a timer exists the controls come from the tray hidden behind the top-bar chip rather than
from the side panel, so pressing a key does not throw away whatever you had open. Starting from
nothing has to use the panel — with no timer, there is no chip to reach for. The alarm toggle
is panel-only too; the tray does not carry it.

**Start takes a duration and an alarm setting.** Type the duration into the action's own
field — `5` for five minutes, `1:30` for a minute and a half, `:45` for forty-five seconds —
and set the alarm to **On**, **Off** or **Leave as is**. One key then configures and starts
the timer, so "five minutes, silently" is a single press.

The alarm is *set*, not toggled, so a button that says silent stays silent however many times
it is pressed and whoever last touched the timer. Leave the duration empty to start at
whatever Meet is already showing.

Both only apply from a standing start. Meet fixes a timer's length once it begins and
disables the boxes, so pressing Start on a *paused* timer resumes it at what is left rather
than restarting it at a new length.

> **Meet runs its timers about 23 seconds long.** Ask for 10 seconds and it counts down from
> 33; ask for 12:30 and it starts at 12:53. This is Meet's own behaviour — its untouched
> 5:00 default starts at 5:23 with nothing written to either box — so the plugin does not
> compensate. A hidden −23s would disagree with the duration Meet itself shows you, and
> would start under-running the day Google fixes it.

Which cards a meeting offers depends on the host's Workspace plan and on whether you are the
host; a personal Google account sees only Speech translation and Timer. Asking for a tool that
is not on offer logs the miss to the browser console along with the cards that *were* there,
and presses nothing.

### Host controls

**Toggle Host Control** flips one switch in the host panel: the `Host management` master
switch, plus who may share their screen, unmute, turn on video, react, send messages or ask
questions. Most stay greyed out until `Host management` is on, so that is usually the first
to bind.

Meet has fourteen switches; these seven are the ones a host might reach for mid-call. The
others — Ask Gemini, Q&A in live stream, add-on activities, third-party capture, continuous
chat, hide-until-approved, anonymous questions — are set once before a webinar starts, and
listing them would only bury the seven that matter under pressure.

Only the host has this panel. For anyone else Meet does not render it, and the extension says
so rather than pressing something.

This is the one part of the plugin that is **English-only**. Every switch carries the same
automation attribute and none has an icon, so the visible label is all that distinguishes
them — and Meet translates it. Matching by position instead would silently start flipping the
wrong setting the day Google inserts a fifteenth switch, which is a worse failure than a miss.

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
