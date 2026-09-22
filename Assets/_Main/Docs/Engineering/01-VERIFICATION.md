# VERIFICATION NOTES — "Deeper"

Environment-specific gotchas for verifying work in this project. Everything here was learned the
expensive way; re-deriving it costs a lot of wasted tool calls.

---

## 1. The editor freezes the player loop when unfocused

Entering play mode via automation while the Unity window is not focused leaves the player loop
stalled — `Time.frameCount` stays at 2 and nothing ticks. `Update`, physics and animation never run,
so any check that depends on time passing silently reports nothing happening.

**Workaround:** set `Application.runInBackground = true` at the start of an automated play-mode
session. This is a runtime-only property and does not persist to `PlayerSettings`.

Symptom to recognise: a frame counter or timer that reads identically across two separate calls.

## 2. Simulated input DOES reach play mode — via a virtual gamepad

**Corrected, 2026-08-16.** This section used to say input could not be simulated at all, and that
real key input needed a human at a focused Game view. That was true as far as it was tested, and
the reason it is wrong is in the name of the setting it blamed:
`editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` gates **pointers and
keyboards**. A gamepad is neither.

Every action in `InputSystem_Actions` has a Gamepad binding — `Move` = leftStick, `Attack` =
buttonWest, `HeavyStrike` = buttonNorth, `Dash` = rightShoulder, `Ultimate` = leftShoulder — so a
virtual pad drives the whole player kit through the real input path:

```csharp
var pad = UnityEngine.InputSystem.Gamepad.current
       ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>("MCPProbePad");
var st = new UnityEngine.InputSystem.LowLevel.GamepadState();
st.leftStick = new Vector2(-1f, 0f);
st.buttons = (uint)(1 << (int)UnityEngine.InputSystem.LowLevel.GamepadButton.West);
UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, st);
```

Two gotchas, both of which produced a confident wrong reading before being understood:

**Set `backgroundBehavior = IgnoreFocus` first, or the device is reset every frame.** With the
editor unfocused and the default `ResetAndDisableNonBackgroundDevices`, the pad stays `added` and
`enabled` and still reads back zero — the state is wiped before anything can sample it. The symptom
is a probe that works once, immediately after entering play mode, and reads zero on every call
after that. In this state a dash-direction test reported "falls back to facing" for all seven cases,
which looks exactly like a real behavioural result.

**For a value (a stick), call `InputSystem.Update()` yourself. For an edge (a button press),
do NOT.** `WasPressedThisFrame` is true only during the frame the input system processed the event.
Calling `InputSystem.Update()` from `execute_code` consumes that frame inside your own call, so by
the time the game's `Update` runs, the edge is gone and the press does nothing — while
`IsPressed()`, a level read, keeps working and makes the failure look selective. Queue the event and
let the player loop flush it, then read the result on the next MCP call.

**Restore both settings when done.** `InputSystem.settings` is a project asset and changes made in
play mode persist. The defaults are
`editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` and
`backgroundBehavior = ResetAndDisableNonBackgroundDevices`.

**Use a persistent observable, not a transient one.** `execute_code` compiles a fresh assembly per
call, so nothing survives between calls — an event handler that records into a local cannot be read
later, and a 0.36s attack is over before the next round trip. Damage dealt to a parked
`TrainingDummy` is the good observable: set its `maxHealth` and `_health` high so nothing dies and
respawns mid-measurement, `Consume()` the `ComboCounter` between cases so a growing multiplier does
not contaminate the reading, and the HP delta *names the action* — 8 is a Basic, 12 a Dash Attack,
20 a tapped Heavy, 44 a fully charged one. For anything that must be caught mid-action, set
`Time.timeScale = 0.06` so the window outlives the round trip, then restore it to a fixed 1 (§8).

## 3. Screenshots DO work — but only when you name a camera

Corrected. The original claim here was that screenshots are unusable in this layout. That is only
half right, and the half that works is the more useful half.

The Game and Scene views are collapsed in the current editor layout (Scene View has reported a
906×61 viewport), so a bare `manage_camera(action="screenshot")` — which goes through the
`ScreenCapture` API and grabs the *viewport* — returns blank or unusable images. This is the user's
window layout, not something to "fix" by rearranging their editor.

**Passing `camera="Main Camera"` renders that camera directly instead of grabbing the viewport, and
it works.** This is how the sprite depth-sorting bug was found and confirmed fixed:

```
manage_camera(action="screenshot", camera="Main Camera", include_image=True, max_resolution=560)
```

Practical notes:
- **Enter play mode, freeze the subject, and frame it yourself.** Set `rb.simulated = false` so
  physics doesn't shove the player off the test spot, disable `CameraRig` so it stops following, then
  set `Camera.main.transform.position` and `orthographicSize` by hand. `orthographicSize` 1.3–3.2
  covers "one character" to "a few tiles".
- **Zoom in far enough.** A first pass at `orthographicSize 2.2` and 400px was too small to judge the
  sorting and nearly produced a wrong "no change" conclusion; at 1.3 and 560px the occlusion was
  obvious.
- **Specifying a camera excludes Screen Space - Overlay canvases**, so the HUD will not appear —
  *unless you point the canvas at your own camera for the duration of the render.* Set
  `canvas.renderMode = ScreenSpaceCamera` with `worldCamera` = a throwaway orthographic camera whose
  `targetTexture` is a 1920×1080 `RenderTexture`, call `Canvas.ForceUpdateCanvases()`, `Render()`,
  `ReadPixels`, then restore `renderMode`/`worldCamera`/`planeDistance` in a `finally`. Put the
  probe camera at `Camera.main`'s position and orthographic size and the world renders behind the
  HUD, which is the only way to judge whether the HUD reads against the actual floor. This is how
  the HUD's white weapon-slot box was found. **Rebuild afterwards** — `Deeper/Build Run HUD` is
  idempotent, so mutating fill amounts and label text to get a representative picture costs nothing:
  re-running it puts the HUD back to its pristine built state.
- Screenshots default to `Assets/Screenshots/`, which puts junk PNGs in the project. Pass
  `output_folder="Captures"` (outside `Assets/`) and delete it afterwards.

**Still useful for art, not renders:** reading the source PNGs off disk, compositing them in code,
writing to the scratchpad and opening with `Read`. That is the right tool for checking sprite
alignment, palettes and VFX scale ratios, where you want exact pixels rather than a rendered frame.

## 4. Assertions are not enough — look at the picture

Every visual defect found so far passed every assertion:

- A helmet layer covered the face pixels, making Down and Up facings indistinguishable on screen.
- Diagonal facings were drawn too close to the cardinals to tell apart while moving.
- The HUD's weapon slot drew as a **solid white box**. Every menu item logged zero warnings, every
  sprite loaded, every serialized field was wired, and the offending object was an `Image` with a
  null sprite — which UGUI draws as a white quad. Nothing is null, nothing errors, and there is no
  assertion that would have failed.

All resolved correctly, frame-locked, with no nulls and no console errors. Only compositing the
layers and actually looking at the image caught them. For anything visual, assert **and** look.

**And look at the size the user is looking at.** The whole HUD restyle was verified by rendering —
at 1920×1080, the reference resolution, where it was perfect. The owner's Game view is 906×463, where
`CanvasScaler` was applying a **0.45** factor that resampled every new detail into the flat chrome
the restyle had replaced; their verdict was "it's the same UI". Rendering proved nothing because the
one resolution I chose was the one where the bug cannot occur. For anything whose appearance depends
on resolution, **render at the size actually in use**, and for HUD work specifically check
`canvas.scaleFactor` is a whole number before drawing any conclusion about the art. Reproducing the
user's window size is one line: make the probe `RenderTexture` 906×463 instead of 1920×1080.

## 5. `execute_code` notes

- It compiles as a method body via CodeDom (C# 6). No local functions, no target-typed `new`, no
  switch expressions. Use `System.Func`/`System.Action` lambdas instead of local functions.
- **`Object` is ambiguous** between `System.Object` and `UnityEngine.Object` in this context — write
  `UnityEngine.Object.DestroyImmediate` in full. Bare `Object.DestroyImmediate` fails to compile.
- **`UnityEditor.SceneManagement` is not referenced**, so `EditorSceneManager` cannot be named
  directly — saving the open scene needs
  `System.Type.GetType("UnityEditor.SceneManagement.EditorSceneManager, UnityEditor")` and
  reflection. `UnityEngine.SceneManagement.SceneManager` resolves fine.
- Locals cannot shadow a name used as a lambda parameter anywhere in the same scope — a frequent
  compile error when building UI hierarchies.
- Some `AssetDatabase` calls are blocked by the safety checker (e.g. `DeleteAsset`). Delete through
  the filesystem and refresh, or pass `safety_checks=false` deliberately.
- Do not touch objects from `PrefabUtility.LoadPrefabContents` after `UnloadPrefabContents` — they
  are destroyed, and the exception fires *after* the useful work has already succeeded.
- A `refresh_unity` reporting `refresh_triggered: false` means new files were not picked up; use
  `scope="all", mode="force"`.

## 5b. Assigning `characterInfo` to a loaded Font does not rebuild its glyph lookup

`PixelFontArt` updates the HUD font asset in place to preserve its GUID. Setting `font.characterInfo`
writes the serialized array correctly — it reads back with all 76 entries, correct advances, correct
UVs — but Unity's **internal character map is not rebuilt**, so `GetCharacterInfo` returns `false` for
every character and `Text` lays out quads with zeroed UVs. The HUD renders completely wordless while
every value you can inspect looks right.

Fix: `AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceUpdate)` **after** `SaveAssets()`.

The trap is that this only bites on a **re-run**. The first generation calls `CreateAsset`, and
creating the asset builds the lookup, so the tool works perfectly once and then silently breaks the
font every time after. Check `font.GetCharacterInfo('A', out ci)` returns true — not just that
`font.characterInfo.Length` is non-zero, which stays correct throughout the failure.

## 6. Re-slicing sprite sheets breaks references

Reimporting a sliced sheet regenerates its sub-sprites. Any `SpriteAnimationSet` pointing at them
silently goes null. **Always rebind the animation sets after a reimport**, then verify by resolving
every state × facing × frame and asserting no nulls.

## 6b. Render shader work to a RenderTexture and LOOK at it

A shader cannot be verified by asserting on values. Three separate "fixed it" claims about the
Ultimate aura were wrong because only numbers were checked; each was settled in one look once the
result was actually rendered:

```csharp
var cam = camGo.AddComponent<Camera>();
cam.orthographic = true; cam.targetTexture = rt; cam.Render();
RenderTexture.active = rt;
tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
```

Render a **grid of parameter variants** in one pass — it costs the same as one and turns tuning
into a single look instead of a round trip per value. Set `_Speed = 0` so a still frame shows the
pattern rather than a smear. Then render through the **real component**, not a hand-built mock: the
mock looked like fire at scale 1.75 while the component was still drawing a rim at 1.18, and only
rendering the real thing exposed that.

**Editing a `.shader` file does not reimport it.** A render immediately after an edit returns the
OLD shader, pixel for pixel identical — which reads as "my change did nothing". Call
`AssetDatabase.ImportAsset` on the shader, or `refresh_unity(scope="all", mode="force")`, and
confirm by checking `GetPropertyName` for a property the edit added.

## 6c. Sprite UVs are sheet UVs, not 0-1

`IN.texcoord` in a sprite shader addresses the **whole sheet**. A sub-sprite spans only a slice of
it — `Body_Base_2_0` covers uv `(0.03, 0.70)` size `(0.19, 0.10)`. Any shader using UV as a
position within the sprite is therefore wrong: a height gradient stays nearly constant from feet to
head, and procedural noise barely varies, which draws a solid blob. Pass the sprite's rect in and
normalise:

```csharp
var tr = sprite.textureRect; var tex = sprite.texture;
mpb.SetVector("_SpriteRect", new Vector4(tr.x/tex.width, tr.y/tex.height,
                                         tr.width/tex.width, tr.height/tex.height));
```
```hlsl
float2 uv = (IN.texcoord - _SpriteRect.xy) / max(_SpriteRect.zw, 1e-5);
```

## 7. Probes that instantiate the Player MUST clean up in a `finally`

`Object.Instantiate(playerPrefab)` inside `execute_code` puts a real `Player(Clone)` in the **open
scene**. If the probe throws before its `DestroyImmediate` — and probes throw often, that is what
they are for — the clone is left behind, and it looks exactly like an engine bug:

- **Attacks slow the game down permanently.** Two `HitStop` components. The second samples
  `Time.timeScale` while the first has already frozen it, then "restores" the game to the frozen
  value. See §8.
- **The cursor misbehaves.** Two `PlayerAim` components fighting over `Cursor.visible`.
- **Input fires twice**, because both `AttackStateMachine`s read the same actions.

Restarting the editor "fixes" it only because the scene reloads from disk, which makes it look
intermittent and unrelated to any code change. Wrap every probe:

```csharp
var go = (GameObject)UnityEngine.Object.Instantiate(prefab);
try { /* probe */ }
finally { UnityEngine.Object.DestroyImmediate(go); }
```

Cheaper still: read the prefab asset with `SerializedObject` instead of instantiating. Most checks
never need a live instance.

Sweep for leftovers with a scan for root objects named `*(Clone)` before blaming anything else.

## 8. Never restore `Time.timeScale` to a sampled value

`HitStop` used to capture `Time.timeScale` when a freeze started and restore to it afterwards. That
is correct only while nothing else has frozen time. When something has — a second `HitStop`, a
pause, another effect — it captures the **frozen** value and permanently restores the game to slow
motion, surviving until the scene reloads. Restore to a fixed `normalScale` instead.

`HitStop.OnEnable` also self-heals: if `Time.timeScale` is at or below `frozenScale` with no freeze
running, it resets. That state is what a **domain reload mid-freeze** leaves behind, which is why
recompiling scripts while the editor is playing could strand the game in slow motion.

## 9. Do not recompile while the editor is in play mode

`refresh_unity` with `compile` triggers a domain reload. In play mode that destroys coroutines
mid-flight (see §8) and resets static state, producing symptoms that look like gameplay bugs.

The editor preference **Script Changes While Playing** is now set to *Recompile After Finished
Playing* (`EditorPrefs` key `ScriptCompilationDuringPlay` = 1), so this cannot happen. It is a
machine-local preference, not project data — it will need setting again on another machine.
Check `EditorApplication.isPlaying` before requesting a compile regardless.

## 10. The whole project compiles without opening the editor

A session with no Unity MCP tools can still get a **real** compile, not a structural guess: Roslyn
and every reference assembly ship inside the editor install. This caught nothing on the HUD pass,
which is the point — it turned "I read it carefully" into "72 sources, 0 errors".

- **Compiler:** `dotnet "<Unity>/Editor/Data/DotNetSdkRoslyn/csc.dll" @args.rsp`
- **Flags:** `-target:library -nostdlib+ -noconfig -langversion:9.0`, plus
  `-define:UNITY_EDITOR;<the DefineConstants string out of the csproj>`.
- **Sources:** every `.cs` under `Assets/`. This project has no assembly definitions, so runtime and
  editor code compile together in one pass — which is also what makes the check meaningful, since
  the editor tools reference the runtime namespaces directly.
- **References:** the `<HintPath>` entries from `Assembly-CSharp.csproj` *and*
  `Assembly-CSharp-Editor.csproj`, **plus every DLL in `Library/ScriptAssemblies/`**.

Three things that produce confidently wrong readings:

1. **The generated csprojs are stale.** Theirs is whatever the editor last wrote, so new files are
   missing from `<Compile Include>` (build the source list yourself) and the *package* assemblies
   can be absent from the reference list entirely. Missing `UnityEngine.UI`, `Unity.InputSystem` and
   `UnityEditor.U2D.Sprites` produced **70 errors that all looked like real code faults** — every one
   of them "type or namespace does not exist". `Library/ScriptAssemblies/` is where Unity keeps its
   own compiled copies of those.
2. **Exclude `Assembly-CSharp*.dll` from the references.** That is the project's own previous build;
   referencing it while compiling the same sources defines every type twice.
3. **Read csc's output as bytes, not text.** The diagnostics come out localised in the OS language
   (Turkish on this machine) and are not decodable as the console codepage — decode UTF-8 with
   `errors="replace"`. `-preferreduilang:en` does *not* override it. Count `": error "` lines rather
   than trying to read them, then look up the ones that matter.

### §10 addendum — the reference set is the whole difficulty (2026-08-25)

Re-run on a session where the Unity MCP dropped mid-work. The recipe above is right in outline and
lands on two traps that each produce thousands of errors that look like code faults:

1. **`CS1703: Multiple assemblies with equivalent identity`.** Taking every `<HintPath>` plus every
   DLL in `Library/ScriptAssemblies/` imports several copies of the same assembly — the editor
   install ships duplicates under `NetStandard/compat/`, `NetStandard/ref/` and the package caches.
   The compile aborts before reading a single project source. **Dedupe by file NAME, not by path**,
   preferring `Library/ScriptAssemblies/`.
2. **Then `CS0518: Predefined type 'System.Void' is not defined` × 5587.** Deduping naively drops the
   core library, and `-nostdlib+` means nothing supplies one implicitly. The fix is to force-include
   the core set and never let the dedupe shadow it:
   - `Editor/Data/NetStandard/ref/2.1.0/netstandard.dll`
   - every DLL under `Editor/Data/NetStandard/compat/2.1.0/shims/` (~120 of them)

   Do **not** substitute `MonoBleedingEdge/.../mscorlib.dll` — that is a different corlib identity
   and re-triggers trap 1.

With both handled: **103 sources, 352 references, exit 0.** Also worth correcting: the diagnostics
came out in **English** on this run, not Turkish, so grepping `error CS` works. Match on the code
rather than the word, which is language-independent either way.

**What this does not prove.** It is a compile, not a run: it confirms every API exists with the
signature being used, and nothing about whether Unity *accepts what the code produces*, and nothing
at all about what the result looks like. The HUD font generator compiled clean well before anyone
knew Unity would accept the `Font` asset it builds, and the HUD it feeds still rendered a solid
white box over the weapon slot. Compile first because it is cheap; then still run it and look.

---

## 11. Rendering the HUD, corrected for URP (2026-08-25)

§3's recipe is right about *why* a camera is needed — a camera-specified capture excludes
Screen Space - Overlay canvases, so the canvas has to be moved onto your own camera and put back in a
`finally`. Two details in it do not survive contact with this project, and each produced a **blank
image that looked exactly like a broken layout**.

1. **`Camera.Render()` does not draw the canvas under URP.** The capture came back as nothing but the
   camera's clear colour, with no warning in the console. Split the work in two: one call sets the
   canvas to `ScreenSpaceCamera`, points it at a throwaway camera whose `targetTexture` is your
   `RenderTexture`, and **returns**; a later call does `ReadPixels` and restores. Frames pass between
   two MCP calls, so the camera renders in the normal player loop, which is the path URP supports.
   Keep the camera, the texture and the saved canvas state in `static` fields between the two.

2. **`FindFirstObjectByType<Canvas>()` is not `HUDCanvas`.** The sandbox has a second canvas for the
   debug menu. Grab the wrong one and you move *it* onto your camera while `HUDCanvas` stays on
   Overlay — which the capture excludes — so you get a blank image again, this time for a completely
   different reason. **Find it by name.**

Also worth knowing: `PixelPerfectHUDScale` drives `scaleFactor` from `Screen.height`, which is the
editor's Game view and not the size you are rendering. Force the scaler to
`Mathf.Max(1, height / 540)` for the capture and let the component put its own value back afterwards.
Log the resulting `canvas.scaleFactor` in the same message as the filename — §4 exists because a
render at the one resolution where a bug cannot occur proves nothing.

## 12. `execute_code` was unusable this session — the fallback is a temporary editor script

Every call, on every compiler setting and with every leading character tried, failed with
`Compilation failed: Line 1: ﻿` — a byte-order mark reaching the CodeDom compiler. `roslyn` is not
installed, so there was no second backend to fall back to.

**The workaround that worked:** write a throwaway `Scripts/Editor/Verify*.cs` with one `[MenuItem]`
per probe step, drive it with `execute_menu_item`, read the results out of the console with
`read_console(filter_text: ...)`, and delete the file when the pass is done. It is slower per step
but it is *more* reliable for a multi-step probe, because state survives between calls and menu items
run in play mode.

Two constraints it inherits: **§9 still applies** — do not edit that script while the editor is in
play mode; stop, edit, recompile, re-enter. And every probe method that a key would normally trigger
should already be public on the real component (`TestControls.GrantLevel`, `UpgradeCard.Pick`,
`VaultReward.Grant`), which is why those are public and context-menued in the first place.

## 9. `execute_code` can fail wholesale on a BOM (2026-09-07)

In one session **every** `execute_code` call failed to compile, including `return 1 + 1;`, with the
single error `Line 1: ﻿` — the message body being a UTF-8 byte-order mark. The transport was
prepending a BOM to the submitted source and CodeDom rejected it as an invalid token before line 1.

Nothing in the code being sent causes or fixes this; a leading newline and a leading `//` comment
were both tried and both failed. **If the first `execute_code` call of a session returns that error,
the tool is unusable for the whole session** — stop trying and plan around it.

**It also survives an MCP reconnect.** The bridge dropped and came back mid-session; `return 1 + 1;`
failed identically afterwards. So it is a property of the client/transport pairing, not a transient
fault — reconnecting is not a fix and is not worth trying.

What still works, and what that costs:

- `read_console`, `execute_menu_item`, `refresh_unity`, `manage_camera`, `find_gameobjects`,
  `manage_editor` (play/stop) and the `mcpforunity://` resources are all unaffected. Reading the
  built scene back through `mcpforunity://scene/gameobject/{id}/components` is the substitute for a
  probe, and it is how `TilemapRenderer.mode` and `sortOrder` were confirmed to be set correctly
  while the floor still rendered wrong.
- What is lost is **driving anything**. With simulated key input already unusable (§2), a session
  with no `execute_code` cannot press a button, call a method, or measure damage. Verification drops
  to "does it build clean, does play mode raise errors, does it look right" — which is real, but it
  is not behavioural verification, and work finished in that state should be reported as unverified
  rather than as working.
- Put a `[ContextMenu]` on anything a probe would have called, so the owner can drive it by hand from
  the Inspector. `WeaponSelectPanel.Open`, `HubDescent.Descend` and `WeaponCard.Pick` all carry one.

## 10. A Screen Space - Overlay canvas cannot be screenshotted here — read it instead

`manage_camera(action="screenshot")` renders **through a camera**, and a camera render excludes
Screen Space - Overlay canvases by definition. Omitting the `camera` argument does not help: it falls
back to the same path and the result still says `(camera: Main Camera)`. So the run HUD, the upgrade
offer and the Hub's Shard counter are all invisible to a screenshot from here.

Two workarounds that do **not** work, both tried:

- **Switching the canvas to Screen Space - Camera.** The bridge cannot set an object-reference
  property — `worldCamera` returns *"Failed to convert value for property 'worldCamera' to type
  'Camera'"* for a plain name, and *"Property 'worldCamera' not found. Did you mean: worldCamera?"*
  for an `{instanceID, component}` shape. Value-typed properties on the same component (`renderMode`,
  `planeDistance`) set fine, so this is specifically references.
- **The RenderTexture probe §3 already describes** — point the canvas at a throwaway camera with a
  `targetTexture` and `ReadPixels` it. That is still the right technique and still the only way to
  judge how the HUD *looks*, but it is written in C# and so needs `execute_code` (§9). With that gone,
  §3's recipe is unavailable, not wrong.

**What works: read the value back off the component.** `mcpforunity://scene/gameobject/{id}/components`
reports live property values in play mode, including `Text.m_Text`, and that is usually the thing you
actually wanted to know. The Shard counter was verified this way — balance temporarily set to 1250 in
the asset, play, and the label read back `m_Text: "1,250"`, which proved the wiring, the draw-on-enable
path, the number formatting and the font's comma glyph in one call.

Two practical notes: the response is **large** (a `Text` dumps its whole vertex buffer), so page it
with `?cursor=N&pageSize=1` when you know which component you want; and **refresh Unity after editing
an asset on disk**, or you read the stale in-memory copy — the first attempt returned `"0"` for
exactly that reason and looked like a wiring failure.

## 13. Two MCP traps from the level-up beat pass (2026-09-19)

**`manage_components set_property` in play mode reports failure and applies anyway.** It returns
*"This cannot be used during play mode"*, yet the stretched `OfferReveal` timings and a camera's
`orthographicSize` both took effect on the live instance. Do not read the error as "nothing
changed" — check the value, and remember a play-mode change still reverts when play stops.

**Play mode can end underneath a probe, and menu-item probes then run in edit mode against the
saved scene.** A domain reload dropped the editor out of play mode mid-session. The next few probe
calls still "worked": `Time.unscaledTime` restarted near zero and then stopped moving, the Player map
read disabled, and a camera capture came back as the edit-mode view — while a `Play()` probe
switched a renderer on in the *scene's* Player instance. The scene was not marked dirty, so a save
would not have caught it either. Reloading the scene from disk discarded it. **Symptom: a clock that
jumps back to under a second.** Check `mcpforunity://editor/state` `is_playing`, or log
`EditorApplication.isPlaying` from the probe itself, before trusting any play-mode reading.


## 14. Check the active scene between every build menu item (2026-09-20)

Two Unity editors were connected to the MCP bridge at once — this project and an unrelated one. Two
things follow, and the second cost a real mistake.

**The bridge does not default to the project you are working in.** `mcpforunity://instances` listed
both; the session's default was the *other* one, and `mcpforunity://project/info` was answering for
it. Pin the target before touching anything:
`set_active_instance(instance="Deeper@<hash>")`. Read `mcpforunity://editor/state` afterwards and
confirm `unity.instance_id` is the project you meant.

**The open scene can change under you mid-sequence, and every builder writes to whatever is open.**
`Deeper/Build Run HUD`, `Build Upgrade Panel` and `Build Pause Menu` all resolve their canvas with
`GameObject.Find("HUDCanvas")` in the *active* scene. The editor reported `TestScene` open, those
three ran, and by the save the active scene was `HubScene` — so the run HUD, the offer panel and the
pause menu were all built into the surface camp, and `manage_scene(action="save")` wrote them there.
The save's own response is what exposed it: it names the scene it saved, and it said `HubScene`.

The fix cost nothing because `HubScene` was clean in git (`git checkout --` restored it exactly), but
the failure is silent: every builder logged success, the console had no errors, and the objects were
real — just in the wrong scene.

So: **call `manage_scene(action="get_active")` immediately before and after each builder**, and read
the scene name out of the save response rather than assuming. `isDirty` flipping to `true` on the
scene you expected is the cheap positive confirmation that the build landed where you meant.
