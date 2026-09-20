using System.Collections.Generic;
using Deeper.Character;
using Deeper.Run;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Creates or refreshes <c>Data/Run/RunConfig.asset</c> — the Hub's choice, carried into the run.
    ///
    /// The weapon list is read off <c>Data/Weapons/</c> rather than typed here, for the reason every
    /// generator in this folder exists: a list assembled by dragging is one that silently disagrees
    /// with the assets on disk the moment a fourth weapon is authored. It is ordered by
    /// <see cref="WeaponType"/>, whose values are stable and documented as such, so the rack always
    /// reads Katana, Bow, Greatsword — GDD §Player's own order.
    ///
    /// Re-running it keeps whichever weapon is currently chosen. That matters while iterating: this
    /// is the only asset in the project that a *running game* writes to, and clobbering the field on
    /// every rebuild would make the rack look broken.
    /// </summary>
    public static class BuildRunConfig
    {
        private const string Folder = "Assets/_Main/Data/Run";
        private const string AssetPath = Folder + "/RunConfig.asset";

        [MenuItem("Deeper/Build Run Config")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_Main/Data", "Run");
            }

            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(AssetPath);
            bool isNew = config == null;

            if (isNew)
            {
                config = ScriptableObject.CreateInstance<RunConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }

            List<WeaponDefinition> weapons = FindWeapons();

            var so = new SerializedObject(config);

            SerializedProperty available = so.FindProperty("available");
            available.arraySize = weapons.Count;
            for (int i = 0; i < weapons.Count; i++)
            {
                available.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            }

            // Only ever filled in, never overwritten: the Hub writes this field at runtime.
            SerializedProperty weapon = so.FindProperty("weapon");
            if (weapon.objectReferenceValue == null && weapons.Count > 0)
            {
                weapon.objectReferenceValue = weapons[0];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log((isNew ? "Created " : "Refreshed ") + AssetPath + " with " + weapons.Count +
                      " weapon(s).", config);
        }

        private static List<WeaponDefinition> FindWeapons()
        {
            var found = new List<WeaponDefinition>();

            foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition",
                                                             new[] { "Assets/_Main/Data/Weapons" }))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));

                if (weapon != null) found.Add(weapon);
            }

            found.Sort((a, b) => a.WeaponType.CompareTo(b.WeaponType));
            return found;
        }
    }
}
