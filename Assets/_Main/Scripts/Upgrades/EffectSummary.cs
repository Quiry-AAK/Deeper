namespace Deeper.Upgrades
{
    /// <summary>
    /// The icon-led offer card's effect taxonomy — twelve glyphs standing in for the prose an
    /// upgrade or Curse used to spell out in full.
    ///
    /// <b>This is an effect taxonomy, not <c>CONTENT_DESIGN</c> §2a's pool-organisation
    /// <c>Category</c> column</b> — a different axis, and they disagree on purpose for three
    /// entries (Windcutter, Deathmark, Thousand Cuts). §2a asks "which pool/sub-pool is this
    /// drawn from"; this asks "what does picking it up change on screen". See the change brief's
    /// entry on this taxonomy for the full mapping and the three disagreements.
    /// </summary>
    public enum UpgradeCategory
    {
        Health,
        Defense,
        Damage,
        OnHit,
        Movement,
        Dash,
        Experience,
        HeavyStrike,
        Combo,
        Gauge,
        Ultimate,
        Utility,
    }

    /// <summary>
    /// The compressed line a card shows in place of its full prose description: which glyph, the
    /// big number (or short token), and the short line under it.
    ///
    /// Extracted from the authored <c>description</c>/<c>upside</c> text, not a replacement for
    /// it — <see cref="UpgradeDefinition.Description"/> and <see cref="CurseDefinition.Upside"/>
    /// stay verbatim and are still what <c>UpgradeStatusReport</c> reads.
    /// </summary>
    [System.Serializable]
    public struct EffectSummary
    {
        /// <summary>Which glyph the card draws.</summary>
        public UpgradeCategory Category;

        /// <summary>
        /// The big line — "+15", "25%", "2-Hit". Left empty (not a fabricated keyword) for the
        /// handful of entries with no clean single number; the <see cref="Detail"/> line carries
        /// the meaning for those.
        /// </summary>
        public string Value;

        /// <summary>The small line under the value — "Max HP", "Reflected".</summary>
        public string Detail;

        /// <summary>
        /// The one line the card actually draws: Detail, then Value, so it reads as a sentence that
        /// ends on its number — "All attacks +3". Either half may be empty.
        ///
        /// Here rather than in <c>UpgradeCard</c> so <c>BuildUpgradeAssets</c>'s fit check measures
        /// exactly the string that gets drawn; two copies of the join would drift apart the way
        /// <c>CharacterState.FallbackArt</c>'s two copies once did.
        /// </summary>
        public string Line
        {
            get
            {
                string detail = Detail ?? string.Empty;
                string value = Value ?? string.Empty;

                if (detail.Length == 0) return value;
                if (value.Length == 0) return detail;
                return detail + " " + value;
            }
        }
    }
}
