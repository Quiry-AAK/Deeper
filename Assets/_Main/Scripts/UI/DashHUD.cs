using Deeper.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The Dig-Dash cooldown pip.
    ///
    /// **This element is in no design doc.** GDD §UI lists HP, weapon icon, Ultimate Gauge, depth,
    /// XP + level, the Wave indicator and the Whisper line — there is no dash readout. It is added
    /// because the dash is the player's entire defensive option (BALANCE's mitigation line), and
    /// because its cooldown is a Hub Core Stat and two run upgrades modify it — a resource the
    /// player is asked to invest in has to be visible. Recorded in the change brief.
    ///
    /// Drawn as a radial wipe rather than a bar: the gauge next to it reads as *filling* toward a
    /// spend (ART_DIRECTION §5), and a second horizontal bar beside it would read as the same kind
    /// of thing. A dark wedge that clears away is a cooldown, and looks like one.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DashHUD : MonoBehaviour
    {
        [Header("Source — found by tag when empty")]
        [SerializeField] private DigDash dash;

        [Header("Widgets")]
        [Tooltip("The icon, full-bright when ready and dimmed while recharging.")]
        [SerializeField] private Image icon;

        [Tooltip("Image with Type = Filled, Fill Method = Radial 360. Draws the cooldown that is " +
                 "still to run, as a dark scrim over the icon — so a ready dash shows nothing.")]
        [SerializeField] private Image sweep;

        [Header("Colours")]
        [Tooltip("Plain white — Image.color multiplies, so anything else tints the icon's own art. " +
                 "This was raised off pale grey back when the glyph was flat drawn chevrons that " +
                 "vanished into the sweep behind them; the generated icon carries its own shading " +
                 "and separates on form instead of on brightness.")]
        [SerializeField] private Color ready = Color.white;
        [SerializeField] private Color recharging = new Color(0.42f, 0.44f, 0.52f, 1f);

        [Tooltip("Shown for the length of the i-frame window, so 'I was actually invulnerable " +
                 "then' is answerable — otherwise the only defensive mechanic in the game gives no " +
                 "feedback that it worked.")]
        [SerializeField] private Color invulnerable = new Color(0.72f, 0.70f, 0.92f, 1f);

        private void Awake()
        {
            if (dash == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) dash = player.GetComponentInChildren<DigDash>(true);
            }

            if (sweep != null)
            {
                sweep.type = Image.Type.Filled;
                sweep.fillMethod = Image.FillMethod.Radial360;
                sweep.fillOrigin = (int)Image.Origin360.Top;
            }
        }

        private void Update()
        {
            if (dash == null) return;

            float charge = dash.CooldownNormalized;   // 1 when ready, 0 the instant it is spent

            if (sweep != null)
            {
                // The cooldown REMAINING, not the charge. Assigning the charge straight through drew
                // a full disc whenever the dash was ready — which is nearly all the time — and
                // emptied it exactly while the cooldown was running. The slot read as permanently
                // tinted and the one moment it had something to say was the moment it went blank.
                sweep.fillAmount = 1f - charge;

                // Off rather than zero-filled: a ready dash should put nothing at all on screen.
                sweep.enabled = charge < 1f;
            }

            if (icon == null) return;

            if (dash.IsDashing) icon.color = invulnerable;
            else icon.color = dash.IsReady ? ready : recharging;
        }
    }
}
