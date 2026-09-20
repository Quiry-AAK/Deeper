using System.Collections.Generic;
using Deeper.Player;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Imports the level-up burst (<c>Art/VFX/LevelUp.png</c>) and wires it onto the player —
    /// ART_DIRECTION §1's import contract, a slice into frames, and the <see cref="LevelUpVFX"/>
    /// component with its renderer on <c>Player.prefab</c>'s <c>Visual</c> group.
    ///
    /// One menu item for both halves because neither is useful alone: slicing regenerates the
    /// sub-sprites, and a frame list wired before the slice points at sprites that no longer exist
    /// (Engineering/01-VERIFICATION.md §6). Idempotent: it finds what an earlier run built rather
    /// than adding a second one, so it is safe to re-run after the art is regenerated.
    ///
    /// <b>The sheet is one row of square cells, each as tall as the sheet</b> — the frame count is
    /// its width over its height. That is how PixelLab's <c>animate_image</c> frames are packed, and
    /// it means a regenerated clip of a different length needs no change here.
    ///
    /// Runs without the sheet too: it wires the component with no frames, which does nothing at
    /// runtime. That is how the level-up beat was built and verified before the art existed.
    /// </summary>
    public static class ImportLevelUpVFX
    {
        private const string SheetPath = "Assets/_Main/Art/VFX/LevelUp.png";
        private const string PlayerPrefab = "Assets/_Main/Prefabs/Player.prefab";
        private const string FramePrefix = "LevelUp";

        /// <summary>
        /// Unlit, so a burst of light is not itself shaded by the cave's lighting — the rig's own
        /// Sprite-Lit-Default would dim it to whatever the Global Light 2D is set to.
        /// </summary>
        private const string UnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>
        /// Behind her body, which draws at 0. See <see cref="LevelUpVFX"/>'s renderer tooltip for why
        /// behind rather than over.
        /// </summary>
        private const int SortingOrder = -1;

        /// <summary>
        /// Where the burst's centre sits relative to <c>Visual</c>, whose origin is already the centre
        /// of her drawn body (Visual sits 0.75 up the root, half her 1.5-unit height). Zero centres
        /// the burst on her; a negative y drops a ground ring toward her feet.
        /// </summary>
        private static readonly Vector3 BurstOffset = Vector3.zero;

        [MenuItem("Deeper/Import Level-Up VFX")]
        public static void Import()
        {
            Sprite[] frames = ImportSheet();
            WirePlayer(frames);
        }

        // ---------------------------------------------------------------- sheet

        private static Sprite[] ImportSheet()
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("No " + SheetPath + " yet — wiring LevelUpVFX with no frames. It " +
                                 "will do nothing until the art is imported by re-running this.");
                return new Sprite[0];
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;               // ART_DIRECTION §1
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int count = height > 0 ? width / height : 0;

            if (count <= 0 || width % height != 0)
            {
                Debug.LogError(SheetPath + " is " + width + "x" + height + " — expected one row of " +
                               "square frames, width a whole multiple of height. Not sliced.");
                return new Sprite[0];
            }

            Slice(importer, count, height);
            importer.SaveAndReimport();

            // Loaded back by name, in column order, rather than trusting LoadAllAssets' order.
            var byName = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(SheetPath))
            {
                if (asset is Sprite sprite) byName[sprite.name] = sprite;
            }

            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                byName.TryGetValue(FrameName(i), out frames[i]);
            }

            Debug.Log("Imported " + SheetPath + ": " + count + " frames of " + height + "x" + height + ".");
            return frames;
        }

        /// <summary>
        /// Cuts the strip into <c>LevelUp_0_&lt;col&gt;</c>, the <c>&lt;piece&gt;_&lt;row&gt;_&lt;col&gt;</c>
        /// convention every sheet in the project uses. Existing sprite ids are kept by name, so a
        /// re-run over unchanged art does not regenerate them.
        /// </summary>
        private static void Slice(TextureImporter importer, int count, int cell)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();

            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var existing = new Dictionary<string, GUID>();
            foreach (SpriteRect old in provider.GetSpriteRects()) existing[old.name] = old.spriteID;

            var rects = new SpriteRect[count];
            var names = new List<SpriteNameFileIdPair>();

            for (int i = 0; i < count; i++)
            {
                string name = FrameName(i);

                rects[i] = new SpriteRect
                {
                    name = name,
                    spriteID = existing.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(i * cell, 0, cell, cell),
                };

                names.Add(new SpriteNameFileIdPair(name, rects[i].spriteID));
            }

            provider.SetSpriteRects(rects);

            // Without this table the sub-sprite file ids are regenerated on every reimport, and the
            // frame list on the prefab silently goes null (01-VERIFICATION §6).
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(names);

            provider.Apply();
        }

        private static string FrameName(int column)
        {
            return FramePrefix + "_0_" + column;
        }

        // ---------------------------------------------------------------- prefab

        private static void WirePlayer(Sprite[] frames)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);

            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    Debug.LogError(PlayerPrefab + " has no Visual group — nothing wired.");
                    return;
                }

                Transform burst = visual.Find("LevelUpBurst");
                if (burst == null)
                {
                    burst = new GameObject("LevelUpBurst").transform;
                    burst.SetParent(visual, false);
                }

                // The whole rig is on the Player layer (CLAUDE.md, prefab layout).
                burst.gameObject.layer = visual.gameObject.layer;
                burst.localPosition = BurstOffset;
                burst.localRotation = Quaternion.identity;
                burst.localScale = Vector3.one;

                var renderer = burst.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = burst.gameObject.AddComponent<SpriteRenderer>();

                var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
                if (unlit != null) renderer.sharedMaterial = unlit;
                else Debug.LogWarning("No " + UnlitMaterialPath + " — the burst keeps the default material.");

                renderer.sortingOrder = SortingOrder;
                renderer.sprite = frames.Length > 0 ? frames[0] : null;

                // Off until played. LevelUpVFX switches it off in Awake as well; this keeps the
                // prefab from showing a frozen burst on her in the Scene view.
                renderer.enabled = false;

                var vfx = visual.GetComponent<LevelUpVFX>();
                if (vfx == null) vfx = visual.gameObject.AddComponent<LevelUpVFX>();

                var so = new SerializedObject(vfx);

                SerializedProperty list = so.FindProperty("frames");
                list.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                }

                so.FindProperty("burstRenderer").objectReferenceValue = renderer;
                so.FindProperty("experience").objectReferenceValue = root.GetComponent<PlayerXP>();
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
                Debug.Log("Wired LevelUpVFX on " + PlayerPrefab + " with " + frames.Length + " frames.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
