using System.Collections.Generic;
using System.IO;
using Deeper.Character;
using Deeper.Stats;
using Deeper.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Turns <see cref="UpgradeCatalog"/> into authored assets: every upgrade, every Curse, the two
    /// pool assets and the Katana's sub-pool.
    ///
    /// <b>It updates in place and never replaces.</b> An asset's GUID is what every reference to it
    /// is made of — the seven upgrades that already exist are pointed at by the Player prefab's
    /// starting list and by the Katana's relic slot, and deleting and recreating them would turn
    /// those into missing references while leaving the file names identical. So a re-run edits the
    /// asset that is already there and only creates what is genuinely new. That also means it is
    /// safe to run repeatedly, which is the point: the table is the source, and this is how the table
    /// reaches the project.
    ///
    /// <b>Icons are assigned only when the PNG exists.</b> A missing icon leaves the field alone
    /// rather than clearing it, so running this before the art lands does not undo the run after.
    /// </summary>
    public static class BuildUpgradeAssets
    {
        private const string UpgradeFolder = "Assets/_Main/Data/Upgrades";
        private const string CurseFolder = "Assets/_Main/Data/Curses";
        private const string IconFolder = "Assets/_Main/Art/UI/Icons/";
        private const string KatanaPath = "Assets/_Main/Data/Weapons/Weapon_Katana.asset";
        private const string SharedPoolPath = UpgradeFolder + "/UpgradePool_Shared.asset";
        private const string CursePoolPath = CurseFolder + "/CursePool.asset";

        [MenuItem("Deeper/Build Upgrade Assets")]
        public static void Build()
        {
            EnsureFolder(UpgradeFolder);
            EnsureFolder(CurseFolder);

            var byId = new Dictionary<string, UpgradeDefinition>();
            var shared = new List<UpgradeDefinition>();
            var katana = new List<UpgradeDefinition>();
            int missingIcons = 0;
            int badText = 0;

            // Pass one writes the fields that stand alone.
            for (int i = 0; i < UpgradeCatalog.Upgrades.Length; i++)
            {
                UpgradeCatalog.Entry entry = UpgradeCatalog.Upgrades[i];
                UpgradeDefinition asset = LoadOrCreate<UpgradeDefinition>(UpgradeFolder + "/" + entry.Id + ".asset");

                var so = new SerializedObject(asset);
                so.FindProperty("id").stringValue = entry.Id;
                so.FindProperty("displayName").stringValue = entry.Name;
                so.FindProperty("description").stringValue = entry.Description;
                so.FindProperty("tier").enumValueIndex = (int)entry.Tier;

                Sprite icon = Icon(entry.Icon);
                if (icon != null) so.FindProperty("icon").objectReferenceValue = icon;
                else missingIcons++;

                WriteModifiers(so.FindProperty("modifiers"), entry.Modifiers);
                WriteSummary(so.FindProperty("summary"), entry.Summary);
                badText += CheckCardText(entry.Name, "name", entry.Name, 1);
                badText += CheckCardText(entry.Name, "summary", entry.Summary.Line, BuildUpgradePanel.SummaryLines);
                so.ApplyModifiedPropertiesWithoutUndo();

                byId[entry.Id] = asset;

                switch (entry.Pool)
                {
                    case UpgradeCatalog.Pool.Shared: shared.Add(asset); break;
                    case UpgradeCatalog.Pool.Katana: katana.Add(asset); break;

                    // Relics are authored but never pooled — CONTENT_DESIGN §4 makes them
                    // guaranteed-drop only, and the weapon asset already names its own.
                    default: break;
                }
            }

            // Pass two resolves the references between entries, which need every asset to exist.
            for (int i = 0; i < UpgradeCatalog.Upgrades.Length; i++)
            {
                UpgradeCatalog.Entry entry = UpgradeCatalog.Upgrades[i];

                var so = new SerializedObject(byId[entry.Id]);
                so.FindProperty("requires").objectReferenceValue = Lookup(byId, entry.Requires, entry.Id);

                SerializedProperty excludes = so.FindProperty("excludes");
                string[] ids = entry.Excludes ?? new string[0];
                excludes.arraySize = ids.Length;

                for (int e = 0; e < ids.Length; e++)
                {
                    excludes.GetArrayElementAtIndex(e).objectReferenceValue = Lookup(byId, ids[e], entry.Id);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var curses = new List<CurseDefinition>();

            for (int i = 0; i < UpgradeCatalog.Curses.Length; i++)
            {
                UpgradeCatalog.CurseEntry entry = UpgradeCatalog.Curses[i];
                CurseDefinition asset = LoadOrCreate<CurseDefinition>(CurseFolder + "/" + entry.Id + ".asset");

                var so = new SerializedObject(asset);
                so.FindProperty("id").stringValue = entry.Id;
                so.FindProperty("displayName").stringValue = entry.Name;
                so.FindProperty("upside").stringValue = entry.Upside;
                so.FindProperty("downside").stringValue = entry.Downside;

                Sprite icon = Icon(entry.Icon);
                if (icon != null) so.FindProperty("icon").objectReferenceValue = icon;
                else missingIcons++;

                WriteSummary(so.FindProperty("upsideSummary"), entry.Summary);
                so.FindProperty("costLine").stringValue = entry.CostLine ?? string.Empty;
                badText += CheckCardText(entry.Name, "name", entry.Name, 1);
                badText += CheckCardText(entry.Name, "upside", entry.Summary.Line, BuildUpgradePanel.CurseUpsideLines);
                badText += CheckCardText(entry.Name, "cost line", entry.CostLine, BuildUpgradePanel.CostLines);
                so.ApplyModifiedPropertiesWithoutUndo();

                curses.Add(asset);
            }

            BuildSharedPool(shared);
            BuildCursePool(curses);
            WireKatana(katana);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Built " + UpgradeCatalog.Upgrades.Length + " upgrades (" + shared.Count +
                      " shared, " + katana.Count + " Katana) and " + curses.Count + " Curses." +
                      (missingIcons > 0
                          ? "  " + missingIcons + " still have no icon in " + IconFolder +
                            " — the card falls back to its tier-coloured frame until the art lands."
                          : string.Empty) +
                      (badText > 0
                          ? "  " + badText + " card-text problem(s) logged above — see the warnings."
                          : string.Empty));
        }

        private static void BuildSharedPool(List<UpgradeDefinition> shared)
        {
            UpgradePool pool = LoadOrCreate<UpgradePool>(SharedPoolPath);

            var so = new SerializedObject(pool);
            SerializedProperty entries = so.FindProperty("shared");
            entries.arraySize = shared.Count;
            for (int i = 0; i < shared.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = shared[i];

            // BALANCE §13, one row per biome: Common / Rare / Epic.
            SerializedProperty weights = so.FindProperty("weightsPerBiome");
            weights.arraySize = 3;
            SetWeights(weights.GetArrayElementAtIndex(0), 65f, 30f, 5f);    // Upper Caves
            SetWeights(weights.GetArrayElementAtIndex(1), 55f, 35f, 10f);   // Flooded Tunnels
            SetWeights(weights.GetArrayElementAtIndex(2), 45f, 40f, 15f);   // Molten Depths

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetWeights(SerializedProperty row, float common, float rare, float epic)
        {
            row.FindPropertyRelative("Common").floatValue = common;
            row.FindPropertyRelative("Rare").floatValue = rare;
            row.FindPropertyRelative("Epic").floatValue = epic;
        }

        private static void BuildCursePool(List<CurseDefinition> curses)
        {
            CursePool pool = LoadOrCreate<CursePool>(CursePoolPath);

            var so = new SerializedObject(pool);
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = curses.Count;
            for (int i = 0; i < curses.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = curses[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireKatana(List<UpgradeDefinition> katana)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(KatanaPath);
            if (weapon == null)
            {
                Debug.LogWarning("No Katana at " + KatanaPath + " — its sub-pool was not wired.");
                return;
            }

            var so = new SerializedObject(weapon);
            SerializedProperty entries = so.FindProperty("weaponUpgrades");
            entries.arraySize = katana.Count;
            for (int i = 0; i < katana.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = katana[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteSummary(SerializedProperty prop, EffectSummary summary)
        {
            prop.FindPropertyRelative("Category").enumValueIndex = (int)summary.Category;
            prop.FindPropertyRelative("Value").stringValue = summary.Value ?? string.Empty;
            prop.FindPropertyRelative("Detail").stringValue = summary.Detail ?? string.Empty;
        }

        /// <summary>
        /// The icon-led card's text guard, reported alongside <c>missingIcons</c>: a wrong glyph or
        /// an overlong line fails silently at runtime — the HUD face has no fallback glyph, and
        /// wrapping is UGUI's problem, not a warning — so this is the only place either gets caught.
        ///
        /// <b>It counts wrapped lines, not characters.</b> It used to allow 42 characters as "two
        /// lines of 21", and Greed's Toll's 41-character cost line passed while wrapping to three —
        /// a line breaks at a space, so a word that does not fit the end of one line takes its whole
        /// length onto the next. That third line drew across the card's bottom border.
        /// <paramref name="maxLines"/> comes from <see cref="BuildUpgradePanel"/>, which owns the boxes.
        /// </summary>
        private static int CheckCardText(string owner, string field, string text, int maxLines)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int problems = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (AllowedGlyphs.Contains(char.ToUpperInvariant(text[i]))) continue;

                Debug.LogWarning("UpgradeCatalog: " + owner + "'s " + field + " '" + text +
                                  "' has a character the HUD face cannot draw ('" + text[i] + "').");
                problems++;
                break;
            }

            int lineChars = BuildUpgradePanel.LineChars;
            string[] words = text.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length <= lineChars) continue;

                Debug.LogWarning("UpgradeCatalog: " + owner + "'s " + field + " has a " +
                                  words[i].Length + "-char word ('" + words[i] + "'), wider than " +
                                  "the card's " + lineChars + "-char line.");
                problems++;
                break;
            }

            int lines = WrappedLines(words, lineChars);

            if (lines > maxLines)
            {
                Debug.LogWarning("UpgradeCatalog: " + owner + "'s " + field + " '" + text +
                                  "' wraps to " + lines + " lines; its box on the card holds " +
                                  maxLines + ".");
                problems++;
            }

            return problems;
        }

        /// <summary>
        /// How many lines <paramref name="words"/> take in a card text box. UGUI's own rule, and
        /// plain counting because the face is monospaced: a word joins the current line with its
        /// space if both fit, otherwise it starts the next one.
        /// </summary>
        private static int WrappedLines(string[] words, int lineChars)
        {
            int lines = 1;
            int used = 0;

            for (int i = 0; i < words.Length; i++)
            {
                int length = words[i].Length;

                if (used == 0) used = length;
                else if (used + 1 + length <= lineChars) used += 1 + length;
                else
                {
                    lines++;
                    used = length;
                }
            }

            return lines;
        }

        /// <summary>Every character the HUD face can draw, folded to one case — see
        /// <see cref="PixelFontGlyphs.Order"/>. Built once rather than per-call.</summary>
        private static readonly HashSet<char> AllowedGlyphs = BuildAllowedGlyphs();

        private static HashSet<char> BuildAllowedGlyphs()
        {
            var set = new HashSet<char>();
            string order = PixelFontGlyphs.Order;
            for (int i = 0; i < order.Length; i++) set.Add(char.ToUpperInvariant(order[i]));
            return set;
        }

        private static void WriteModifiers(SerializedProperty prop, StatModifier[] rows)
        {
            prop.arraySize = rows != null ? rows.Length : 0;
            if (rows == null) return;

            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("stat").enumValueIndex = (int)rows[i].Stat;
                element.FindPropertyRelative("kind").enumValueIndex = (int)rows[i].Kind;
                element.FindPropertyRelative("value").floatValue = rows[i].Value;
            }
        }

        private static UpgradeDefinition Lookup(Dictionary<string, UpgradeDefinition> byId, string id, string owner)
        {
            if (string.IsNullOrEmpty(id)) return null;

            UpgradeDefinition found;
            if (byId.TryGetValue(id, out found)) return found;

            Debug.LogWarning("UpgradeCatalog: " + owner + " references '" + id + "', which is not in the table.");
            return null;
        }

        private static Sprite Icon(string file)
        {
            return string.IsNullOrEmpty(file)
                ? null
                : AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + file + ".png");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
