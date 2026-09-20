using System.Globalization;
using Deeper.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The Shard total, top-right of the camp — the first item on GDD §UI's Hub Screen list.
    ///
    /// **Hub only, deliberately.** ART_DIRECTION §5's in-run HUD is HP, XP and level, Ultimate,
    /// weapon, dash, wave and depth, and Shards are not on it — they are awarded once at run end and
    /// spent between runs, so a counter during a descent would be a number that cannot change
    /// watching a player who cannot spend it. It is built onto the Hub's canvas by
    /// <c>BuildHubScene</c> and exists nowhere else.
    ///
    /// It subscribes rather than polling in Update, unlike most of the run HUD. That is not
    /// stylistic: the run HUD's bars are reading values that change every frame, and this reads one
    /// that changes a handful of times per session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShardCounterHUD : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("The wallet. Wired by BuildHubScene.")]
        [SerializeField] private ShardBank bank;

        [Header("Widgets")]
        [SerializeField] private Text label;

        private void Awake()
        {
            LegacyUIFont.EnsureFont(label);
        }

        private void OnEnable()
        {
            if (bank != null) bank.BalanceChanged += Draw;

            // Drawn once on the way in as well as on every change, because the balance was set long
            // before this object existed — a purely event-driven counter shows an empty label until
            // the first transaction, which in this build would be never.
            Draw(bank != null ? bank.Balance : 0);
        }

        private void OnDisable()
        {
            if (bank != null) bank.BalanceChanged -= Draw;
        }

        private void Draw(int balance)
        {
            if (label == null) return;

            // Invariant culture, so the separator is a comma on every machine. The bitmap face has a
            // comma and no period-as-separator, and a German editor would otherwise render 1.250 in
            // a font whose period sits on the baseline — unreadable at this size.
            label.text = balance.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
