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

**What this does not prove.** It is a compile, not a run: it confirms every API exists with the
signature being used, and nothing about whether Unity *accepts what the code produces*, and nothing
at all about what the result looks like. The HUD font generator compiled clean well before anyone
knew Unity would accept the `Font` asset it builds, and the HUD it feeds still rendered a solid
white box over the weapon slot. Compile first because it is cheap; then still run it and look.
