using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// The volume that springs the room. Walk into it and <see cref="CombatRoom"/> locks the doors
    /// and starts the fight.
    ///
    /// This is its own object because Unity delivers trigger callbacks to the GameObject carrying
    /// the collider, and the volume has to be *positioned* — it sits on the room's half-way line,
    /// not at the room's origin. Same shape as `AttackHitbox`: a child that owns its own volume.
    ///
    /// **This must not sit on the Default layer.** `ThrownRock.blockingLayers` is Default and the
    /// rock despawns on entering anything in it; the rock is itself a trigger carrying a kinematic
    /// rigidbody, so Unity fires the callback trigger-to-trigger. A Default-layer volume spanning
    /// the room would eat every rock a Rock Slinger throws across it, which reads as "the rocks
    /// randomly vanish" rather than as a layer mistake. Layer 8 `RoomTrigger` exists for this.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomEntry : MonoBehaviour
    {
        [Tooltip("Who springs the room. Layer 6 (Player) — the same filter ContactDamage uses. " +
                 "The whole player rig is on 6, so her aim reticle and hitbox can trip this too; " +
                 "harmless, because the room ignores everything but the first entry.")]
        [SerializeField] private LayerMask playerLayers = 1 << 6;

        [Header("Refs")]
        [Tooltip("The room this volume belongs to. Wired, not searched.")]
        [SerializeField] private CombatRoom room;

        private void Awake()
        {
            if (room == null) room = GetComponentInParent<CombatRoom>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TrySpring(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Stay as well as Enter, and this is a decision rather than belt-and-braces: re-arming
            // the room while the player is standing inside the volume would otherwise leave it
            // Armed forever, because she never crosses the boundary again. With Stay, re-arming
            // restarts the fight under her feet if she is on the line and waits for a walk-in if
            // she is not — both of which are what you want while tuning it.
            //
            // Her Rigidbody2D is Never Sleep (the same prefab setting ContactDamage depends on),
            // so Stay keeps arriving even when she stands perfectly still.
            TrySpring(other);
        }

        private void TrySpring(Collider2D other)
        {
            if (room == null || other == null) return;
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;

            room.PlayerEntered();
        }
    }
}
