using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.Rooms
{
    /// <summary>
    /// Draws a room. Everything you see of a room comes from here; everything you collide with
    /// comes from the prefab's collision tilemap, and the two are deliberately separate.
    ///
    /// That split is what makes the visuals re-rollable. The room's shape, its doors, its spawn
    /// markers and its collision are fixed by the hand-authored layout and never change; the floor
    /// tile, the wall block and the scatter of props are drawn fresh on every enable. A pooled room
    /// re-mounted later looks different without a single collider moving.
    ///
    /// **Depth needs no special case.** In an isometric grid a cell further back has a larger
    /// (x + y) and therefore a higher world Y, so sorting on world Y — exactly what
    /// <see cref="Deeper.Core.YDepthSort"/> already does for actors — is isometric depth sorting.
    /// Walls and props use the same formula and the same Actors layer, so the player walks behind
    /// the blocks above her and in front of the ones below with nothing added.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IsometricRoomView : MonoBehaviour
    {
        [Header("Layout — baked by BuildRoomPrefab")]
        [Tooltip("The room's ASCII map. Row 0 is the TOP row, matching the Layout_*.cs source. " +
                 "Baked in rather than looked up so a room prefab is self-contained.")]
        [SerializeField] private string[] map;

        [Header("Scene parts — wired by BuildRoomPrefab")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap floor;
        [SerializeField] private Transform scenery;

        [Header("Art sets — one is drawn per roll")]
        [SerializeField] private TileBase[] floorTiles;
        [SerializeField] private Sprite[] wallBlocks;
        [SerializeField] private Sprite[] props;

        [Header("Tuning")]
        [Tooltip("Fraction of open floor cells that get a prop.")]
        [SerializeField, Range(0f, 0.3f)] private float propDensity = 0.05f;

        [Tooltip("Props are authored at 32px against a 64px tile, so they need doubling. A whole " +
                 "number, because anything else resamples pixel art off its own grid.")]
        [SerializeField] private int propScale = 2;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// Re-drawn on enable, not on Awake: rooms are pooled, and a recycled instance never runs
        /// Awake again. This is the same rule every pooled actor in the project follows.
        /// </summary>
        private void OnEnable()
        {
            Randomise();
        }

        [ContextMenu("Randomise Visuals")]
        public void Randomise()
        {
            if (grid == null || floor == null || map == null || map.Length == 0) return;

            Clear();

            TileBase tile = Pick(floorTiles);
            Sprite block = Pick(wallBlocks);

            int height = map.Length;
            int width = map[0].Length;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Floor is painted under the walls too: a wall block is not a floor, and a hole
                    // behind one shows as void the moment anything is destructible.
                    if (tile != null) floor.SetTile(new Vector3Int(x, y, 0), tile);

                    char cell = At(x, y);

                    if (cell == '#' || cell == 'O')
                    {
                        if (block != null) Spawn(block, Centre(x, y), 1);
                    }
                    else if (cell == '.' && Random.value < propDensity)
                    {
                        Spawn(Pick(props), Centre(x, y), Mathf.Max(1, propScale));
                    }
                }
            }
        }

        private void Clear()
        {
            floor.ClearAllTiles();

            foreach (GameObject go in _spawned)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private void Spawn(Sprite sprite, Vector3 local, int scale)
        {
            if (sprite == null) return;

            var go = new GameObject("Scenery", typeof(SpriteRenderer));
            go.transform.SetParent(scenery != null ? scenery : transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer r = go.GetComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingLayerName = "Actors";

            // YDepthSort's own formula (step 0.1, priority 0). Using the WORLD y, because a room can
            // be mounted anywhere and an actor's sort order is computed from its world position too —
            // comparing a local number against a world one is how scenery ends up in front of a
            // player standing behind it.
            r.sortingOrder = -Mathf.RoundToInt(go.transform.position.y / 0.1f) * 2;

            _spawned.Add(go);
        }

        private char At(int x, int y)
        {
            string row = map[map.Length - 1 - y];
            return x >= 0 && x < row.Length ? row[x] : '#';
        }

        private Vector3 Centre(int x, int y)
        {
            Vector3 size = grid.cellSize;
            return new Vector3((x - y) * size.x * 0.5f,
                               (x + y) * size.y * 0.5f + size.y * 0.5f,
                               0f);
        }

        private static T Pick<T>(T[] set) where T : class
        {
            if (set == null || set.Length == 0) return null;
            return set[Random.Range(0, set.Length)];
        }
    }
}
