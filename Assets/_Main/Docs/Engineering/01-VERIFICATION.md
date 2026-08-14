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

## 2. Simulated key input does not reach play mode

`InputSystem.QueueStateEvent` + `InputSystem.Update()` called from an editor-context script writes
into the **editor's** input state buffer, not play mode's. The device stays enabled, the action stays
bound, and the value never changes.

Relaxing `InputSettings.editorInputBehaviorInPlayMode` and `backgroundBehavior` does not fix it.

**Consequence:** actual key-press-to-movement cannot be verified by automation. Verify everything
*downstream* of the input value instead — resolve the `InputAction`, confirm it is enabled and has
the expected bound controls, then drive the systems directly through their public API. Real key
input needs a human at a focused Game view.

**If you do toggle input settings to experiment, restore them.** `InputSystem.settings` is a project
asset and changes made in play mode persist. The defaults are
`editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` and
`backgroundBehavior = ResetAndDisableNonBackgroundDevices`.

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
- **Specifying a camera excludes Screen Space - Overlay canvases**, so the HUD will not appear.
- Screenshots default to `Assets/Screenshots/`, which puts junk PNGs in the project. Pass
  `output_folder="Captures"` (outside `Assets/`) and delete it afterwards.

**Still useful for art, not renders:** reading the source PNGs off disk, compositing them in code,
writing to the scratchpad and opening with `Read`. That is the right tool for checking sprite
alignment, palettes and VFX scale ratios, where you want exact pixels rather than a rendered frame.

## 4. Assertions are not enough — look at the picture

Every visual defect found so far passed every assertion:

- A helmet layer covered the face pixels, making Down and Up facings indistinguishable on screen.
- Diagonal facings were drawn too close to the cardinals to tell apart while moving.

Both resolved correctly, frame-locked, with no nulls and no console errors. Only compositing the
layers and actually looking at the image caught them. For anything visual, assert **and** look.

## 5. `execute_code` notes

- It compiles as a method body via CodeDom (C# 6). No local functions, no target-typed `new`, no
  switch expressions. Use `System.Func`/`System.Action` lambdas instead of local functions.
- Locals cannot shadow a name used as a lambda parameter anywhere in the same scope — a frequent
  compile error when building UI hierarchies.
- Some `AssetDatabase` calls are blocked by the safety checker (e.g. `DeleteAsset`). Delete through
  the filesystem and refresh, or pass `safety_checks=false` deliberately.
- Do not touch objects from `PrefabUtility.LoadPrefabContents` after `UnloadPrefabContents` — they
  are destroyed, and the exception fires *after* the useful work has already succeeded.
- A `refresh_unity` reporting `refresh_triggered: false` means new files were not picked up; use
  `scope="all", mode="force"`.

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
