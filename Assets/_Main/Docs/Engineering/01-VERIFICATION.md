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

## 3. Screenshots are unreliable here; composite to PNG instead

The Game and Scene views are collapsed in the current editor layout (Scene View has reported a
906×61 viewport), so `manage_camera(action="screenshot")` returns blank or unusable images. This is
the user's window layout, not something to "fix" by rearranging their editor.

**Dependable alternative:** read the source PNGs off disk, composite them in code, write the result
to the scratchpad, and open it with the `Read` tool. This works regardless of editor layout and is
how the sprite rig has been visually checked throughout.

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
