# TEST SCENE — "Deeper"

`Assets/_Main/Scenes/TestScene.unity` (renamed from `SampleScene`, same GUID, still the only scene in
Build Settings).

---

## 1. What this scene is for

**Everything gets built and tuned here first.** New systems — enemies, hazards, rooms, upgrades,
curses, the Bow and Greatsword — land in this scene, get played until they feel right, and only then
get assembled into the real game scenes (Hub, run flow). Owner-directed, and the working order for
the whole project.

**It is a sandbox, not a level.** Nothing here is shipped content. It has no run flow, no floor
progression and no win/lose state; it exists so a single mechanic can be reached in two seconds
instead of by playing a run up to it.

### Why one scene and not one scene per mechanic

Considered and rejected, for three reasons:

1. **Rooms are prefabs, not scenes.** LEVEL_DESIGN authors rooms as hand-built prefabs pulled from a
   per-biome pool — so "test all room types" means dropping room prefabs into one scene, and a scene
   per room would fight the architecture rather than support it.
2. **Roguelike bugs live in the interactions.** An upgrade that breaks the Ultimate Gauge, a hazard
   that eats i-frames, a curse that inverts a stat — none of those reproduce in a scene that contains
   only the one system. Isolating systems hides exactly the class of bug this genre is made of.
3. **Scenes are the worst Unity asset to duplicate.** Every scene carries its own copy of the camera,
   lighting, HUD and player wiring; a fix to any of them then has to be repeated N times, and the
   copies drift silently.

**The discipline that makes it work:** everything testable must live in a **prefab or a component**,
never authored only in this scene. The scene is a mount point. If a system can only be exercised by
scene objects that exist nowhere else, the real game cannot reuse it and the sandbox has become a
dead end. Test-only *harness* code (below) is the deliberate exception, and it stays in
`Scripts/Testing/`.

Revisit this if the scene ever gets slow to load or impossible to read at a glance. The fix then is
still not one scene per mechanic — it is turning groups of it into prefabs and switching them off.

---

## 2. What is in it

| Object | Holds | Notes |
|---|---|---|
| `Main Camera` | `Camera`, `CameraRig` | Follow + look-ahead + impact shake |
| `Global Light 2D` | `Light2D` | Targets sorting layers `Default`, `Actors`, `Overlay` — an untargeted layer renders black |
| `Player` | The player rig | `Prefabs/Player.prefab`, starts at **(4.5, 8.5)** — the room's walk-in position, west of the entry band |
| `Level` | `Grid`, plus the original `Floor` / `Walls` tilemaps **switched off** | **Room prefabs mount here.** The hand-painted 28×16 tilemaps are kept but inactive; the room prefab below replaced them |
| └─ `CombatRoom_UpperCaves_01` | The first room type | `Prefabs/Rooms/CombatRoom_UpperCaves_01.prefab` — its own `Grid`, tilemaps, two doors, entry volume and encounter |
| `HUDCanvas` | `UltimateGaugeHUD` | The *shipped* HUD — gauge + combo readout |
| `EventSystem` | Input System UI module | |
| `TestHarness` | — | Everything test-only, in one group that can be deleted in one click |
| ├─ `Dummies` | `TestSpawner` | **The room starts empty** — `F4` spawns |
| ├─ `RoomControls` | `TestRoomControls` | `F12` re-arms the Combat Room |
| ├─ `CaveCrawlers` | `TestSpawner` | `F6`. Max 20 alive |
| ├─ `RockSlingers` | `TestSpawner` | `F7`. Max 10. Ring radius 6, just outside its 5.5 stop distance, so it does not walk backwards the moment it spawns |
| ├─ `TunnelBrutes` | `TestSpawner` | `F8`. Max 6 |
| ├─ `DeepWardens` | `TestSpawner` | `F9`. Max 3 — it is a rare Elite |
| ├─ `Controls` | `TestControls` | Player-side cheat keys |
| └─ `Overlay` | `Canvas`, `TestOverlay` | Legend (top-left) + live status (top-right), `sortingOrder 10` |

**The four enemy spawners are wired into `TestOverlay.spawners` explicitly.** `TestOverlay` only
falls back to `FindObjectsByType` when that array is **empty**, and the scene already wired `Dummies`
— so a new spawner left unwired works on its key but never appears in the legend or the alive count.

**`TestControls.playerStart` is wired to the room's `PlayerStart` marker**, so `F3` returns her to the
walk-in position rather than to wherever she happened to be when Play started. `F3` then `F12` re-runs
the room from the top.

---

## 3. Keys

Function keys, because every letter, digit and arrow in `InputSystem_Actions.inputactions` is already
bound to a real action. All of them are serialized fields — retune in the Inspector without a
recompile. The on-screen legend is generated from those same fields, so it cannot drift.

| Key | Does | Why it exists |
|---|---|---|
| `F1` | Fill Ultimate Gauge | The gauge takes 100 landed hits to fill; tuning the Ultimate otherwise means recharging every time |
| `F2` | Heal player to full | There is no healing and no death handling in the game yet, so a session otherwise ends parked at 0 HP |
| `F3` | Reset player to start | Contact damage and lunges push her across the room over a long session |
| `F4` | Spawn dummies | The scene starts with **none** — owner-directed. Nothing is in the room until you ask for it, so what is being tested is never mixed up with what was left lying around |
| `F5` | Clear dummies | Removes everything the spawner owns, including any dummy hand-placed under it in the scene — those are adopted at `Start`, so the count stays honest and Clear means what it says. Pooled actors are returned rather than destroyed, so Clear costs nothing and the next Spawn is free |
| `F6` | Spawn Cave Crawlers | Fast basic melee — the pressure enemy. The only one that hurts on touch |
| `F7` | Spawn Rock Slingers | Basic ranged. Kites to 5.5 and throws; safe to stand inside |
| `F8` | Spawn Tunnel Brutes | Heavy melee. Radial slam + knockback, safe to stand beside between slams |
| `F9` | Spawn Deep Wardens | The Elite — a Tunnel Brute variant, tinted violet, 100 HP |
| `F11` | Clear **all** enemies | One key for all four, not four clear keys. **Does not touch the room's enemies** — those belong to the room's own pool, not to a `TestSpawner` |
| `F10` | Hide/show the panel | For a clean look at the game without unwiring anything |
| `LShift` | **Dig-Dash** (not a cheat key) | The real bind, per GDD §Controls. Listed here because the overlay's `DASH` readout is the only way to see the cooldown and the i-frame window, both of which are otherwise invisible |
| `F12` | Re-arm the Combat Room | Releases everything the room spawned, reopens both doors, back to `Armed`. The entry volume also listens on trigger-*stay*, so pressing this while standing on the band restarts the fight under your feet; anywhere else, walk in again. **The last free function key** |

**Why one shared clear key.** Four enemies do not fit the old one-spawn-plus-one-clear-key-each
scheme: `F10` is the panel toggle, so only `F6`–`F9`, `F11` and `F12` were free — six keys for eight
slots. Sharing `F11` across all four `clearKey` fields needs no code change, because each
`TestSpawner` reads `Keyboard.current` independently and they all fire on the same press. `F5` still
clears **only** dummies.

**The function keys are now full.** `F12` went to the room. Anything added after this needs a modifier
chord, a debug menu, or a real rethink of the harness — there is no next key.

**There is deliberately no slow-motion key.** `HitStop` restores `Time.timeScale` to a fixed normal
after every landed hit, so a debug slow-mo would snap back to 1 on the next connect and read as a
broken key. Use the editor's own pause/step, or `HitStop`'s serialized fields.

---

## 4. The harness

`Assets/_Main/Scripts/Testing/` — **test-only, must never appear in a real room.**

- **`TestSpawner`** — one object per prefab, each with its own spawn/clear keys. Not one spawner with
  a list of prefabs: a sandbox is useful precisely when it can be set to "three crawlers and nothing
  else", and per-prefab objects make that a checkbox in the Hierarchy. Spawns on a golden-angle ring
  (radius 5 around room centre) unless `spawnPoints` are wired, capped at `maxAlive` so a held key
  cannot flood the room and make frame time — instead of feel — the thing being measured.
  **`spawnOnStart` is 0**: the room starts empty by owner direction. Set it for a room that comes up
  populated. Any actor hand-placed as a child of the spawner is adopted at `Start`, so it is counted
  in the readout and removed by Clear like everything else.
- **`TestControls`** — the player-side cheats above.
- **`TestRoomControls`** — re-arms the Combat Room, and supplies its `ROOM state / WAVE n/N / LEFT k`
  line to the overlay. Separate from `CombatRoom` because that is shipped content and must never read
  `Keyboard.current`; separate from `TestControls` because it is a different job.
- **`TestOverlay`** — the on-screen panel: the key legend, plus `HP / ULT % / COMBO / Dummies`. It
  exists because the game has no HP bar and no damage numbers yet, so "did that land, and for how
  much" is otherwise invisible. Refreshes on a 0.1 s unscaled interval, not per frame — this scene is
  where frame feel gets judged, so the readout must not be the thing stuttering it.

All three read keys straight off `Keyboard.current` rather than through the `.inputactions` asset, on
purpose: debug keys must not appear in the player's action map, which is shipped content and is where
the rebinding UI will read from.

Every action is also a public method with a `[ContextMenu]` entry, because simulated key presses never
reach play mode (`01-VERIFICATION.md` §2) — automated probes have to call them directly.

`Enemies/TrainingDummy.cs` is test-only in the same sense: it stands back up instead of dying so a
tuning pass never runs out of things to swing at. `Damageable` and `ContactDamage` on that prefab are
the reusable halves.

---

## 5. Adding to it

- **A new enemy** — duplicate the `Dummies` object, point `prefab` at the enemy, set `displayName`,
  give it a free **spawn** key and set `clearKey` to the shared `F11`. **You must also append it to
  `TestOverlay`'s `spawners` array** — the auto-discovery fallback only runs when that array is
  empty, and it no longer is. `F12` is the only free function key left; past that, either double up
  on one spawn key or the harness needs a real change.
  *(The earlier advice here — "a free pair of keys, `F6`/`F7` next" — is superseded: pairs ran out
  at four enemies.)*
- **A moving enemy** — mirror `Player.prefab`'s root rigidbody: Dynamic, gravity 0, Interpolate,
  **Never Sleep**, Continuous, freeze rotation Z. Never Sleep is not optional — Unity stops
  delivering `OnCollisionStay2D` to a sleeping body and contact damage silently dies with it.
  Collider must be **non-trigger**, or `AttackHitbox` filters it out and the enemy cannot be hit.
- **Anything spawned** — wire every Inspector reference on its prefab, and **never leave an optional
  reference to `RigRefs.Find`**. `TestSpawner` parents what it spawns to itself, so the search from
  `transform.root` lands on `TestHarness` and resolves against a *sibling enemy*. This is not
  theoretical: it made every Cave Crawler adopt the contact damage of whatever spawned after it. Use
  `GetComponent` for anything the prefab layout already pins to a known object.
- **Anything pooled** — reset your own state in `OnEnable`, not `Awake`. Actors come from an
  `ActorPool` now, so a reused instance never runs `Awake` a second time; release deactivates and
  get reactivates, which is what makes `OnEnable` the right hook. `Alive` counts *active* instances,
  because pooled ones are deactivated children rather than nulls.
- **A room** — drop the room prefab under `Level` and switch the current tilemaps off. Rooms are
  prefabs by design (LEVEL_DESIGN), so this is the same operation the real floor loader will do.
  *(Done once, for `CombatRoom_UpperCaves_01`. `Level/Floor` and `Level/Walls` are the switched-off
  originals.)* Three things a second room has to get right, all learned building the first:
  the room root must sit at **(0,0,0)** with cell size 1 so its own `Grid` cannot disagree with
  `Level`'s; interior cover must be **isolated convex posts** with clearance, because `EnemyChase`
  has no pathfinding and a concave pocket traps an enemy into a room that never unlocks; and spawn
  points must land **inside the aggro radius** (10–12) of whatever spawns there, or the enemy stands
  still until the player walks to it.
- **A room-scoped trigger volume** — put it on layer **8 `RoomTrigger`**, never Default. `ThrownRock`
  despawns on entering anything in its blocking mask, and that mask is Default — a Default-layer
  volume across a room silently eats every Rock Slinger projectile crossing it. Demonstrated both
  ways; see the engineering plan's *First Combat Room*.
- **Upgrades / curses** — they apply through `PlayerStats.SetSource(key, modifiers)`, so a test
  granter belongs next to `TestControls` as its own component (one job each), not as more keys bolted
  onto it.
- **A second weapon** — it is one field on `RunLoadout` on the Player. No harness change needed.

---

## 6. Verified

Built and checked in play mode (`Application.runInBackground = true` first, per `01-VERIFICATION.md`
§1; play-mode changes discarded, 0 stray objects in the saved scene, `timeScale` back at 1, console
clean — 0 errors, 0 warnings):

| Check | Result |
|---|---|
| Scene rename | GUID `8c9cfa26…` preserved, Build Settings path updated automatically |
| Room starts empty | 0 `TrainingDummy` in the saved scene, `spawnOnStart` = 0 |
| Spawner adopts actors hand-placed under it | `Alive` = 3 at `Start`, checked against the three dummies before they were removed |
| `Spawn()` | 3 → 4, spawned at (19.0, 8.0) — on the ring, clear of the player start |
| `Spawn(6)` burst | 6 alive, spread around the ring, all inside the room bounds |
| `Clear()` | 0 alive, every dummy destroyed |
| `FillUltimate()` | gauge 0 → 100, `IsFull` true |
| `HealPlayer()` | 100 → 75 after a 25 hit → back to 100 |
| `ResetPlayer()` | moved to (2, 2), returned to (14.0, 8.0) via the rigidbody |
| Legend | generated from the key fields, all 6 lines |
| Status line | `HP 100/100   ULT 100%   COMBO x0   Dummies 4`, live |
| Nothing left behind | 0 stray `(spawned)` / `(Clone)` objects in the saved scene |

**Not verified — needs your eyes:** actual key presses. Synthetic input never reaches play mode
(`01-VERIFICATION.md` §2), so F1–F10 were exercised through their public methods, not through the
keyboard.
