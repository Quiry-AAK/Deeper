using System;
using UnityEngine;
using Deeper.Rooms;

namespace Deeper.Run
{
    /// <summary>
    /// One entry in a biome's room pool: a handcrafted layout, plus the fights that are allowed to
    /// happen in it.
    ///
    /// Which encounters suit which layout is **authored, not computed**. It would be possible to
    /// infer legality from a room's spawn-marker count, but there is nothing to infer: the spawner
    /// already reuses markers when a batch outnumbers them, so the only real constraint is a
    /// judgement about how crowded a given room should feel — and judgements belong in the
    /// Inspector where a designer can see and change them.
    /// </summary>
    [Serializable]
    public sealed class RoomOption
    {
        [Tooltip("The room prefab. Its geometry is hand-built and never generated (LEVEL_DESIGN §1).")]
        public GameObject prefab;

        [Tooltip("Fights that may be drawn for this layout. Leave empty to always use the " +
                 "encounter authored on the prefab itself.")]
        public EncounterDefinition[] encounters;
    }
}
