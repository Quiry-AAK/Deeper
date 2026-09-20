using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// One room's hand-authored ASCII map, as an asset a running game can read.
    ///
    /// The maps themselves live in `Scripts/Editor/Layout_*.cs` and are edited there by a human —
    /// LEVEL_DESIGN section 1 locks rooms as hand-built, and a person editing those characters is the
    /// hand. But an editor script is compiled into the editor assembly, so nothing at runtime can
    /// see it. `Deeper/Generate Room Layout Assets` copies each map into one of these, which is what
    /// a scene actually loads.
    ///
    /// Generated, never hand-edited: change the .cs and re-run the menu item, or the asset and the
    /// layout it came from drift apart.
    /// </summary>
    [CreateAssetMenu(fileName = "Layout_", menuName = "Deeper/Rooms/Room Layout", order = 6)]
    public sealed class RoomLayoutAsset : ScriptableObject
    {
        [Tooltip("Row 0 is the room's TOP row, matching the ASCII source it was copied from.")]
        [SerializeField] private string[] rows;

        [Tooltip("The Layout_*.cs class this was generated from, so the source is findable.")]
        [SerializeField] private string source;

        public string[] Rows { get { return rows; } }
        public string Source { get { return source; } }

        public int Width { get { return rows != null && rows.Length > 0 ? rows[0].Length : 0; } }
        public int Height { get { return rows != null ? rows.Length : 0; } }

        /// <summary>Reads a cell with y counting UP from the bottom, matching RoomLayout.At.</summary>
        public char At(int x, int y)
        {
            if (rows == null || rows.Length == 0) return '#';
            int row = rows.Length - 1 - y;
            if (row < 0 || row >= rows.Length) return '#';
            string line = rows[row];
            return x >= 0 && x < line.Length ? line[x] : '#';
        }

        public bool IsSolid(int x, int y)
        {
            char c = At(x, y);
            return c == '#' || c == 'O';
        }
    }
}
