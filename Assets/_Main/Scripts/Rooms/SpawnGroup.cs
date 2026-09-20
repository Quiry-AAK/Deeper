using System;
using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// One enemy type, and how many of it arrive in a single batch.
    ///
    /// This lived as a private nested class inside <see cref="WaveSpawner"/> until encounters became
    /// content rather than prefab data. A room now draws its fight from an
    /// <see cref="EncounterDefinition"/> at mount time, and a private nested type cannot be authored
    /// from outside the component that owns it.
    ///
    /// The field names are unchanged, and that is load-bearing: Unity serialises a by-value
    /// [Serializable] class as an untyped nested mapping keyed by field name — no type token is
    /// written — so every room prefab authored against the nested version deserialises against this
    /// one untouched. Keep it a class rather than a struct (the spawner null-guards every element),
    /// and never put [SerializeReference] on a field holding it, which *would* write a type token
    /// and break exactly that compatibility.
    /// </summary>
    [Serializable]
    public sealed class SpawnGroup
    {
        [Tooltip("Enemy prefab. One pool is built per distinct prefab across every wave.")]
        public GameObject prefab;

        [Tooltip("How many of it in this batch.")]
        public int count = 1;
    }
}
