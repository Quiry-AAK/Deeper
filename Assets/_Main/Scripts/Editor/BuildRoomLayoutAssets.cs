using UnityEditor;
using UnityEngine;
using Deeper.Rooms;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Copies each hand-authored ASCII map into a <see cref="RoomLayoutAsset"/> so a running scene
    /// can read it. The maps stay the source of truth in `Layout_*.cs`; these are generated copies.
    ///
    /// Listed explicitly rather than found by reflection: a list is readable, and reflection over
    /// the editor assembly to find "classes with a Map field" is the kind of cleverness CLAUDE.md
    /// asks this codebase not to grow. The cost of the list is one line per room, paid once.
    /// </summary>
    public static class BuildRoomLayoutAssets
    {
        private const string Folder = "Assets/_Main/Data/Layouts";

        [MenuItem("Deeper/Generate Room Layout Assets")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_Main/Data", "Layouts");
            }

            Write("UpperCaves_01", Layout_UpperCaves_01.Map, "Layout_UpperCaves_01");
            Write("UpperCaves_02", Layout_UpperCaves_02.Map, "Layout_UpperCaves_02");
            Write("UpperCaves_03", Layout_UpperCaves_03.Map, "Layout_UpperCaves_03");
            Write("UpperCaves_04", Layout_UpperCaves_04.Map, "Layout_UpperCaves_04");
            Write("UpperCaves_05", Layout_UpperCaves_05.Map, "Layout_UpperCaves_05");
            Write("UpperCaves_06", Layout_UpperCaves_06.Map, "Layout_UpperCaves_06");
            Write("UpperCaves_07", Layout_UpperCaves_07.Map, "Layout_UpperCaves_07");
            Write("SecretVault_01", Layout_SecretVault_01.Map, "Layout_SecretVault_01");
            Write("MiniBossArena_01", Layout_MiniBossArena_01.Map, "Layout_MiniBossArena_01");
            Write("FinalBossArena_01", Layout_FinalBossArena_01.Map, "Layout_FinalBossArena_01");
            Write("FinalBossArena_02", Layout_FinalBossArena_02.Map, "Layout_FinalBossArena_02");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generate Room Layout Assets: 11 layout(s) under " + Folder + ".");
        }

        private static void Write(string name, string[] map, string source)
        {
            if (!RoomLayout.Validate(map, source)) return;

            string path = Folder + "/Layout_" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<RoomLayoutAsset>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<RoomLayoutAsset>();

            // Written through SerializedObject rather than a setter, the same way every other
            // builder here writes private fields. It also sidesteps a real trap: an `internal`
            // helper on the asset would be invisible from this file, because editor code compiles
            // into Assembly-CSharp-Editor while the asset lives in Assembly-CSharp.
            var so = new SerializedObject(asset);
            SerializedProperty rows = so.FindProperty("rows");
            rows.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++)
            {
                rows.GetArrayElementAtIndex(i).stringValue = map[i];
            }
            so.FindProperty("source").stringValue = source;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
        }
    }
}
