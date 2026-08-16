using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Generates the flat-colour placeholder art a room needs, and imports it to the project's
    /// sprite settings.
    ///
    /// Committed for the same reason <see cref="PlaceholderEnemySheets"/> is: placeholder art gets
    /// regenerated every time a silhouette or a colour turns out to read badly, and a generator that
    /// ran once in a scratch directory is a tool you pay for twice.
    ///
    /// Programmer art, deliberately NOT routed through the `deeper-art` skill — this exists so that
    /// "is the room locked right now" is answerable at a glance while the lock is being tuned, not
    /// to set style.
    /// </summary>
    public static class PlaceholderRoomArt
    {
        private const string OutputFolder = "Assets/_Main/Art/Placeholder/Rooms";

        /// <summary>One tile wide, two tall — the door gap in the wall ring is 1×2.</summary>
        private const int DoorWidth = 32;
        private const int DoorHeight = 64;

        // Muted brown against the wall tile's grey-purple (sampled: base 87,82,92). Brown rather
        // than a brighter colour because ART_DIRECTION §2 reserves orange-red for hazard telegraphs
        // and cyan-white for hazard accents — a door wearing either would read as a threat.
        private static readonly Color32 Base = new Color32(96, 70, 48, 255);
        private static readonly Color32 Shadow = new Color32(58, 42, 30, 255);
        private static readonly Color32 Highlight = new Color32(128, 98, 70, 255);

        // Iron bands. Deliberately the wall's own highlight grey, so the door reads as part of the
        // stonework rather than as a prop dropped in front of it.
        private static readonly Color32 Band = new Color32(117, 110, 124, 255);

        [MenuItem("Deeper/Generate Placeholder Room Art")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);

            string path = OutputFolder + "/Door.png";
            File.WriteAllBytes(path, RenderDoor());

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings(path);

            Debug.Log("Placeholder room art written to " + path);
        }

        private static byte[] RenderDoor()
        {
            var pixels = new Color32[DoorWidth * DoorHeight];

            for (int y = 0; y < DoorHeight; y++)
            {
                for (int x = 0; x < DoorWidth; x++)
                {
                    Color32 c = Base;

                    // A 2px inset border, so the door edge stays visible where it meets the wall
                    // tiles either side of the gap. Without it the two shapes merge into one block
                    // and "is it shut" stops being readable at game zoom.
                    bool border = x < 2 || x >= DoorWidth - 2 || y < 2 || y >= DoorHeight - 2;
                    if (border) c = Shadow;

                    // Two horizontal bands. They are what make the door read as a barrier rather
                    // than a coloured rectangle at 32px — the vertical grain alone is too subtle.
                    bool band = (y >= 16 && y < 20) || (y >= 44 && y < 48);
                    if (band && !border) c = Band;

                    // Plank grooves, one every 8px, skipped on the bands so they stay unbroken.
                    if (!border && !band && x % 8 == 0) c = Shadow;

                    // A single lit column down the left, matching the top-left key light the rest
                    // of the placeholder art is drawn with (ART_DIRECTION §2).
                    if (!border && !band && x == 3) c = Highlight;

                    pixels[y * DoorWidth + x] = c;
                }
            }

            var texture = new Texture2D(DoorWidth, DoorHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        private static void ApplyImportSettings(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;              // ART_DIRECTION §1
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            importer.SaveAndReimport();
        }
    }
}
