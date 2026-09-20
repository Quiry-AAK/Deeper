using Deeper.Stats;
using Deeper.Upgrades;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Every upgrade and Curse the design docs currently name, as a table.
    ///
    /// Its own file because it is data, not logic — the same split
    /// <see cref="PixelFontGlyphs"/> and <see cref="RoomLayout"/> already make. Forty-six
    /// ScriptableObjects hand-edited as YAML is a content set nobody can review or diff; a table is
    /// one file where a wrong number is visible, and <see cref="BuildUpgradeAssets"/> turns it into
    /// assets that keep their GUIDs.
    ///
    /// <b>Values come from BALANCE §9 (shared), §10 (Katana) and §11 (Curses); names and tiers from
    /// CONTENT_DESIGN §1-§3.</b> Where the two disagree, BALANCE wins — it is the numbers doc and it
    /// is the one that was kept current. Three transcription traps, all recorded in the change brief:
    ///
    /// - CONTENT_DESIGN §1 still lists <b>Keen Eye, Lucky Find and Glimmer Magnet</b>. The run
    ///   currency was deleted; the live entries are Quick Study and Insight Magnet, and Lucky Find is
    ///   gone with the chests. BALANCE §9 already reflects this.
    /// - Katana's <b>Finisher+ and Echo Slash are struck through</b> in §2a and must not enter the
    ///   pool — the Combo Finisher they modified no longer exists. That leaves the Katana with 13
    ///   entries, not the 15 the section footer still claims.
    /// - §2's Bow and Greatsword sub-pools are <b>deliberately not authored here</b>. Neither weapon
    ///   is built, so every entry would reference a system that does not exist.
    ///
    /// <b>Only seven of these carry <see cref="StatModifier"/>s.</b> Everything else is behavioural —
    /// Thorns, Explosive Finish, every Curse — and needs hooks in the damage pipeline that are not
    /// built. Those land as data with a real name, description and icon and no effect, which is
    /// honest; expressing them as approximate stat modifiers would change the numbers by the wrong
    /// amount and look like they worked.
    ///
    /// <b>Card text must stay inside the HUD face's glyph set</b> (see <see cref="PixelFontGlyphs"/>):
    /// A-Z, 0-9, space and <c>. , : / % + - [ ] ! ? ( ) x ' &amp;</c>. An arrow or an em dash renders as
    /// a hole in the middle of a word and nothing catches it, which is why Momentum Edge reads
    /// "stack cap 10 to 14" here rather than the way BALANCE §10 writes it with an arrow.
    ///
    /// <b>And it must fit its box on the card</b>, all of which draw at the one font size, 21
    /// characters to a line, wrapping at spaces: a name gets <b>one</b> line, an upgrade's summary
    /// <b>five</b>, a Curse's upside <b>two</b> (its cost mark sits under them) and its cost line
    /// <b>two</b>. <see cref="BuildUpgradeAssets"/> warns on any entry that wraps past its box, and
    /// reads those budgets off <see cref="BuildUpgradePanel"/> rather than restating them.
    ///
    /// <b>Every entry also carries an <see cref="EffectSummary"/></b> (added for the icon-led card
    /// pass) — a category glyph plus a compressed line, extracted from the prose above rather than
    /// replacing it. <see cref="UpgradeStatusReport"/> and every other reader still goes through
    /// <c>Description</c>/<c>Upside</c>/<c>Downside</c>.
    ///
    /// <b>The Value/Detail split has one rule, and it follows from how the card joins them.</b>
    /// <c>UpgradeCard</c> draws <c>Detail + " " + Value</c>, so <b>the line can only end with the
    /// Value</b>:
    /// <list type="bullet">
    /// <item>One trailing number — keep the split. Detail is what changes, Value is the number:
    /// "Move speed" + "+10%".</item>
    /// <item>Two numbers, a word-shaped value, or a whole sentence — put the line in Detail and
    /// leave Value empty, which <c>DrawSummary</c> already handles.</item>
    /// </list>
    /// These pairs were originally authored for a <i>two-line</i> card, where a big Value sat above
    /// a small Detail and two numbers stayed visually apart. Collapsing them onto one line (owner,
    /// 2026-09-17) turned about twenty of them into nonsense — "Arcs within 3.0 4",
    /// "To knockback Immune", "Per hit, cap 5. Resets on a miss +2%" — and they were re-authored
    /// against this rule on 2026-09-18. <b>Prefer the trailing number wherever the line reads as
    /// English that way</b>; leading with it is the fallback, not the house style.
    /// </summary>
    internal static class UpgradeCatalog
    {
        /// <summary>Which draw an entry belongs to.</summary>
        public enum Pool
        {
            /// <summary>CONTENT_DESIGN §1 — offered whatever weapon is equipped.</summary>
            Shared,

            /// <summary>CONTENT_DESIGN §2a — only while the Katana is equipped.</summary>
            Katana,

            /// <summary>CONTENT_DESIGN §4 — never drawn. Guaranteed-drop only.</summary>
            Relic,
        }

        public struct Entry
        {
            public string Id;
            public string Name;
            public string Description;
            public string Icon;
            public UpgradeTier Tier;
            public Pool Pool;
            public StatModifier[] Modifiers;
            public EffectSummary Summary;

            /// <summary>Id of the upgrade this one extends, or null.</summary>
            public string Requires;

            /// <summary>Ids this entry cannot be taken alongside. Author both sides.</summary>
            public string[] Excludes;
        }

        public struct CurseEntry
        {
            public string Id;
            public string Name;
            public string Upside;
            public string Downside;
            public string Icon;
            public EffectSummary Summary;

            /// <summary>The card's short cost line — a compression of Downside, not Downside itself.</summary>
            public string CostLine;
        }

        private static StatModifier[] One(StatType stat, ModifierKind kind, float value)
        {
            return new[] { new StatModifier(stat, kind, value) };
        }

        private static readonly StatModifier[] None = new StatModifier[0];

        /// <summary>Builds one card's compressed line. A thin wrapper so every entry below reads as
        /// one line, mirroring <see cref="One"/>'s pattern for modifiers.</summary>
        private static EffectSummary Effect(UpgradeCategory category, string value, string detail)
        {
            return new EffectSummary { Category = category, Value = value, Detail = detail };
        }

        // ---------------------------------------------------------------- upgrades

        public static readonly Entry[] Upgrades =
        {
            // ---- Shared: Survivability (CONTENT_DESIGN §1, BALANCE §9) ----
            new Entry { Id = "Upgrade_Vitality", Name = "Vitality", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "+15 Max HP", Icon = "Upg_Vitality",
                        Modifiers = One(StatType.MaxHP, ModifierKind.Flat, 15f),
                        Summary = Effect(UpgradeCategory.Health, "+15", "Max HP") },

            new Entry { Id = "Upgrade_SecondWind", Name = "Second Wind", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "Heal 20% max HP on pickup", Icon = "Upg_SecondWind", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Health, "20%", "Heal on pickup") },

            new Entry { Id = "Upgrade_IronSkin", Name = "Iron Skin", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "-2 flat damage taken per hit", Icon = "Upg_IronSkin",
                        Modifiers = One(StatType.DamageReduction, ModifierKind.Flat, 2f),
                        Summary = Effect(UpgradeCategory.Defense, "-2", "Damage taken") },

            new Entry { Id = "Upgrade_Thorns", Name = "Thorns", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "Reflect 25% of damage taken", Icon = "Upg_Thorns", Modifiers = None,
                        // Tagged Defense, not Damage, even though it deals damage back — it follows
                        // §1's Survivability placement rather than what the number happens to do.
                        Summary = Effect(UpgradeCategory.Defense, "25%", "Reflected") },

            new Entry { Id = "Upgrade_Adrenaline", Name = "Adrenaline", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "Heal 8% max HP on Ultimate use", Icon = "Upg_Adrenaline", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Health, "8%", "Heal on Ultimate") },

            new Entry { Id = "Upgrade_LastStand", Name = "Last Stand", Tier = UpgradeTier.Epic, Pool = Pool.Shared,
                        Description = "-30% damage taken below 25% HP", Icon = "Upg_LastStand", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Defense, "-30%", "Damage below 25% HP") },

            // ---- Shared: Offense ----
            new Entry { Id = "Upgrade_HeavyHands", Name = "Heavy Hands", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "+3 flat damage, all attacks", Icon = "Upg_HeavyHands",
                        Modifiers = One(StatType.DamageBonus, ModifierKind.Flat, 3f),
                        Summary = Effect(UpgradeCategory.Damage, "+3", "All attacks") },

            new Entry { Id = "Upgrade_BleedingStrikes", Name = "Bleeding Strikes", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "3 damage per tick, 3 ticks, stacks to 3", Icon = "Upg_BleedingStrikes",
                        Modifiers = None, Excludes = new[] { "Upgrade_VenomEdge" },
                        // Per-tick x ticks, never the product (9) — the product publishes a total
                        // the design docs don't state (change brief flag).
                        Summary = Effect(UpgradeCategory.OnHit, "", "3 damage x 3 ticks, stacks to 3") },

            new Entry { Id = "Upgrade_Momentum", Name = "Momentum", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "+15% damage for 3s after a Dig-Dash", Icon = "Upg_Momentum", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Damage, "+15%", "3s after a Dig-Dash") },

            new Entry { Id = "Upgrade_Overwhelm", Name = "Overwhelm", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "+2% damage per hit in a row, cap 5. Resets on a miss",
                        Icon = "Upg_Overwhelm", Modifiers = None,
                        // Both "cap 5" and "resets on a miss" have to survive compression — either
                        // half missing misstates how the stack behaves (change brief flag).
                        Summary = Effect(UpgradeCategory.Damage, "", "+2% per hit, cap 5, resets on a miss") },

            new Entry { Id = "Upgrade_Executioner", Name = "Executioner", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "+20% damage to enemies below 25% HP", Icon = "Upg_Executioner", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Damage, "+20%", "Below 25% HP") },

            new Entry { Id = "Upgrade_ExplosiveFinish", Name = "Explosive Finish", Tier = UpgradeTier.Epic, Pool = Pool.Shared,
                        Description = "A killing blow deals 15 damage in a 2.0 radius",
                        Icon = "Upg_ExplosiveFinish", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Damage, "", "On kill, 15 damage in a 2.0 radius") },

            // ---- Shared: Mobility / Utility ----
            new Entry { Id = "Upgrade_FleetFoot", Name = "Fleet Foot", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "+10% move speed", Icon = "Upg_FleetFoot",
                        Modifiers = One(StatType.MoveSpeed, ModifierKind.Percent, 0.1f),
                        Summary = Effect(UpgradeCategory.Movement, "+10%", "Move speed") },

            new Entry { Id = "Upgrade_Quickstep", Name = "Quickstep", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "-20% Dig-Dash cooldown", Icon = "Upg_Quickstep",
                        Modifiers = One(StatType.DashCooldown, ModifierKind.Percent, -0.2f),
                        Summary = Effect(UpgradeCategory.Dash, "-20%", "Dig-Dash cooldown") },

            new Entry { Id = "Upgrade_LongDash", Name = "Long Dash", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "+25% Dig-Dash distance", Icon = "Upg_LongDash",
                        Modifiers = One(StatType.DashDistance, ModifierKind.Percent, 0.25f),
                        Summary = Effect(UpgradeCategory.Dash, "+25%", "Dig-Dash distance") },

            new Entry { Id = "Upgrade_PhaseStep", Name = "Phase Step", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "+0.1s of Dig-Dash invulnerability", Icon = "Upg_PhaseStep", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Dash, "+0.1s", "Dig-Dash invulnerability") },

            new Entry { Id = "Upgrade_BlinkStrike", Name = "Blink Strike", Tier = UpgradeTier.Epic, Pool = Pool.Shared,
                        Description = "Dig-Dash deals 12 damage to anything it passes through",
                        Icon = "Upg_BlinkStrike", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Dash, "", "Dig-Dash deals 12 damage passing through") },

            // ---- Shared: XP (BALANCE §9's renamed currency row) ----
            new Entry { Id = "Upgrade_QuickStudy", Name = "Quick Study", Tier = UpgradeTier.Common, Pool = Pool.Shared,
                        Description = "+20% XP from enemies", Icon = "Upg_QuickStudy",
                        Modifiers = One(StatType.OreGain, ModifierKind.Percent, 0.2f),
                        Summary = Effect(UpgradeCategory.Experience, "+20%", "XP from enemies") },

            new Entry { Id = "Upgrade_InsightMagnet", Name = "Insight Magnet", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "XP is pulled in from 3.0 units away", Icon = "Upg_InsightMagnet", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Experience, "3.0", "XP pull range") },

            // ---- Shared: On-Hit Procs ----
            new Entry { Id = "Upgrade_FrostTouch", Name = "Frost Touch", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "-30% enemy move speed for 1.5s on hit", Icon = "Upg_FrostTouch", Modifiers = None,
                        Summary = Effect(UpgradeCategory.OnHit, "", "-30% enemy move speed for 1.5s") },

            new Entry { Id = "Upgrade_VenomEdge", Name = "Venom Edge", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "2 damage per tick, 4 ticks, stacks to 5", Icon = "Upg_VenomEdge",
                        Modifiers = None, Excludes = new[] { "Upgrade_BleedingStrikes" },
                        Summary = Effect(UpgradeCategory.OnHit, "", "2 damage x 4 ticks, stacks to 5") },

            new Entry { Id = "Upgrade_StaticDischarge", Name = "Static Discharge", Tier = UpgradeTier.Epic, Pool = Pool.Shared,
                        Description = "4 damage arcs to one enemy within 3.0 units",
                        Icon = "Upg_StaticDischarge", Modifiers = None,
                        Summary = Effect(UpgradeCategory.OnHit, "", "4 damage arcs to one enemy within 3.0") },

            // ---- Shared: Situational ----
            new Entry { Id = "Upgrade_CurseSynergy", Name = "Curse Synergy", Tier = UpgradeTier.Rare, Pool = Pool.Shared,
                        Description = "+8% damage for each Curse taken this run", Icon = "Upg_CurseSynergy", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Damage, "+8%", "Per Curse taken") },

            new Entry { Id = "Upgrade_GamblersEdge", Name = "Gambler's Edge", Tier = UpgradeTier.Epic, Pool = Pool.Shared,
                        Description = "A 4th upgrade option for the rest of the run",
                        Icon = "Upg_GamblersEdge", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Utility, "", "4th card for the rest of the run") },

            // ---- Katana: Heavy Strike (CONTENT_DESIGN §2a, BALANCE §10) ----
            new Entry { Id = "Upgrade_TwinCut", Name = "Twin Cut", Tier = UpgradeTier.Common, Pool = Pool.Katana,
                        Description = "Heavy Strike becomes a 2-hit chain. 2nd hit: 12 damage",
                        Icon = "Upg_TwinCut", Modifiers = None,
                        Summary = Effect(UpgradeCategory.HeavyStrike, "", "2-hit chain, 2nd hit 12 damage") },

            new Entry { Id = "Upgrade_TripleCut", Name = "Triple Cut", Tier = UpgradeTier.Rare, Pool = Pool.Katana,
                        Description = "Extends the chain to 3 hits. 3rd hit: 12 damage",
                        Icon = "Upg_TripleCut", Modifiers = None, Requires = "Upgrade_TwinCut",
                        Summary = Effect(UpgradeCategory.HeavyStrike, "", "3-hit chain, 3rd hit 12 damage") },

            new Entry { Id = "Upgrade_ShadowStepSlash", Name = "Shadow Step Slash", Tier = UpgradeTier.Rare, Pool = Pool.Katana,
                        Description = "Heavy Strike blinks behind the target. Range 4.0, 28 damage",
                        Icon = "Upg_ShadowStepSlash", Modifiers = None,
                        Summary = Effect(UpgradeCategory.HeavyStrike, "", "Blinks behind, range 4.0, 28 damage") },

            // ---- Katana: Combo Counter ----
            new Entry { Id = "Upgrade_MomentumEdge", Name = "Momentum Edge", Tier = UpgradeTier.Common, Pool = Pool.Katana,
                        Description = "Combo stack cap 10 up to 14", Icon = "Upg_MomentumEdge", Modifiers = None,
                        // "10 to 14", not an arrow — the HUD face's glyph set has none (see class
                        // doc). Spelled out rather than "10-14", which reads as a range 10 THROUGH
                        // 14 rather than a cap raised from one to the other.
                        Summary = Effect(UpgradeCategory.Combo, "", "Stack cap 10 to 14") },

            new Entry { Id = "Upgrade_FlowState", Name = "Flow State", Tier = UpgradeTier.Rare, Pool = Pool.Katana,
                        Description = "Combo stacks survive taking damage. Still reset on a miss",
                        Icon = "Upg_FlowState", Modifiers = None,
                        // No clean single number — Value left empty rather than fabricating a
                        // keyword, Detail alone carrying the line. Once the lone entry doing this;
                        // now one of 22, see the class doc's split rule.
                        Summary = Effect(UpgradeCategory.Combo, "", "Combo survives damage, not a miss") },

            new Entry { Id = "Upgrade_RazorFocus", Name = "Razor Focus", Tier = UpgradeTier.Rare, Pool = Pool.Katana,
                        Description = "Combo stacks decay over 1.5s instead of resetting",
                        Icon = "Upg_RazorFocus", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Combo, "", "Stacks decay over 1.5s, no reset") },

            new Entry { Id = "Upgrade_ComboOverflow", Name = "Combo Overflow", Tier = UpgradeTier.Epic, Pool = Pool.Katana,
                        Description = "Stacks past the cap give +3% Ultimate gauge each",
                        Icon = "Upg_ComboOverflow", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Gauge, "+3%", "Gauge per overflow stack") },

            // ---- Katana: Ultimate Gauge ----
            new Entry { Id = "Upgrade_GaugeBloodrush", Name = "Gauge: Bloodrush", Tier = UpgradeTier.Common, Pool = Pool.Katana,
                        Description = "Basic Attack gauge gain +4% (12% total)",
                        Icon = "Upg_GaugeBloodrush", Modifiers = None,
                        // The grant leads and the running total trails it (change brief flag) —
                        // leading with 12% would overstate what this one pick gives.
                        Summary = Effect(UpgradeCategory.Gauge, "", "Basic Attack gauge +4%, 12% total") },

            new Entry { Id = "Upgrade_GaugeVengeance", Name = "Gauge: Vengeance", Tier = UpgradeTier.Common, Pool = Pool.Katana,
                        Description = "+2% gauge on taking damage (3% total)",
                        Icon = "Upg_GaugeVengeance", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Gauge, "", "Gauge on damage taken +2%, 3% total") },

            new Entry { Id = "Upgrade_GaugeAdrenalRush", Name = "Gauge: Adrenal Rush", Tier = UpgradeTier.Rare, Pool = Pool.Katana,
                        Description = "+50% gauge gain at 8 or more Combo stacks",
                        Icon = "Upg_GaugeAdrenalRush", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Gauge, "+50%", "At 8+ Combo stacks") },

            // ---- Katana: Alt Ultimate and build-definers ----
            new Entry { Id = "Upgrade_ThousandCuts", Name = "Thousand Cuts", Tier = UpgradeTier.Epic, Pool = Pool.Katana,
                        Description = "Alt Ultimate: a mobile flurry, with free movement throughout",
                        Icon = "Upg_ThousandCuts", Modifiers = None,
                        // CONTENT_DESIGN §2a calls this "Alt Ultimate"; tagged Ultimate here too —
                        // the one entry where the two axes agree.
                        Summary = Effect(UpgradeCategory.Ultimate, "", "Alt Ult: mobile flurry, move freely") },

            new Entry { Id = "Upgrade_Deathmark", Name = "Deathmark", Tier = UpgradeTier.Rare, Pool = Pool.Katana,
                        Description = "A marked target takes +25% from the next Heavy Strike",
                        Icon = "Upg_Deathmark", Modifiers = None,
                        // §2a calls this "Build-defining"; tagged HeavyStrike here — different axis,
                        // see the class doc comment and the change brief's taxonomy entry.
                        Summary = Effect(UpgradeCategory.HeavyStrike, "+25%", "To a marked target") },

            new Entry { Id = "Upgrade_Windcutter", Name = "Windcutter", Tier = UpgradeTier.Common, Pool = Pool.Katana,
                        Description = "+15% Basic Attack range, and it pierces 1 more enemy",
                        Icon = "Upg_Windcutter", Modifiers = None,
                        // §2a calls this "Build-defining"; tagged Damage here — different axis, see
                        // the class doc comment and the change brief's taxonomy entry.
                        Summary = Effect(UpgradeCategory.Damage, "", "Basic Attack range +15%, +1 pierce") },

            // ---- Relic (CONTENT_DESIGN §4, BALANCE §12). Never drawn; the Secret Vault grants it. ----
            new Entry { Id = "Upgrade_EndlessEdge", Name = "Endless Edge", Tier = UpgradeTier.Legendary, Pool = Pool.Relic,
                        Description = "The Combo Counter has no cap. Each stack gives 1%, not 2%",
                        Icon = "Upg_EndlessEdge", Modifiers = None,
                        Summary = Effect(UpgradeCategory.Combo, "", "No cap, 1% per stack not 2%") },
        };

        // ---------------------------------------------------------------- curses

        public static readonly CurseEntry[] Curses =
        {
            new CurseEntry { Id = "Curse_GlassCannon", Name = "Glass Cannon", Icon = "Curse_GlassCannon",
                             Upside = "+40% damage dealt", Downside = "Take double damage",
                             Summary = Effect(UpgradeCategory.Damage, "+40%", "Damage dealt"),
                             CostLine = "2x damage taken" },

            // BALANCE §11 flags this one as broken rather than tuned: its cost was a faster Rising
            // Hazard, and the Hazard was cut. The card says so instead of pretending it is a trade.
            // Kept close to Downside on purpose — shortening it to "No cost" would assert this Curse
            // is pure upside, which is a design claim engineering doesn't get to make. Trimmed from
            // "Cost pending - the Hazard it paid was cut", which wrapped to three lines in a
            // two-line box and drew across the card's bottom border; "its" keeps "it paid".
            new CurseEntry { Id = "Curse_GreedsToll", Name = "Greed's Toll", Icon = "Curse_GreedsToll",
                             Upside = "+200% XP from enemies", Downside = "Cost pending: the Hazard it paid was cut",
                             Summary = Effect(UpgradeCategory.Experience, "+200%", "XP from enemies"),
                             CostLine = "Cost pending: its Hazard was cut" },

            new CurseEntry { Id = "Curse_RecklessVigor", Name = "Reckless Vigor", Icon = "Curse_RecklessVigor",
                             Upside = "+50% Ultimate gauge gain", Downside = "Ultimate deals 25% less damage",
                             Summary = Effect(UpgradeCategory.Gauge, "+50%", "Ultimate gauge gain"),
                             CostLine = "-25% Ultimate damage" },

            new CurseEntry { Id = "Curse_FrailGrip", Name = "Frail Grip", Icon = "Curse_FrailGrip",
                             Upside = "+1 free Heavy Strike chain hit", Downside = "Heavy Strike gives no gauge",
                             Summary = Effect(UpgradeCategory.HeavyStrike, "+1", "Free chain hit"),
                             CostLine = "Heavy Strike gives no gauge" },

            new CurseEntry { Id = "Curse_BloodDebt", Name = "Blood Debt", Icon = "Curse_BloodDebt",
                             Upside = "Heal to full HP now", Downside = "-20% Max HP for the rest of the run",
                             Summary = Effect(UpgradeCategory.Health, "", "Heal to full HP now"),
                             CostLine = "-20% Max HP for the rest of the run" },

            new CurseEntry { Id = "Curse_IronCurse", Name = "Iron Curse", Icon = "Curse_IronCurse",
                             Upside = "Immune to knockback", Downside = "-15% move speed",
                             Summary = Effect(UpgradeCategory.Defense, "", "Immune to knockback"),
                             CostLine = "-15% move speed" },

            new CurseEntry { Id = "Curse_StarvingBlade", Name = "Starving Blade", Icon = "Curse_StarvingBlade",
                             Upside = "+1% damage per 1% HP missing, up to +60%", Downside = "Healing is halved",
                             Summary = Effect(UpgradeCategory.Damage, "", "+1% damage per 1% HP missing, max +60%"),
                             CostLine = "Healing is halved" },

            new CurseEntry { Id = "Curse_Overclock", Name = "Overclock", Icon = "Curse_Overclock",
                             Upside = "+20% attack speed", Downside = "Hyper Armor and Combo stability are off",
                             Summary = Effect(UpgradeCategory.Damage, "+20%", "Attack speed"),
                             CostLine = "Hyper Armor and Combo stability off" },
        };
    }
}
