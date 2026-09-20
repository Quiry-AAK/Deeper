using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Imports the camp's fixture sprites in <c>Art/Environment/SurfaceCamp/Props/</c>.
    ///
    /// Separate from <see cref="BuildRoomTiles"/> and <see cref="BuildIsometricArt"/> for one
    /// reason, and it is the whole content of this file: **a camp fixture pivots at its feet.**
    /// Both of those set a Center pivot, which is right for a tile — a diamond's widest row sits on
    /// its canvas centre, so centre-pivoting lands it exactly on the cell. A tent is not a diamond.
    /// Centre-pivoting a 128px tent on a cell buries its lower half in the ground, and the taller
    /// the fixture the worse it gets: the 160px headframe would sink to its crossbeam.
    ///
    /// With a bottom-centre pivot the sprite's baseline *is* its ground contact, so
    /// <see cref="BuildHubScene"/> places every fixture at the plain cell centre with no per-prop
    /// offset to tune — and adding a new prop needs no new number.
    ///
    /// They also live in their own folder rather than at the biome root, so `Generate Room Tiles`
    /// (whose scan is non-recursive) cannot reach them and reset this pivot on its next run.
    /// </summary>
    public static class BuildHubArt
    {
        private const string PropFolder = "Assets/_Main/Art/Environment/SurfaceCamp/Props";
        private const string IsoFolder = "Assets/_Main/Art/Environment/SurfaceCamp/Iso";

        /// <summary>A tile's diamond: 64 wide, 32 tall, on a 64x64 canvas. ART_DIRECTION §1's 2:1.</summary>
        private const int TileSize = 64;
        private const int HalfHeight = 16;


        [MenuItem("Deeper/Generate Hub Art")]
        public static void Generate()
        {
            if (!Directory.Exists(PropFolder))
            {
                Debug.LogError("No " + PropFolder + " folder.");
                return;
            }

            int flattened = FlattenFloorTiles();

            int done = 0;

            foreach (string file in Directory.GetFiles(PropFolder, "*.png"))
            {
                string path = file.Replace('\\', '/');
                int index = path.IndexOf("Assets/");
                if (index > 0) path = path.Substring(index);

                if (Import(path)) done++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generate Hub Art: imported " + done + " camp fixture sprite(s), flattened " +
                      flattened + " floor tile(s).");
        }

        /// <summary>
        /// Strips the side wall off every <c>Iso/Floor_*.png</c>, leaving the bare top-face diamond.
        ///
        /// **This is the fix for the defect that made the first three builds of the camp unusable**,
        /// and the rule it encodes applies to every isometric floor tile this project ever
        /// generates. A generated tile is a little 3D block: a diamond top face with a side wall
        /// hanging below it. That wall lands in exactly the pixels the neighbouring tile's top face
        /// occupies, so whichever draws last wins — and this project's URP `Renderer2D` runs
        /// `TransparencySortMode.Default` under an orthographic camera, which sorts by Z. Every tile
        /// sits at z = 0, so they all tie and the order within a Tilemap is undefined. The floor
        /// came out as a field of raised blocks; `TilemapRenderer.mode` and `sortOrder` were both
        /// already correct and changing `sortOrder` did nothing, because it was never being applied.
        ///
        /// A floor does not need a wall — nothing ever sees the underside of the ground, because the
        /// camp is walled on all four sides. With the wall gone there is nothing left to reveal and
        /// the draw order stops mattering at all, which is a far more durable fix than persuading
        /// the renderer to sort. Walls keep their block: a wall is *meant* to read as raised.
        ///
        /// Idempotent — a tile with no full-width row below its axis is already flat and is skipped,
        /// so this is safe to re-run and safe to run on art that was prepared before it existed.
        /// </summary>
        private static int FlattenFloorTiles()
        {
            if (!Directory.Exists(IsoFolder)) return 0;

            int done = 0;

            foreach (string file in Directory.GetFiles(IsoFolder, "Floor_*.png"))
            {
                if (Flatten(file)) done++;
            }

            return done;
        }

        private static bool Flatten(string file)
        {
            // Loaded from the bytes rather than through AssetDatabase, so this needs no readable
            // import setting and cannot be affected by whatever the importer is currently doing.
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(file)))
            {
                UnityEngine.Object.DestroyImmediate(source);
                return false;
            }

            try
            {
                if (source.width != TileSize || source.height != TileSize) return false;

                Color32[] pixels = source.GetPixels32();

                // The diamond's axis is the FIRST full-width row scanning DOWN from the top: above
                // it is the top face's upper slope, below it the wall — which is also full width,
                // hence "first". Texture rows count up from the bottom, so scanning down is
                // descending y.
                int axis = -1;
                for (int y = TileSize - 1; y >= 0; y--)
                {
                    if (RowWidth(pixels, y) >= TileSize - 1) { axis = y; break; }
                }

                if (axis < 0) return false;

                // Already flat? Measured by the silhouette's HEIGHT, not by whether the row under
                // the axis is full width.
                //
                // The width test was the obvious one and it was wrong: the skirt this method adds
                // makes the row below the axis full width too, so a tile that had already been
                // flattened looked exactly like an unflattened block and was cropped again on every
                // run — shifting it a pixel and eating its edges each time. Height cannot be fooled
                // that way. A bare diamond is 32 rows plus the skirt; a block carries its wall on
                // top of that and is 50+.
                int top = -1;
                int bottom = -1;
                for (int y = TileSize - 1; y >= 0; y--)
                {
                    if (RowWidth(pixels, y) <= 0) continue;
                    if (top < 0) top = y;
                    bottom = y;
                }

                if (top - bottom + 1 <= (TileSize / 2) + 6) return false;

                var flat = new Color32[TileSize * TileSize];

                for (int d = -HalfHeight; d <= HalfHeight; d++)
                {
                    int from = axis + d;
                    int to = TileSize / 2 + d;
                    if (from < 0 || from >= TileSize || to < 0 || to >= TileSize) continue;

                    // Half-width of a 2:1 diamond at vertical offset d from its axis...
                    int trueHalf = (TileSize / 2) - (2 * Mathf.Abs(d));

                    // ...plus a two-pixel skirt. An exact diamond tapers to nothing at each vertex,
                    // so neighbours meet at a point rather than overlapping and the camera's
                    // background showed through as a speck on every tile — a regular lattice of
                    // them across the whole floor. The skirt makes tiles overlap slightly instead.
                    //
                    // The skirt copies the diamond's own EDGE colour outward (the source x is
                    // clamped to the true edge) rather than whatever the source has out there,
                    // which would be side wall. That distinction matters because the draw order
                    // between two tiles is undefined here: if the skirt held wall pixels, an
                    // overlap would sometimes paint a dark fringe over the neighbour's face. Edge
                    // colour overlapping edge colour is invisible whichever way it lands.
                    int half = Mathf.Min(TileSize / 2, trueHalf + 2);
                    if (half <= 0) continue;

                    int edge = Mathf.Max(trueHalf, 1);

                    for (int x = (TileSize / 2) - half; x < (TileSize / 2) + half; x++)
                    {
                        if (x < 0 || x >= TileSize) continue;

                        int sourceX = Mathf.Clamp(x, (TileSize / 2) - edge, (TileSize / 2) + edge - 1);
                        flat[to * TileSize + x] = pixels[from * TileSize + sourceX];
                    }
                }

                var output = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                output.SetPixels32(flat);
                output.Apply();

                File.WriteAllBytes(file, output.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(output);

                Debug.Log("Flattened " + Path.GetFileName(file) + " (diamond axis at row " + axis +
                          "); its side wall would otherwise show through every neighbouring tile.");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static int RowWidth(Color32[] pixels, int y)
        {
            int first = -1;
            int last = -1;

            for (int x = 0; x < TileSize; x++)
            {
                if (pixels[y * TileSize + x].a <= 8) continue;
                if (first < 0) first = x;
                last = x;
            }

            return first < 0 ? 0 : last - first + 1;
        }

        private static bool Import(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;              // ART_DIRECTION §1
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return true;
        }
    }
}
