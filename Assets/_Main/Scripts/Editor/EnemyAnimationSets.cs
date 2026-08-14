using System.Collections.Generic;
using Deeper.Animation;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Binds a sliced enemy sheet's sub-sprites into its <see cref="SpriteAnimationSet"/>, per the
    /// enemy sheet contract (12 rows x 4 columns; Idle/Move, Telegraph, Attack, Death; three
    /// authored directions).
    ///
    /// **This has to be a tool rather than inspector work.** `SpriteAnimationSet.Clip` is a private
    /// struct and `clips` is a private field, so there is no authoring API — the only ways in are
    /// `SerializedObject` or hand-written YAML. And re-slicing a sheet regenerates its sub-sprites,
    /// which silently nulls every reference pointing at them (Engineering/01-VERIFICATION.md §6).
    /// Three enemies x 5 clips x 5 direction arrays is 195 references to restore by hand after
    /// every art change; this makes it a menu click.
    ///
    /// Run it after <see cref="PlaceholderEnemySheets"/>, always in that order.
    /// </summary>
    public static class EnemyAnimationSets
    {
        private const string SheetFolder = "Assets/_Main/Art/Placeholder/Enemies";
        private const string SetFolder = "Assets/_Main/Data/Animation";

        private static readonly string[] Enemies = { "CaveCrawler", "RockSlinger", "TunnelBrute" };

        /// <summary>One clip of the contract: which state it drives and which sheet rows feed it.</summary>
        private struct ClipSpec
        {
            public CharacterState State;
            public int FirstRow;      // Down; Up and Side are the next two rows
            public int FirstColumn;
            public int Columns;

            public ClipSpec(CharacterState state, int firstRow, int firstColumn, int columns)
            {
                State = state; FirstRow = firstRow; FirstColumn = firstColumn; Columns = columns;
            }
        }

        private static readonly ClipSpec[] Contract =
        {
            // Idle binds ONE frame of the walk cycle, not all four. ART_DIRECTION §4 budgets
            // Idle/Move as a single 4-frame line, so they share art — but a standing enemy playing
            // the full cycle walks in place. Resolve() wraps frame % length, so a one-entry array
            // holds a still pose for free.
            new ClipSpec(CharacterState.Idle, 0, 0, 1),
            new ClipSpec(CharacterState.Move, 0, 0, 4),
            new ClipSpec(CharacterState.Telegraph, 3, 0, 3),
            new ClipSpec(CharacterState.BasicAttack, 6, 0, 3),
            new ClipSpec(CharacterState.Death, 9, 0, 3),
        };

        [MenuItem("Deeper/Bind Enemy Animation Sets")]
        public static void Bind()
        {
            foreach (string enemy in Enemies)
            {
                Dictionary<string, Sprite> sprites = LoadSheet(enemy);
                if (sprites == null) continue;

                string setPath = $"{SetFolder}/Anim_Enemy_{enemy}.asset";
                var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(setPath);

                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<SpriteAnimationSet>();
                    AssetDatabase.CreateAsset(set, setPath);
                }

                Write(set, enemy, sprites);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{nameof(EnemyAnimationSets)}: bound {Enemies.Length} enemy animation sets.");
        }

        private static Dictionary<string, Sprite> LoadSheet(string enemy)
        {
            string path = $"{SheetFolder}/{enemy}.png";
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"{nameof(EnemyAnimationSets)}: {path} has no sub-sprites. Run " +
                               "'Deeper/Generate Placeholder Enemy Sheets' first.");
                return null;
            }

            var byName = new Dictionary<string, Sprite>();
            foreach (Object asset in assets)
            {
                var sprite = asset as Sprite;
                if (sprite != null) byName[sprite.name] = sprite;
            }

            return byName;
        }

        private static void Write(SpriteAnimationSet set, string enemy, Dictionary<string, Sprite> sprites)
        {
            var serialized = new SerializedObject(set);
            SerializedProperty clips = serialized.FindProperty("clips");
            clips.arraySize = Contract.Length;

            for (int i = 0; i < Contract.Length; i++)
            {
                ClipSpec spec = Contract[i];
                SerializedProperty clip = clips.GetArrayElementAtIndex(i);

                clip.FindPropertyRelative("State").enumValueIndex = (int)spec.State;

                Sprite[] down = Frames(sprites, enemy, spec.FirstRow + 0, spec.FirstColumn, spec.Columns);
                Sprite[] up = Frames(sprites, enemy, spec.FirstRow + 1, spec.FirstColumn, spec.Columns);
                Sprite[] side = Frames(sprites, enemy, spec.FirstRow + 2, spec.FirstColumn, spec.Columns);

                Assign(clip.FindPropertyRelative("Down"), down);
                Assign(clip.FindPropertyRelative("Up"), up);
                Assign(clip.FindPropertyRelative("Side"), side);

                // Enemies author three directions; the diagonals reuse the Side art. Pointing the
                // arrays at the same sprites is what lets Facing.ToArt() cover all eight facings
                // with no code change, and it is the whole saving of 4-directional enemies over
                // the player's 5 authored rows. Bosses get their own diagonal art.
                Assign(clip.FindPropertyRelative("DownDiagonal"), side);
                Assign(clip.FindPropertyRelative("UpDiagonal"), side);

                // Left empty on purpose. StrikeFrames exists because the player's five directions
                // were drawn as five different swings that connect on different frames; an enemy's
                // Attack clip puts the impact on frame 0 in every direction, so there is nothing
                // to correct.
                clip.FindPropertyRelative("StrikeFrames").arraySize = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
        }

        private static Sprite[] Frames(
            Dictionary<string, Sprite> sprites, string enemy, int row, int firstColumn, int columns)
        {
            var frames = new Sprite[columns];

            for (int i = 0; i < columns; i++)
            {
                string key = $"{enemy}_{row}_{firstColumn + i}";
                if (!sprites.TryGetValue(key, out frames[i]))
                {
                    Debug.LogError($"{nameof(EnemyAnimationSets)}: {enemy} sheet has no sub-sprite " +
                                   $"'{key}'. The sheet and this contract have drifted apart.");
                }
            }

            return frames;
        }

        private static void Assign(SerializedProperty array, Sprite[] frames)
        {
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
        }
    }
}
