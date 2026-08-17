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

        /// <summary>One tile square — the pedestal stands on a single floor cell.</summary>
        private const int PedestalSize = 32;

        // A desaturated brass for the vault's lock plate and the pedestal's prize. Deliberately
        // muted and never saturated: ART_DIRECTION §2 reserves orange-red for hazard telegraphs, and
        // a bright gold at this size reads as one across a room.
        private static readonly Color32 Brass = new Color32(146, 118, 62, 255);
        private static readonly Color32 BrassShadow = new Color32(96, 76, 40, 255);

        // Stone, sampled from the same wall base the door is matched against.
        private static readonly Color32 Stone = new Color32(87, 82, 92, 255);
        private static readonly Color32 StoneShadow = new Color32(56, 52, 62, 255);
        private static readonly Color32 StoneLight = new Color32(117, 110, 124, 255);

        [MenuItem("Deeper/Generate Placeholder Room Art")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);

            Write("Door.png", RenderDoor());
            Write("VaultDoor.png", RenderVaultDoor());
            Write("Pedestal.png", RenderPedestal());

            Debug.Log("Placeholder room art written to " + OutputFolder);
        }

        private static void Write(string fileName, byte[] png)
        {
            string path = OutputFolder + "/" + fileName;
            File.WriteAllBytes(path, png);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings(path);
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

            return Encode(pixels, DoorWidth, DoorHeight);
        }

        /// <summary>
        /// The vault door. Same 1x2 gap as a floor door, but it has to answer a different question
        /// at a glance — not "is this shut" but "is this the one I need a key for" — so it is iron
        /// where the floor door is timber, with a brass lock plate at eye height.
        /// </summary>
        private static byte[] RenderVaultDoor()
        {
            var pixels = new Color32[DoorWidth * DoorHeight];

            for (int y = 0; y < DoorHeight; y++)
            {
                for (int x = 0; x < DoorWidth; x++)
                {
                    Color32 c = Band;

                    bool border = x < 2 || x >= DoorWidth - 2 || y < 2 || y >= DoorHeight - 2;
                    if (border) c = StoneShadow;

                    // Riveted plating: three horizontal seams instead of the timber door's two
                    // bands, which is what carries "reinforced" at 32px without any new colour.
                    bool seam = (y >= 12 && y < 15) || (y >= 30 && y < 33) || (y >= 48 && y < 51);
                    if (seam && !border) c = StoneShadow;

                    // Rivets along each seam.
                    if (seam && !border && x % 7 == 3) c = StoneLight;

                    // The lock plate, centred at the height a hand would be. This is the only part
                    // that differs in hue, so it is what the eye lands on.
                    bool plate = x >= 11 && x < 21 && y >= 34 && y < 46;
                    if (plate && !border)
                    {
                        c = Brass;

                        bool plateEdge = x == 11 || x == 20 || y == 34 || y == 45;
                        if (plateEdge) c = BrassShadow;

                        // The keyhole itself: a slot dark enough to read as a hole.
                        bool keyhole = x >= 14 && x < 18 && y >= 37 && y < 43;
                        if (keyhole) c = StoneShadow;
                    }

                    pixels[y * DoorWidth + x] = c;
                }
            }

            return Encode(pixels, DoorWidth, DoorHeight);
        }

        /// <summary>
        /// The vault's payout pedestal. A stone plinth with a brass prize resting on it — the prize
        /// is the readable part, because the plinth alone is indistinguishable from an interior post
        /// at this size, and a post is the one thing in a room you must not confuse with a prop.
        /// </summary>
        private static byte[] RenderPedestal()
        {
            var pixels = new Color32[PedestalSize * PedestalSize];

            for (int y = 0; y < PedestalSize; y++)
            {
                for (int x = 0; x < PedestalSize; x++)
                {
                    // Transparent by default: the pedestal does not fill its cell, so the floor
                    // tile reads around it and it sits in the room rather than replacing a tile.
                    Color32 c = new Color32(0, 0, 0, 0);

                    // Plinth: a tapered base, wider at the bottom, occupying the lower half.
                    int inset = 6 + y / 4;
                    bool plinth = y < 16 && x >= inset && x < PedestalSize - inset;
                    if (plinth)
                    {
                        c = Stone;

                        if (x == inset || x == PedestalSize - inset - 1 || y == 0) c = StoneShadow;
                        if (y == 15) c = StoneLight;                 // the lit top face
                        if (x == inset + 1 && y > 0 && y < 15) c = StoneLight;   // top-left key light
                    }

                    // The prize, sitting on that top face.
                    bool prize = x >= 12 && x < 20 && y >= 16 && y < 24;
                    if (prize)
                    {
                        c = Brass;

                        if (x == 12 || x == 19 || y == 16) c = BrassShadow;
                        if (x == 13 && y > 16 && y < 23) c = StoneLight;
                    }

                    pixels[y * PedestalSize + x] = c;
                }
            }

            return Encode(pixels, PedestalSize, PedestalSize);
        }

        private static byte[] Encode(Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
