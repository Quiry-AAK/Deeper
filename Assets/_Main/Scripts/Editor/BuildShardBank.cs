using Deeper.Meta;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Creates <c>Data/Meta/ShardBank.asset</c> — the Hub's Shard total.
    ///
    /// Its own menu item rather than a right-click Create, for the reason <see cref="BuildRunConfig"/>
    /// has one: <see cref="BuildHubScene"/> wires the counter to a **fixed path**, and an asset the
    /// owner has to remember to create by hand in the right folder with the right name is an asset
    /// that will one day be somewhere else, leaving the Hub silently showing nothing.
    ///
    /// **Re-running it never touches the balance.** This is one of the two assets in the project a
    /// *running game* writes to, and resetting a player's currency on a rebuild of the scene would
    /// be the worst possible behaviour for a tool nobody expects to be destructive.
    /// </summary>
    public static class BuildShardBank
    {
        private const string Folder = "Assets/_Main/Data/Meta";
        private const string AssetPath = Folder + "/ShardBank.asset";

        [MenuItem("Deeper/Build Shard Bank")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_Main/Data", "Meta");
            }

            var bank = AssetDatabase.LoadAssetAtPath<ShardBank>(AssetPath);

            if (bank != null)
            {
                Debug.Log(AssetPath + " already exists; balance left at " + bank.Balance + ".", bank);
                return;
            }

            bank = ScriptableObject.CreateInstance<ShardBank>();
            AssetDatabase.CreateAsset(bank, AssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Zero, and that is the honest starting value: GDD §Currency awards Shards once at run
            // end and there is no run end yet, so anything else would be a number the game cannot
            // explain. Use the asset's "Grant 250 Shards" context menu to see the counter move.
            Debug.Log("Created " + AssetPath + " with a balance of 0.", bank);
        }
    }
}
