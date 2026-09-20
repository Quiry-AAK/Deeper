using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.Rooms
{
    /// <summary>
    /// Gives one handcrafted layout a different look every time it is drawn — the half of the Hades
    /// model that is not geometry. LEVEL_DESIGN §1 locks rooms as hand-built and §2 calls the
    /// variable part "tile dressing"; this is that, and it never moves a wall.
    ///
    /// Three levers, cheapest first:
    ///
    /// 1. **Per-cell rotation.** Four looks per floor tile for no art at all. Safe because the tile
    ///    assets carry <c>TileFlags.LockColor</c> but *not* <c>LockTransform</c>.
    /// 2. **Weighted tile variants**, floor and wall.
    /// 3. **Ground decals** on a third tilemap.
    ///
    /// Note what is deliberately absent: **nothing is tinted.** Both committed tile assets carry
    /// <c>LockColor</c>, so <c>Tilemap.SetColor</c> is a silent no-op on them — no error, no effect.
    /// Unlocking per cell would work, but tinting is also the mechanism most likely to wander into
    /// ART_DIRECTION §2's reserved hazard accents by accident, since a hue shift on grey stone
    /// drifts orange without anyone deciding to. Variety comes from which tile, not what colour.
    ///
    /// Also absent: Unity's <c>RuleTile</c> random output. It resolves through a Perlin sample of the
    /// grid position, so the same room at the same coordinates would look identical every run —
    /// exactly backwards from the goal. (<c>RandomTile</c> proper is not even in tilemap.extras
    /// 4.1.0.) The picking is done here, against the run's own stream.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomDressing : MonoBehaviour
    {
        [Header("Tilemaps — wired by BuildRoomPrefab")]
        [SerializeField] private Tilemap floor;
        [SerializeField] private Tilemap walls;

        [Tooltip("Ground decals. A tilemap rather than scattered SpriteRenderers, so 'decoration " +
                 "never collides' is true by construction instead of by convention: decor tiles are " +
                 "authored ColliderType.None and this map has no TilemapCollider2D, so there is " +
                 "nowhere a collider could be added by mistake. EnemyChase has no pathfinding, and " +
                 "one stray solid cell can trap an enemy in a room that then never unlocks.")]
        [SerializeField] private Tilemap decor;

        [Tooltip("Which cells may take a decal. Row 0 is the room's TOP row, matching the ASCII " +
                 "map. Baked by BuildRoomPrefab from the same map the tiles are painted from, so " +
                 "the two cannot drift; a runtime component cannot read the editor-only layout.")]
        [SerializeField] private string[] decorMask;

        private int _signature;

        /// <summary>
        /// A hash of every choice this room made. Exists because "it looks different every time" is
        /// the one goal here that cannot be checked by reading state back, and screenshots are
        /// awkward in this project's editor layout: two runs printing the same number are provably
        /// not varying. It does not prove the result looks *good* — only a human can say that.
        /// </summary>
        public int Signature { get { return _signature; } }

        /// <summary>
        /// Called by <see cref="Deeper.Run.FloorLoader"/> while the room instance is still parented
        /// to its inactive staging holder, so a room is never visible undressed for a frame.
        ///
        /// Explicitly not an Awake hook: the randomness belongs to the run, not to the room, and a
        /// room that dressed itself could not be handed a seed.
        /// </summary>
        public void Dress(RoomTheme theme, System.Random random)
        {
            if (theme == null || random == null) return;

            _signature = 0;

            PaintVariants(floor, theme.FloorTiles, theme.RotateFloorTiles, random);
            PaintVariants(walls, theme.WallTiles, false, random);
            ScatterDecor(theme, random);
        }

        /// <summary>
        /// Swaps each painted cell for a random variant, and optionally gives it a quarter turn.
        ///
        /// Variants need no eligibility mask, unlike decals: a floor variant under a doorway is
        /// still floor, and under a spawn marker it is still floor. Only decoration has to keep
        /// clear of anything.
        /// </summary>
        private void PaintVariants(Tilemap map, TileBase[] variants, bool rotate, System.Random random)
        {
            if (map == null || variants == null || variants.Length == 0) return;

            BoundsInt bounds = map.cellBounds;

            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!map.HasTile(cell)) continue;

                TileBase pick = variants[random.Next(variants.Length)];
                map.SetTile(cell, pick);
                Accumulate(pick != null ? pick.name.GetHashCode() : 0);

                if (!rotate) continue;

                int quarters = random.Next(4);
                Accumulate(quarters);

                if (quarters == 0) continue;

                // Safe because the tile assets do not set TileFlags.LockTransform — only LockColor.
                map.SetTransformMatrix(cell, Matrix4x4.TRS(
                    Vector3.zero, Quaternion.Euler(0f, 0f, 90f * quarters), Vector3.one));
            }
        }

        private void ScatterDecor(RoomTheme theme, System.Random random)
        {
            if (decor == null) return;

            decor.ClearAllTiles();

            TileBase[] set = theme.DecorTiles;
            if (set == null || set.Length == 0) return;
            if (decorMask == null || decorMask.Length == 0) return;

            int height = decorMask.Length;

            for (int y = 0; y < height; y++)
            {
                // Row 0 is the top row, so the highest y in the string array is the lowest cell.
                string row = decorMask[height - 1 - y];
                if (row == null) continue;

                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] != Eligible) continue;
                    if (random.NextDouble() >= theme.DecorDensity) continue;

                    TileBase pick = set[random.Next(set.Length)];
                    decor.SetTile(new Vector3Int(x, y, 0), pick);
                    Accumulate(x * 73856093 ^ y * 19349663);
                }
            }
        }

        private void Accumulate(int value)
        {
            unchecked { _signature = _signature * 31 + value; }
        }

        /// <summary>The character <see cref="decorMask"/> uses for a cell that may take a decal.</summary>
        public const char Eligible = '.';
    }
}
