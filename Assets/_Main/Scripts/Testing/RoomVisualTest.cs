using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Deeper.Rooms;

namespace Deeper.Testing
{
    /// <summary>
    /// A sandbox for looking at rooms, isometric. Press the button and a different room appears: a
    /// different hand-authored layout, a different floor, a different wall, a different scatter of
    /// props.
    ///
    /// There is no player and no fight here, by owner direction — this scene answers "does the room
    /// look right", and a character walking around is not part of that question. The camera is
    /// parked at gameplay distance rather than framing the whole layout, because 28x16 diamonds is
    /// an enormous area in isometric and seeing all of it turns the room into a floor plan.
    ///
    /// **Depth needs no special case.** In an isometric grid a cell further back has a larger
    /// (x + y) and therefore a higher world Y, so sorting on world Y — exactly what
    /// <c>YDepthSort</c> already does for actors — is isometric depth sorting.
    ///
    /// Test-only, like everything in this folder. It reads the same `RoomLayoutAsset`s and the same
    /// art the game will, so what is judged here is what ships.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomVisualTest : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Hand-authored layouts, generated from Layout_*.cs by Deeper/Generate Room Layout Assets.")]
        [SerializeField] private RoomLayoutAsset[] layouts;

        [Tooltip("Isometric floor tiles. One is drawn per room; several is what stops every room " +
                 "looking like the last one.")]
        [SerializeField] private TileBase[] floorTiles;

        [Tooltip("Isometric wall blocks, drawn as sprites so they can sort against anything standing " +
                 "in front of them.")]
        [SerializeField] private Sprite[] wallBlocks;

        [Tooltip("Standing objects scattered on open floor.")]
        [SerializeField] private Sprite[] props;

        [Header("Scene — wired by the builder")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap floor;
        [SerializeField] private Transform scenery;
        [SerializeField] private Camera view;

        [Header("Tuning")]
        [Tooltip("Fraction of open floor cells that get a prop.")]
        [SerializeField, Range(0f, 0.3f)] private float propDensity = 0.06f;

        [Tooltip("Props are authored at 32px against a 64px tile, so they need doubling to sit at " +
                 "the right scale. A whole number, because anything else resamples pixel art.")]
        [SerializeField] private int propScale = 2;

        [Tooltip("Half the visible height in world units. Gameplay distance, not a floor plan.")]
        [SerializeField] private float zoom = 7.5f;

        [Tooltip("Re-roll key, as well as the button — a click reaches the player's action map, and " +
                 "a key is the only thing a probe can drive (Engineering/01-VERIFICATION.md §2).")]
        [SerializeField] private Key reRollKey = Key.F1;

        /// <summary>Raised with a one-line description of whatever was just built.</summary>
        public event Action<string> Rolled;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private string _summary = "nothing yet";

        public string Summary { get { return _summary; } }

        private void Start()
        {
            ReRoll();
        }

        private void Update()
        {
            // Straight off Keyboard.current: debug keys must never appear in the player's action
            // map, which is shipped content and where the rebinding UI will read from.
            if (Keyboard.current != null && Keyboard.current[reRollKey].wasPressedThisFrame) ReRoll();
        }

        [ContextMenu("Re-roll Room")]
        public void ReRoll()
        {
            if (!Ready()) return;

            Clear();

            RoomLayoutAsset layout = Pick(layouts);
            TileBase tile = Pick(floorTiles);
            Sprite wall = Pick(wallBlocks);

            PaintFloor(layout, tile);
            int walls = PlaceWalls(layout, wall);
            int placed = ScatterProps(layout);
            FrameOn(layout);

            _summary = string.Format("{0}   floor {1}   wall {2}   {3} blocks, {4} props",
                layout.name, tile != null ? tile.name : "none",
                wall != null ? wall.name : "none", walls, placed);

            if (Rolled != null) Rolled(_summary);
        }

        private bool Ready()
        {
            if (grid == null || floor == null || scenery == null)
            {
                Debug.LogError("RoomVisualTest is missing its scene wiring.", this);
                return false;
            }

            if (layouts == null || layouts.Length == 0)
            {
                Debug.LogError("No layouts. Run Deeper/Generate Room Layout Assets.", this);
                return false;
            }

            return true;
        }

        private void Clear()
        {
            floor.ClearAllTiles();
            foreach (GameObject go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
        }

        private void PaintFloor(RoomLayoutAsset layout, TileBase tile)
        {
            if (tile == null) return;

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    // Painted under the walls too: a wall block is not a floor, and a hole behind one
                    // shows as void the moment anything is destructible.
                    floor.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private int PlaceWalls(RoomLayoutAsset layout, Sprite wall)
        {
            if (wall == null) return 0;

            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (!layout.IsSolid(x, y)) continue;
                    Spawn("Wall", wall, CellCentre(x, y), 1);
                    count++;
                }
            }
            return count;
        }

        private int ScatterProps(RoomLayoutAsset layout)
        {
            if (props == null || props.Length == 0) return 0;

            int placed = 0;
            for (int y = 1; y < layout.Height - 1; y++)
            {
                for (int x = 1; x < layout.Width - 1; x++)
                {
                    if (layout.At(x, y) != '.') continue;
                    if (UnityEngine.Random.value >= propDensity) continue;

                    Spawn("Prop", Pick(props), CellCentre(x, y), Mathf.Max(1, propScale));
                    placed++;
                }
            }
            return placed;
        }

        private GameObject Spawn(string name, Sprite sprite, Vector3 position, int scale)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(scenery, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer r = go.GetComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingLayerName = "Actors";

            // YDepthSort's own formula (step 0.1, priority 0), so scenery and live actors would
            // interleave correctly if this scene ever gained any.
            r.sortingOrder = -Mathf.RoundToInt(position.y / 0.1f) * 2;

            _spawned.Add(go);
            return go;
        }

        /// <summary>
        /// The centre of a cell's diamond. Unity's isometric Grid does the projection, which is why
        /// no (x-y)/(x+y) maths appears anywhere in this class.
        /// </summary>
        private Vector3 CellCentre(int x, int y)
        {
            return grid.CellToWorld(new Vector3Int(x, y, 0)) +
                   new Vector3(0f, grid.cellSize.y * 0.5f, 0f);
        }

        private void FrameOn(RoomLayoutAsset layout)
        {
            if (view == null) return;

            Vector3 middle = CellCentre(layout.Width / 2, layout.Height / 2);
            view.orthographic = true;
            view.orthographicSize = zoom;
            view.transform.position = new Vector3(middle.x, middle.y, -10f);
        }

        private static T Pick<T>(T[] set) where T : class
        {
            if (set == null || set.Length == 0) return null;
            return set[UnityEngine.Random.Range(0, set.Length)];
        }
    }
}
