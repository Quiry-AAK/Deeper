using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Deeper.Rooms;
using Deeper.UI;

namespace Deeper.Run
{
    /// <summary>
    /// Runs a descent: draws rooms from the bag, mounts each one east of the last, dresses it, gives
    /// it a fight, and moves on when it is cleared — through sixteen floors and three biomes, to a
    /// boss that ends it.
    ///
    /// Named for what it does rather than `RoomManager`, which the engineering plan's file list
    /// proposed — CLAUDE.md bans `Manager`, and that same list also named `SecretVault.cs` and
    /// `Inventory.cs`, neither of which turned out to deserve existing.
    ///
    /// **Rooms are placed physically adjacent, never teleported between.** That single decision is
    /// why this class stays small: the next room's own <see cref="RoomEntry"/> band is already the
    /// arrival trigger, so there is no exit volume and no arrival marker to build; and the
    /// engineering plan's "load and spring must not share a frame" trap cannot fire, because she has
    /// to walk the length of a room to reach the next band.
    ///
    /// **Every design rule about ordering lives in <see cref="RoleFor"/>**, in one readable method,
    /// which is where `RoomBag`'s own comment says they belong. What used to sit inline as
    /// `if (isLastOfFloor &amp;&amp; _floor % 5 == 0)` is now one case among several, and a room type
    /// still to come is a value on <see cref="RoomRole"/>, an entry on the biome's pool, and a line
    /// there.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloorLoader : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("The shape of the whole descent: which biome each floor draws from, and which " +
                 "floor is the last. Replaced a single biome pool, which could not express a run " +
                 "that changes biome or ends.")]
        [SerializeField] private RunPlan runPlan;

        [Header("Mount points — wired in the scene")]
        [Tooltip("Rooms live under here once they are configured and active.")]
        [SerializeField] private Transform liveRoot;

        [Tooltip("MUST be an INACTIVE object. Rooms are instantiated under it so Unity defers their " +
                 "Awake, which is the only window in which an encounter can be swapped: WaveSpawner " +
                 "builds its pools in Awake exactly once and ActorPool has no dispose.")]
        [SerializeField] private Transform stagingRoot;

        [Tooltip("The run's randomness.")]
        [SerializeField] private RunSeed seed;

        [Header("Presentation — optional")]
        [Tooltip("Tinted per biome when the theme changes. Not per room: two rooms are alive at " +
                 "once and share a wall, so per-room colours would visibly disagree across it.")]
        [SerializeField] private Light2D ambient;

        [Tooltip("The HUD's depth readout, if the scene has one.")]
        [SerializeField] private DepthIndicatorHUD depth;

        [Tooltip("Snapped onto the player the moment she is placed in the run's first room. " +
                 "Without it the rig spends the opening seconds smoothing in from wherever the " +
                 "scene's Player prefab happens to sit.")]
        [SerializeField] private Deeper.CameraControl.CameraRig cameraRig;

        [Header("Player")]
        [SerializeField] private string playerTag = "Player";

        /// <summary>Raised with the new 1-based floor number.</summary>
        public event Action<int> FloorChanged;

        /// <summary>Raised for every room as it becomes active, already dressed and populated.</summary>
        public event Action<CombatRoom> RoomMounted;

        /// <summary>
        /// Raised once, when the last room of the last floor is cleared — GDD §Game Loop 7's "defeat
        /// the boss and escape (win)". The other way a run ends is death, which this class knows
        /// nothing about; <see cref="RunEnd"/> subscribes to both and is the only thing that does.
        /// </summary>
        public event Action RunCompleted;

        private RoomBag _bag;
        private RoomTheme _theme;
        private BiomeRoomPool _pool;

        private CombatRoom _current;
        private RoomConnection _currentConnection;
        private RoomRole _currentRole;
        private CombatRoom _previous;
        private bool _currentHasRoomBehind;
        private bool _finished;

        private int _floor;
        private int _roomIndex;
        private int _roomsThisFloor;

        private System.Random _fallbackRandom;

        public int Floor { get { return _floor; } }
        public int RoomIndex { get { return _roomIndex; } }
        public int RoomsThisFloor { get { return _roomsThisFloor; } }
        public CombatRoom Current { get { return _current; } }

        /// <summary>The biome the current floor is drawn from. Null before a run starts.</summary>
        public BiomeRoomPool Pool { get { return _pool; } }

        /// <summary>What slot the room she is standing in fills.</summary>
        public RoomRole CurrentRole { get { return _currentRole; } }

        /// <summary>The floor a run ends on, read off the plan.</summary>
        public int FinalFloor { get { return runPlan != null ? runPlan.FinalFloor : 0; } }

        /// <summary>
        /// Start rather than Awake, for the reason `TestRoomSelector` gives in its own comment: a
        /// room's Start calls Arm(), which reaches into a spawner whose pools are built in *its*
        /// Awake, so mounting must happen after every Awake in the scene has run.
        /// </summary>
        private void Start()
        {
            if (stagingRoot != null && stagingRoot.gameObject.activeSelf)
            {
                Debug.LogError("stagingRoot is active. Rooms staged under an active object run " +
                               "their Awake immediately, so their encounter can no longer be " +
                               "swapped. Deactivate it.", this);
            }

            BeginRun();
        }

        [ContextMenu("Begin Run")]
        public void BeginRun()
        {
            Clear();

            if (runPlan == null) { Debug.LogError("FloorLoader has no RunPlan.", this); return; }
            if (seed != null) seed.NewRun();

            _bag = new RoomBag(Random);

            _floor = 1;
            StartFloor();

            if (_pool == null)
            {
                Debug.LogError("RunPlan has no biome pool for floor 1, so there is nothing to " +
                               "descend into.", this);
                return;
            }

            CombatRoom first = MountNextRoom(null);
            if (first == null) return;

            PlacePlayerAt(first);
        }

        /// <summary>
        /// Mounts the next room without waiting for a clear. A probe seam — simulated key presses
        /// never reach play mode (Engineering/01-VERIFICATION.md §2), so a sixteen-floor run cannot
        /// be walked end to end by hand for verification.
        /// </summary>
        [ContextMenu("Mount Next")]
        public void MountNext()
        {
            if (_finished) return;

            AdvanceFrom(_current);
        }

        private System.Random Random
        {
            get
            {
                if (seed != null) return seed.Selection;

                // Cached rather than `new System.Random()` per call. Two instances constructed in
                // the same tick share a time-derived seed and return the same sequence, so an
                // unwired seed would not be merely unseeded — it would draw the same room twice.
                if (_fallbackRandom == null) _fallbackRandom = new System.Random();
                return _fallbackRandom;
            }
        }

        /// <summary>
        /// The design's own rules about which room goes where, in one place and in reading order.
        ///
        /// GDD §Game Loop 3 and 6: Biome 1 is floors 1–5, a Mini-Boss ends every 5th floor
        /// (LEVEL_DESIGN §6), and Floor 16 is two fights — the Depth Warden, then Zyno — in that
        /// order. Anything else is an ordinary Combat Room.
        ///
        /// Deliberately not data. A `RoomRole[]` authored per floor would be sixteen arrays saying
        /// "Combat, Combat, Combat, Combat" and one saying something interesting, and the rule
        /// "every fifth floor" would stop being visible anywhere.
        /// </summary>
        private RoomRole RoleFor(int floor, int roomIndex, bool isLast)
        {
            int final = FinalFloor;

            if (final > 0 && floor >= final)
            {
                return roomIndex == 0 ? RoomRole.FinalBoss : RoomRole.TrueFinalBoss;
            }

            if (isLast && floor % 5 == 0) return RoomRole.MiniBoss;

            return RoomRole.Combat;
        }

        /// <summary>
        /// How many rooms this floor holds. LEVEL_DESIGN §5 says 3–5, drawn per floor — except the
        /// last, which is exactly the two fights Floor 16 is specified as.
        /// </summary>
        private int RoomsForFloor(int floor)
        {
            int final = FinalFloor;
            if (final > 0 && floor >= final) return 2;

            return Random.Next(_pool.MinRoomsPerFloor, _pool.MaxRoomsPerFloor + 1);
        }

        private void StartFloor()
        {
            BiomeRoomPool next = runPlan != null ? runPlan.PoolFor(_floor) : null;

            // Refill and re-theme only when the biome actually changes. Refilling every floor would
            // reset the bag mid-biome, which is the one thing a reshuffling bag exists to prevent —
            // CORE_SYSTEMS §8's "drawn through without immediate repeats" only holds if the draw
            // survives a floor boundary.
            if (next != _pool)
            {
                _pool = next;
                if (_pool != null)
                {
                    _bag.Fill(_pool.Options);
                    PickTheme();
                }
            }

            if (_pool == null) return;

            _roomIndex = 0;
            _roomsThisFloor = RoomsForFloor(_floor);

            if (depth != null) depth.SetFloor(_floor);
            if (FloorChanged != null) FloorChanged(_floor);
        }

        private void PickTheme()
        {
            RoomTheme[] themes = _pool != null ? _pool.Themes : null;
            if (themes == null || themes.Length == 0) return;

            _theme = themes[Random.Next(themes.Length)];
            if (ambient != null && _theme != null) ambient.color = _theme.AmbientLight;
        }

        /// <summary>
        /// Instantiates, configures and activates one room, butted against <paramref name="behind"/>.
        ///
        /// The order matters and is the whole reason this is one method: everything is configured
        /// while the instance is still under the inactive staging root, and only the last step makes
        /// it live. Reparenting is what runs Awake, BuildPools and Arm, in that order.
        /// </summary>
        private CombatRoom MountNextRoom(RoomConnection behind)
        {
            bool isLastOfFloor = _roomIndex + 1 >= _roomsThisFloor;
            RoomRole role = RoleFor(_floor, _roomIndex, isLastOfFloor);
            RoomOption option = role != RoomRole.Combat ? _pool.RoomFor(role) : null;

            if (option == null && role != RoomRole.Combat)
            {
                Debug.LogWarning("Floor " + _floor + " wants a " + role + " room and this biome " +
                                 "authors none, so an ordinary Combat Room is drawn instead. The " +
                                 "seam is BiomeRoomPool.specialRooms.", this);
            }

            if (option == null)
            {
                // Always needsExit, and worth stating rather than leaving as a puzzle: in an
                // eastward corridor a floor does not end anywhere, it runs straight on into the next
                // one, so every drawn room is walked out of and a dead end can never be drawn. That
                // is why the Secret Vault — authored with a single doorway — is not in the floor
                // pool at all. If it is ever put on the route it needs an east door, which is a
                // layout change and a design question (CORE_SYSTEMS §8 calls a Secret Floor a
                // *detour*, and a detour needs the branch LEVEL_DESIGN §1 rules out).
                //
                // A boss arena reached through the fallback above comes through here too, which is
                // why the two Floor 16 arenas are the only rooms whose role still decides the run's
                // end: the role is kept below even when the prefab was not the authored one, or an
                // unbuilt Zyno would leave a run with no way to finish.
                option = _bag.Draw(true);
                if (option == null) return null;
            }

            GameObject instance = Instantiate(option.prefab, stagingRoot);
            instance.name = option.prefab.name;   // strip "(Clone)": a stray one is the leak tell

            RoomConnection connection = instance.GetComponent<RoomConnection>();
            if (connection == null)
            {
                Debug.LogError(instance.name + " has no RoomConnection, so it cannot be placed.", this);
                Destroy(instance);
                return null;
            }

            RoomDressing dressing = instance.GetComponent<RoomDressing>();
            if (dressing != null && _theme != null)
            {
                // Its own stream, seeded from one draw of the run's: changing how much a room
                // scatters must never be able to reshuffle the floor order.
                dressing.Dress(_theme, new System.Random(Random.Next()));
            }

            if (option.encounters != null && option.encounters.Length > 0)
            {
                WaveSpawner spawner = instance.GetComponentInChildren<WaveSpawner>(true);
                if (spawner != null)
                {
                    spawner.SetEncounter(option.encounters[Random.Next(option.encounters.Length)]);
                }
            }

            instance.transform.position = PositionFor(connection, behind);

            CombatRoom room = instance.GetComponent<CombatRoom>();
            if (room == null)
            {
                Debug.LogError(instance.name + " has no CombatRoom.", this);
                Destroy(instance);
                return null;
            }

            _currentHasRoomBehind = behind != null;
            room.StateChanged += HandleStateChanged;
            room.Cleared += HandleCleared;

            instance.transform.SetParent(liveRoot != null ? liveRoot : transform, true);
            instance.SetActive(true);

            _current = room;
            _currentConnection = connection;
            _currentRole = role;
            _roomIndex++;

            if (RoomMounted != null) RoomMounted(room);
            return room;
        }

        /// <summary>
        /// Expressed in door positions rather than room widths, so two rooms of different heights
        /// still line their doorways up with no special case — the 20x12 Wave Room, whose west door
        /// sits a tile higher than a 16x10 Combat Room's, comes out shifted down by exactly that.
        /// </summary>
        private static Vector3 PositionFor(RoomConnection room, RoomConnection behind)
        {
            if (behind == null || behind.EastAnchor == null || room.WestAnchor == null)
            {
                return Vector3.zero;
            }

            return behind.EastAnchor.position + Vector3.right - room.WestAnchor.localPosition;
        }

        private void HandleStateChanged(RoomState state)
        {
            // Arm() opens every door in the room's list and *then* raises this, so a close here
            // sticks. Only the run's first room needs it: every other room has one behind it that
            // she still has to be able to walk in from.
            if (state == RoomState.Armed && !_currentHasRoomBehind) SealWayBack();
        }

        private void HandleCleared()
        {
            // The clear opens every door again, and this fires after that. StateChanged(Cleared)
            // fires *before* it, which is why both hooks are subscribed rather than only the one.
            SealWayBack();

            if (_currentRole == RoomRole.TrueFinalBoss)
            {
                CompleteRun();
                return;
            }

            AdvanceFrom(_current);
        }

        /// <summary>
        /// The run is won. The room stays mounted and sealed — she is standing in Zyno's arena and
        /// the summary opens over it, which is a far better last frame than being dropped into a
        /// corridor with nothing in it.
        /// </summary>
        private void CompleteRun()
        {
            if (_finished) return;
            _finished = true;

            if (_current != null)
            {
                _current.StateChanged -= HandleStateChanged;
                _current.Cleared -= HandleCleared;
            }

            Debug.Log("Run complete: floor " + _floor + " of " + FinalFloor + ".", this);

            if (RunCompleted != null) RunCompleted();
        }

        private void SealWayBack()
        {
            if (_currentConnection != null && _currentConnection.West != null)
            {
                _currentConnection.West.Close();
            }
        }

        private void AdvanceFrom(CombatRoom cleared)
        {
            if (cleared == null || _finished) return;

            cleared.StateChanged -= HandleStateChanged;
            cleared.Cleared -= HandleCleared;

            RoomConnection behind = _currentConnection;

            if (_roomIndex >= _roomsThisFloor)
            {
                _floor++;
                StartFloor();
            }

            // Destroy the room *before* the one she is standing in. `cleared` is under her feet:
            // she cannot have cleared the next room without walking into it first.
            if (_previous != null) Destroy(_previous.gameObject);
            _previous = cleared;

            MountNextRoom(behind);
        }

        private void PlacePlayerAt(CombatRoom room)
        {
            RoomConnection connection = room.GetComponent<RoomConnection>();
            if (connection == null || connection.Arrival == null) return;

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) { Debug.LogError("No GameObject tagged " + playerTag + ".", this); return; }

            // Through the body, not the transform: moving a transform out from under a Rigidbody2D
            // leaves the physics position behind. Same as TestRoomSelector.
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = connection.Arrival.position;
            else player.transform.position = connection.Arrival.position;

            // After the move, not before. CameraRig positions itself in its own Awake, which runs
            // before this — so without the snap the run opens on the camera sliding in from wherever
            // the Player prefab was authored, for as long as its follow smoothing takes.
            if (cameraRig != null) cameraRig.SnapToTarget();
        }

        private void Clear()
        {
            if (_current != null)
            {
                _current.StateChanged -= HandleStateChanged;
                _current.Cleared -= HandleCleared;
            }

            if (liveRoot != null)
            {
                for (int i = liveRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(liveRoot.GetChild(i).gameObject);
                }
            }

            _current = null;
            _currentConnection = null;
            _currentRole = RoomRole.Combat;
            _previous = null;
            _pool = null;
            _theme = null;
            _finished = false;
            _floor = 0;
            _roomIndex = 0;
            _roomsThisFloor = 0;
        }
    }
}
