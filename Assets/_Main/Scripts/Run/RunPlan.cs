using System;
using UnityEngine;

namespace Deeper.Run
{
    /// <summary>
    /// The shape of a whole descent: which biome each floor is drawn from, and which floor is the
    /// last one.
    ///
    /// GDD §Game Loop 3 spells the run out — "Biome 1 (floors 1–5) → Mini-Boss 1 → Biome 2 (6–10) →
    /// Mini-Boss 2 → Biome 3 (11–15) → Mini-Boss 3 → Floor 16: Final Boss". <see cref="FloorLoader"/>
    /// used to hold a single <see cref="BiomeRoomPool"/>, which can express floors but not biomes,
    /// so a run had no end and never changed biome.
    ///
    /// **An asset, so the run's shape is content.** Fifteen floors of Upper Caves for testing, or
    /// three floors to check a Mini-Boss, is a number in the Inspector rather than a recompile — the
    /// same reason every other table in this project is data (Design Rule 8).
    /// </summary>
    [CreateAssetMenu(fileName = "RunPlan", menuName = "Deeper/Run/Run Plan", order = 12)]
    public sealed class RunPlan : ScriptableObject
    {
        /// <summary>One biome's stretch of the descent.</summary>
        [Serializable]
        public sealed class Stage
        {
            [Tooltip("The rooms, themes and bosses these floors draw from.")]
            public BiomeRoomPool pool;

            [Tooltip("The last floor this biome covers, 1-based and inclusive. Upper Caves is 5.")]
            public int throughFloor = 5;
        }

        [Tooltip("In descent order, shallowest first. Each stage runs from just after the previous " +
                 "stage's last floor through its own.")]
        [SerializeField] private Stage[] stages = new Stage[0];

        /// <summary>
        /// The floor a run ends on — **derived from the last stage**, never a second serialized
        /// number. A typed `finalFloor` beside this array is a second source of truth that can
        /// disagree with it, which is the trap `CombatRoom.IsWaveRoom` and
        /// `EncounterDefinition.TotalHealth` both already avoid by deriving instead of storing.
        /// </summary>
        public int FinalFloor
        {
            get
            {
                int last = 0;
                if (stages == null) return 0;

                foreach (Stage stage in stages)
                {
                    if (stage != null && stage.throughFloor > last) last = stage.throughFloor;
                }

                return last;
            }
        }

        /// <summary>
        /// The biome floor <paramref name="floor"/> is drawn from, or null if the plan has no stages.
        ///
        /// A floor past the end clamps to the deepest stage rather than returning null. That case is
        /// only reachable by a probe driving the loader past the final floor, and giving it the last
        /// biome's rooms is a far more useful answer than a null reference.
        /// </summary>
        public BiomeRoomPool PoolFor(int floor)
        {
            if (stages == null || stages.Length == 0) return null;

            foreach (Stage stage in stages)
            {
                if (stage != null && stage.pool != null && floor <= stage.throughFloor) return stage.pool;
            }

            for (int i = stages.Length - 1; i >= 0; i--)
            {
                if (stages[i] != null && stages[i].pool != null) return stages[i].pool;
            }

            return null;
        }
    }
}
