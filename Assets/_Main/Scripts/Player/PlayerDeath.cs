using Deeper.Animation;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Player
{
    /// <summary>
    /// Ends her. Plays the Death clip, stops her moving, aiming and attacking, and takes her out of
    /// the fight.
    ///
    /// This closes the oldest gap in the project. `Damageable.Died` has fired on the player since
    /// the training-dummy pass with **nothing subscribed**: at 0 HP she kept her collider, kept her
    /// input, and got shoved around a locked room by enemies that went on hitting her, in a game
    /// with no way to lose.
    ///
    /// **It ends her and nothing else.** What the *run* does about it — the Shard award, the summary
    /// screen, the trip back to the Hub — is <see cref="Deeper.Run.RunEnd"/>'s, which subscribes to
    /// the same event from the scene. Two subscribers rather than one call chain, because a death in
    /// the sandbox has a body and no run: `TestScene` gets the corpse and none of the ceremony.
    ///
    /// Mirrors <see cref="Deeper.Enemies.EnemyDeath"/>, minus the pooling — she is never released,
    /// because a run ends with her body on the floor and a screen over it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDeath : MonoBehaviour
    {
        [Tooltip("Seconds the Death clip is played over. Longer than an enemy's 0.45 despawn " +
                 "because nothing removes her at the end of it — the last frame is what the " +
                 "run-end screen opens on top of.")]
        [SerializeField] private float deathClipSeconds = 1.2f;

        [Header("Refs — wired in the Inspector, resolved on this object when empty")]
        [Tooltip("Hers, on the rig root beside the collider. GetComponentInParent, never " +
                 "RigRefs.Find: Find searches from transform.root, and if the player were ever " +
                 "parented to anything it would resolve against a sibling actor instead.")]
        [SerializeField] private Damageable health;

        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private SpriteAnimationView view;

        [Tooltip("Switched off the moment she dies, so a corpse stops being shoved around the room " +
                 "and stops soaking contact damage that would re-raise Died.")]
        [SerializeField] private Collider2D body;

        [Tooltip("Switched off on death. Left running, she walks, swings and dashes through her " +
                 "own death animation, and the run-end screen opens over a fight she is still in.")]
        [SerializeField] private PlayerController movement;

        [SerializeField] private AttackStateMachine attacks;
        [SerializeField] private DigDash dash;

        [Tooltip("Switched off so the reticle stops tracking the mouse over a body. It also hands " +
                 "the hardware cursor back, which the run-end screen needs to be clickable.")]
        [SerializeField] private PlayerAim aim;

        private bool _dead;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<Damageable>();
            if (body == null) body = GetComponentInParent<Collider2D>();
            if (movement == null) movement = GetComponentInParent<PlayerController>();

            // These three legitimately live in other groups on the rig, and RigRefs is how anything
            // reaches across one (CLAUDE.md). Safe here where EnemyDeath's optional-field hazard is
            // not: every one of them is always present on the player, so a search can never come
            // back with a sibling actor's.
            characterAnimator = RigRefs.Find(this, characterAnimator);
            view = RigRefs.Find(this, view);
            attacks = RigRefs.Find(this, attacks);
            dash = RigRefs.Find(this, dash);
            aim = RigRefs.Find(this, aim);
        }

        private void OnEnable()
        {
            if (health != null) health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            // Guarded, unlike EnemyDeath's. An enemy is released on death and stops taking hits;
            // she is not, and two contact sources landing on the same frame would otherwise restart
            // the clip and re-raise the run end behind the summary already on screen.
            if (_dead) return;
            _dead = true;

            if (body != null) body.enabled = false;
            if (movement != null) movement.enabled = false;
            if (attacks != null) attacks.enabled = false;
            if (dash != null) dash.enabled = false;

            // Aim last and separately: disabling it is what restores the hardware cursor, and the
            // summary screen behind this is a UGUI panel with buttons on it.
            if (aim != null) aim.enabled = false;

            PlayDeathClip();
        }

        /// <summary>
        /// Puts her back on her feet. The seam a respawn would use, and what the sandbox's
        /// `Damageable.Refill` needs to be worth pressing — without it a death in `TestScene` is
        /// permanent until Play is stopped.
        /// </summary>
        [ContextMenu("Revive")]
        public void Revive()
        {
            _dead = false;

            if (body != null) body.enabled = true;
            if (movement != null) movement.enabled = true;
            if (attacks != null) attacks.enabled = true;
            if (dash != null) dash.enabled = true;
            if (aim != null) aim.enabled = true;

            if (characterAnimator != null) characterAnimator.CancelAction();
            if (health != null) health.Refill();
        }

        private void PlayDeathClip()
        {
            if (characterAnimator == null) return;

            int frames = view != null && view.AnimationSet != null
                ? view.AnimationSet.FrameCount(CharacterState.Death, characterAnimator.Facing)
                : 0;

            // No frames is the normal case today: ART_DIRECTION §3's death art does not exist, and
            // CharacterState.Death falls back to Idle so she stands rather than vanishing. Playing a
            // zero-length clip would divide the duration by nothing.
            if (frames <= 0) return;

            characterAnimator.PlayAction(CharacterState.Death, deathClipSeconds, frames);
        }
    }
}
