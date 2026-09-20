using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.Rooms
{
    /// <summary>
    /// One look a room can be dressed in. LEVEL_DESIGN §2's own phrase for this is "tile dressing",
    /// which is where the name comes from.
    ///
    /// Several themes per biome is what makes a repeated layout read as a new room — the Hades
    /// model, where geometry is handcrafted and everything on top of it is drawn. It also stays
    /// inside ART_DIRECTION §2's locked palettes, because the variety comes from *which tile* rather
    /// than from tinting: a hue shift on grey stone drifts toward the reserved orange-red hazard
    /// accent without anyone deciding to, and §2 reserves that colour for danger telegraphs alone.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme_", menuName = "Deeper/Rooms/Room Theme", order = 5)]
    public sealed class RoomTheme : ScriptableObject
    {
        [Header("Tiles")]
        [Tooltip("Floor variants, picked per cell. Leave empty to keep whatever the room was built " +
                 "with.")]
        [SerializeField] private TileBase[] floorTiles;

        [Tooltip("Wall variants, picked per cell.")]
        [SerializeField] private TileBase[] wallTiles;

        [Tooltip("Ground-plane decals scattered on eligible floor cells: cracks, moss, rubble, ore " +
                 "glints. Nothing waist-high — a decor tilemap cannot Y-sort per cell, and anything " +
                 "with visible height also reads as cover the player expects to block.")]
        [SerializeField] private TileBase[] decorTiles;

        [Header("Density")]
        [Tooltip("Fraction of eligible floor cells that get a decal.")]
        [SerializeField, Range(0f, 0.4f)] private float decorDensity = 0.12f;

        [Tooltip("Rotate floor cells by a random quarter turn. Four looks per tile for free, and " +
                 "the cheapest variety in the system — it needs no art at all.")]
        [SerializeField] private bool rotateFloorTiles = true;

        [Header("Mood")]
        [Tooltip("Ambient light colour for the biome. Applied when the theme changes, not per " +
                 "room: two rooms are alive at once and physically adjacent, so per-room colours " +
                 "would visibly disagree across the shared wall.")]
        [SerializeField] private Color ambientLight = Color.white;

        public TileBase[] FloorTiles { get { return floorTiles; } }
        public TileBase[] WallTiles { get { return wallTiles; } }
        public TileBase[] DecorTiles { get { return decorTiles; } }
        public float DecorDensity { get { return decorDensity; } }
        public bool RotateFloorTiles { get { return rotateFloorTiles; } }
        public Color AmbientLight { get { return ambientLight; } }

        /// <summary>
        /// Catches the one authoring mistake that breaks a room silently: a floor or decor tile that
        /// carries a collider. <c>EnemyChase</c> has no pathfinding, so a stray solid cell in open
        /// floor can form a pocket that traps an enemy — and a room only unlocks when every enemy is
        /// dead, so the fight simply never ends. Walls are meant to collide; nothing else is.
        /// </summary>
        private void OnValidate()
        {
            WarnIfSolid(floorTiles, "floorTiles");
            WarnIfSolid(decorTiles, "decorTiles");
        }

        private void WarnIfSolid(TileBase[] set, string field)
        {
            if (set == null) return;

            foreach (TileBase entry in set)
            {
                Tile tile = entry as Tile;
                if (tile != null && tile.colliderType != Tile.ColliderType.None)
                {
                    Debug.LogError(string.Format(
                        "{0}.{1} contains '{2}', which has colliderType {3}. Floor and decor tiles " +
                        "must be None, or they can trap an enemy in a room that then never unlocks.",
                        name, field, tile.name, tile.colliderType), this);
                }
            }
        }
    }
}
