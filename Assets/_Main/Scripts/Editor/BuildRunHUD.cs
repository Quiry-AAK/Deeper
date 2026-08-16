using Deeper.Character;
using Deeper.Combat;
using Deeper.Player;
using Deeper.Rooms;
using Deeper.UI;
using Deeper.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds the in-run HUD canvas: HP, XP + level, Ultimate Gauge, weapon icon, Dig-Dash pip,
    /// wave indicator and depth readout, laid out per ART_DIRECTION §5.
    ///
    /// **The layout lives here rather than in hand-dragged RectTransforms** for the same reason
    /// <see cref="RoomLayout"/> holds the room's map: a HUD assembled by dragging is a layout nobody
    /// can reproduce, review or diff, and every retune means finding the same six anchors again.
    /// The numbers below are the layout, and re-running the menu item rebuilds it exactly.
    ///
    /// Idempotent — it deletes the previously generated root and rebuilds, so it is safe to run
    /// repeatedly while tuning.
    ///
    /// ART_DIRECTION §5 corners: HP top-left, XP + level top-right, Ultimate Gauge and weapon icon
    /// bottom-centre. The dash pip sits beside the gauge; GDD §UI does not list it (see the change
    /// brief) but it belongs with the other things the player spends.
    /// </summary>
    public static class BuildRunHUD
    {
        private const string CanvasName = "HUDCanvas";
        private const string RootName = "RunHUD";
        private const string ArtFolder = "Assets/_Main/Art/UI/";

        /// <summary>
        /// The screen height at which the HUD draws at 1x — so at the 1080p design target the
        /// canvas scales it **2x**, and every number in this file is half its on-screen size.
        ///
        /// Authoring at half size is what keeps the HUD from dominating a small window. Drawn 1:1
        /// the factor is forced to 1 everywhere below 1080, which made the health bar a third of the
        /// width of a 906px editor Game view instead of a sixth. See
        /// <see cref="Deeper.UI.PixelPerfectHUDScale"/> for why the factor has to be a whole number
        /// in the first place.
        /// </summary>
        private const int ReferenceHeight = 540;

        /// <summary>Kept only for the CanvasScaler's own field; the scale mode ignores it.</summary>
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private const float Margin = 14f;

        /// <summary>
        /// The pixel font's native size (see <see cref="PixelFontArt"/>), so UGUI scales nothing.
        /// </summary>
        private const int BodyText = 7;   // 14 on screen

        /// <summary>Exactly 2x native — the one size that stays on the grid when scaled up.</summary>
        private const int TitleText = 14;   // 28 on screen

        /// <summary>
        /// The border widths <see cref="HUDFrameArt"/> draws its two slots at. Anything inset to
        /// sit *inside* a slot — a socket, a cooldown sweep, an icon — has to match, or it either
        /// slides under the frame or leaves a gap of world showing through the hole.
        /// </summary>
        private const float SlotBorder = 4f;
        private const float UpgradeSlotBorder = 2f;

        /// <summary>
        /// The chase bar's colour, written to both its Image and to <c>StatBar.ghostColor</c> —
        /// <c>StatBar</c> re-applies it on Awake, so a single value here is what keeps the scene
        /// view and play mode showing the same bar.
        /// </summary>
        private static readonly Color ChaseColour = new Color(0.69f, 0.52f, 0.54f, 1f);

        [MenuItem("Deeper/Build Run HUD")]
        public static void Build()
        {
            Canvas canvas = FindOrCreateCanvas();

            Transform existing = canvas.transform.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            RectTransform root = NewRect(RootName, canvas.transform);
            Stretch(root);

            BuildHealth(root);
            BuildUpgrades(root);
            BuildExperience(root);
            BuildUltimate(root);
            BuildWeaponAndDash(root);
            BuildWaveIndicator(root);

            EditorUtility.SetDirty(canvas.gameObject);
            Debug.Log("Built the run HUD under " + CanvasName + "/" + RootName + ".", canvas);
            Selection.activeGameObject = root.gameObject;
        }

        // ---------------------------------------------------------------- elements

        private static void BuildHealth(RectTransform parent)
        {
            // HUD_BarSlim, not the generated HUD_BarLarge it replaces: at 448x129 that piece was a
            // fifth of the screen wide and an eighth of it tall, which is what "too big" meant.
            // This one is 160x18 authored, which is the same 320x36 on screen at the canvas's 2x —
            // the chrome pass that followed added material, not size.
            const string Art = "HUD_BarSlim";
            Vector2 size = SizeOf(Load(Art), new Vector2(160f, 18f));

            RectTransform group = NewRect("Health", parent);
            AnchorTopLeft(group, size, new Vector2(Margin, -Margin));

            // The one bar with a chase bar. HP is the only value here that falls, and the only one
            // where how big the drop was matters more than where it landed.
            StatBar bar = BuildBar(group, Art, new Color(0.62f, 0.16f, 0.22f, 1f), withGhost: true);

            var hud = group.gameObject.AddComponent<HealthBarHUD>();
            Wire(hud, "bar", bar);
            Wire(hud, "health", PlayerPart<Damageable>());
        }

        private static void BuildExperience(RectTransform parent)
        {
            const string Art = "HUD_BarSlimXP";
            Sprite badge = Load("HUD_SlotHex");
            Vector2 size = SizeOf(Load(Art), new Vector2(120f, 11f));
            Vector2 badgeSize = SizeOf(badge, new Vector2(28f, 28f));

            RectTransform group = NewRect("Experience", parent);
            AnchorTopRight(group, size, new Vector2(-Margin - badgeSize.x - 6f, -Margin));

            // No chase bar: XP only ever climbs, so there is nothing behind the fill to show.
            StatBar bar = BuildBar(group, Art, new Color(0.44f, 0.62f, 0.44f, 1f), withGhost: false);

            // The badge is a sibling of the bar's group so the bar's own rect stays exactly the
            // frame art — anchoring the badge inside it would make the fill inset arithmetic lie.
            RectTransform badgeRect = NewRect("LevelBadge", parent);
            AnchorTopRight(badgeRect, badgeSize, new Vector2(-Margin, -Margin + 7f));
            AddImage(badgeRect, badge, Color.white);

            Text levelLabel = AddText(badgeRect, "1", BodyText, TextAnchor.MiddleCenter);
            levelLabel.color = new Color(0.85f, 0.83f, 0.78f, 1f);

            // Depth sits under the XP bar, the only free corner left once §5's three are placed.
            RectTransform depth = NewRect("Depth", parent);
            AnchorTopRight(depth, new Vector2(size.x, 14f), new Vector2(-Margin - badgeSize.x - 6f, -Margin - size.y - 3f));
            Text depthLabel = AddText(depth, "FLOOR 1 / 16", BodyText, TextAnchor.MiddleRight);
            depthLabel.color = new Color(0.66f, 0.64f, 0.62f, 1f);

            var depthHud = depth.gameObject.AddComponent<DepthIndicatorHUD>();
            Wire(depthHud, "label", depthLabel);

            var hud = group.gameObject.AddComponent<ExperienceBarHUD>();
            Wire(hud, "bar", bar);
            Wire(hud, "levelLabel", levelLabel);
            Wire(hud, "experience", PlayerPart<PlayerXP>());
        }

        private static void BuildUltimate(RectTransform parent)
        {
            const string Art = "HUD_BarSlimUltimate";
            Vector2 size = SizeOf(Load(Art), new Vector2(150f, 15f));

            RectTransform group = NewRect("Ultimate", parent);
            AnchorBottomCentre(group, size, new Vector2(0f, Margin));

            BarPieces pieces = BuildBarVisuals(group, Art, new Color(0.42f, 0.55f, 0.72f, 1f),
                                               withGhost: false);

            // The shipped UltimateGaugeHUD already owns the gauge's full-pulse (ART_DIRECTION §6's
            // must-have VFX) and the combo readout, so it is kept and re-skinned rather than
            // rewritten. It is the sole driver of this fill — no StatBar here.
            var hud = group.gameObject.AddComponent<UltimateGaugeHUD>();
            Wire(hud, "fillImage", pieces.Fill);
            Wire(hud, "label", pieces.Label);
            Wire(hud, "gauge", PlayerPart<UltimateGauge>());
            Wire(hud, "combo", PlayerPart<ComboCounter>());

            RectTransform combo = NewRect("Combo", group);
            AnchorBottomCentre(combo, new Vector2(size.x, 13f), new Vector2(0f, size.y + 2f));
            Text comboLabel = AddText(combo, string.Empty, BodyText, TextAnchor.MiddleCenter);
            comboLabel.color = new Color(0.85f, 0.65f, 0.40f, 1f);
            Wire(hud, "comboLabel", comboLabel);
        }

        private static void BuildWeaponAndDash(RectTransform parent)
        {
            Sprite squareSlot = Load("HUD_SlotSquare");
            Sprite fill = Load("HUD_Fill");
            Vector2 slotSize = SizeOf(squareSlot, new Vector2(40f, 40f));

            float gaugeHalf = SizeOf(Load("HUD_BarSlimUltimate"), new Vector2(150f, 15f)).x * 0.5f;

            // **Dash left, weapon right** (owner-directed — this pair used to be the other way
            // round). Reading left to right the row is now the thing she does to survive, the
            // resource she builds, then the thing she is holding.
            //
            // Both use the SAME square slot, also owner-directed. A round dash slot was tried and
            // rejected: beside a square weapon slot a circle reads as a different kind of element
            // rather than as its pair, and the generated pip it had replaced was itself a disc set
            // in a pointed plate, not a bare circle.
            RectTransform dash = NewRect("Dash", parent);
            AnchorBottomCentre(dash, slotSize, new Vector2(-gaugeHalf - slotSize.x * 0.5f - 8f, Margin));

            // Socket, then the glyph, then the cooldown scrim OVER it, then the frame — sibling
            // order is draw order, and the scrim has to come after the glyph or it only darkens the
            // socket showing through the icon's transparent pixels. Insets match HUD_SlotSquare's
            // border, so socket, scrim and glyph all land on the same square as the slot's hole.
            RectTransform dashSocket = NewRect("Socket", dash);
            Inset(dashSocket, Vector4.one * SlotBorder);
            AddImage(dashSocket, fill, new Color(0.09f, 0.08f, 0.10f, 0.85f));

            // Centred at the icon's authored 64 rather than stretched, for the same reason as the
            // weapon icon below: a non-integer resample of point-filtered pixel art loses its grid.
            RectTransform dashGlyph = NewRect("Glyph", dash);
            Centre(dashGlyph, new Vector2(32f, 32f));
            Image dashImage = AddImage(dashGlyph, Load("HUD_IconDash"), Color.white);
            dashImage.preserveAspect = true;

            // A dark scrim, not a blue tint. This used to be a translucent blue wash, and because
            // DashHUD fed it the *charge* rather than the cooldown remaining, a ready dash drew a
            // full blue disc across the slot — permanently, since the dash is ready nearly always —
            // and cleared it exactly while the cooldown was running. Both halves are fixed: DashHUD
            // fills the remainder and switches the Image off at full, so a ready dash draws nothing.
            RectTransform sweep = NewRect("Sweep", dash);
            Inset(sweep, Vector4.one * SlotBorder);
            Image sweepImage = AddImage(sweep, fill, new Color(0.05f, 0.05f, 0.07f, 0.72f));
            sweepImage.type = Image.Type.Filled;
            sweepImage.fillMethod = Image.FillMethod.Radial360;
            sweepImage.fillOrigin = (int)Image.Origin360.Top;
            sweepImage.enabled = false;   // built ready, so nothing shows until a dash is spent

            RectTransform dashFrame = NewRect("Frame", dash);
            Stretch(dashFrame);
            AddImage(dashFrame, squareSlot, Color.white);

            RectTransform key = NewRect("Key", dash);
            AnchorBottomCentre(key, new Vector2(slotSize.x, 10f), new Vector2(0f, -10f));
            Text keyLabel = AddText(key, "LSHIFT", BodyText, TextAnchor.MiddleCenter);
            keyLabel.color = new Color(0.60f, 0.58f, 0.62f, 1f);

            var dashHud = dash.gameObject.AddComponent<DashHUD>();
            Wire(dashHud, "icon", dashImage);
            Wire(dashHud, "sweep", sweepImage);
            Wire(dashHud, "dash", PlayerPart<DigDash>());

            RectTransform weapon = NewRect("Weapon", parent);
            AnchorBottomCentre(weapon, slotSize, new Vector2(gaugeHalf + slotSize.x * 0.5f + 8f, Margin));

            RectTransform weaponSocket = NewRect("Socket", weapon);
            Inset(weaponSocket, Vector4.one * SlotBorder);
            AddImage(weaponSocket, null, new Color(0.09f, 0.08f, 0.10f, 0.85f));

            RectTransform weaponFrame = NewRect("Frame", weapon);
            Stretch(weaponFrame);
            AddImage(weaponFrame, squareSlot, Color.white);

            // Drawn at the icon's authored 64, which is now exactly the slot's hole: point-filtered
            // pixel art resampled by a non-integer factor loses its grid, which is the one thing the
            // whole art contract exists to protect. The slot's 6px border is chosen to make 76 - 12
            // land on 64 for precisely this reason.
            RectTransform weaponIcon = NewRect("Icon", weapon);
            Centre(weaponIcon, new Vector2(32f, 32f));
            Image weaponImage = AddImage(weaponIcon, null, Color.white);
            weaponImage.preserveAspect = true;

            // Off until WeaponIconHUD has a sprite to put in it, exactly as the upgrade slots are.
            // An Image with a null sprite draws a solid white quad, and this one is 64px square in
            // the middle of the screen — it filled the whole slot in the first render of the built
            // HUD, and it is not only an editor artefact: with no tagged player in the scene
            // nothing ever assigns the sprite.
            weaponImage.enabled = false;

            var weaponHud = weapon.gameObject.AddComponent<WeaponIconHUD>();
            Wire(weaponHud, "icon", weaponImage);
            Wire(weaponHud, "loadout", PlayerPart<RunLoadout>());
        }

        /// <summary>
        /// The run's upgrade strip, down the left edge under the health bar (owner-directed).
        ///
        /// Slots are pre-built and switched on as upgrades arrive rather than instantiated, so a
        /// level-up — which already pauses and opens a panel — does no allocation on top of that.
        /// </summary>
        private static void BuildUpgrades(RectTransform parent)
        {
            Sprite slotArt = Load("HUD_SlotUpgrade");
            Vector2 slotSize = SizeOf(slotArt, new Vector2(22f, 22f));

            float healthHeight = SizeOf(Load("HUD_BarSlim"), new Vector2(160f, 18f)).y;
            const int SlotCount = 10;
            const float Gap = 3f;

            RectTransform group = NewRect("Upgrades", parent);
            AnchorTopLeft(group, new Vector2(slotSize.x, (slotSize.y + Gap) * SlotCount),
                          new Vector2(Margin, -Margin - healthHeight - 7f));

            var slotRoots = new GameObject[SlotCount];
            var slots = new Image[SlotCount];
            var icons = new Image[SlotCount];

            for (int i = 0; i < SlotCount; i++)
            {
                RectTransform slot = NewRect("Slot" + i, group);
                AnchorTopLeft(slot, slotSize, new Vector2(0f, -i * (slotSize.y + Gap)));
                slotRoots[i] = slot.gameObject;

                // The frame goes on the slot object itself, because UpgradeListHUD tints that
                // Image to the upgrade's tier colour. The socket is a child underneath it: a
                // hollow outline alone vanished against the world — see the change brief.
                RectTransform socket = NewRect("Socket", slot);
                Inset(socket, Vector4.one * UpgradeSlotBorder);
                AddImage(socket, null, new Color(0.07f, 0.07f, 0.09f, 0.5f));

                RectTransform icon = NewRect("Icon", slot);
                Inset(icon, Vector4.one * UpgradeSlotBorder);
                Image iconImage = AddImage(icon, null, Color.white);
                iconImage.preserveAspect = true;
                iconImage.enabled = false;
                icons[i] = iconImage;

                // Added last so the frame draws over its own socket and icon.
                RectTransform frameRect = NewRect("Frame", slot);
                Stretch(frameRect);
                slots[i] = AddImage(frameRect, slotArt, Color.white);

                slot.gameObject.SetActive(false);
            }

            RectTransform overflow = NewRect("Overflow", group);
            AnchorTopLeft(overflow, new Vector2(slotSize.x, 11f),
                          new Vector2(0f, -SlotCount * (slotSize.y + Gap)));
            Text overflowLabel = AddText(overflow, string.Empty, BodyText, TextAnchor.MiddleCenter);
            overflowLabel.color = new Color(0.78f, 0.79f, 0.82f, 1f);

            var hud = group.gameObject.AddComponent<UpgradeListHUD>();
            Wire(hud, "overflowLabel", overflowLabel);
            Wire(hud, "upgrades", PlayerPart<RunUpgrades>());
            WireArray(hud, "slotRoots", slotRoots);
            WireArray(hud, "slots", slots);
            WireArray(hud, "icons", icons);
        }

        private static void BuildWaveIndicator(RectTransform parent)
        {
            // Two objects, and that is not decoration. The component polls in Update to decide
            // whether to show, so it must live on an object that STAYS active — putting it on the
            // thing it hides would stop its own Update running and it could never come back.
            Sprite plaque = Load("HUD_Banner");

            RectTransform host = NewRect("Wave", parent);
            AnchorTopCentre(host, SizeOf(plaque, new Vector2(134f, 19f)), new Vector2(0f, -Margin));

            RectTransform panel = NewRect("Panel", host);
            Stretch(panel);

            // The plaque goes on the panel, not the host, so hiding the panel hides the art with
            // the text — an empty frame sitting at the top of the screen between waves would read
            // as something the player is meant to look at.
            AddImage(panel, plaque, Color.white);

            Text label = AddText(panel, "WAVE 1 / 3", TitleText, TextAnchor.MiddleCenter);
            label.color = new Color(0.86f, 0.80f, 0.68f, 1f);

            var hud = host.gameObject.AddComponent<WaveIndicatorHUD>();
            Wire(hud, "group", panel.gameObject);
            Wire(hud, "label", label);
            Wire(hud, "room", Object.FindFirstObjectByType<CombatRoom>());

            // Hidden by default: GDD §UI shows this only inside Wave Rooms.
            panel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- pieces

        private struct BarPieces
        {
            public Image Frame;
            public Image Socket;
            public Image Ghost;
            public Image Fill;
            public Text Label;
        }

        /// <summary>
        /// Frame art, an inset socket, an inset fill and a centred label — the pixels only.
        ///
        /// Separate from <see cref="BuildBar"/> because the Ultimate Gauge must NOT get a StatBar:
        /// <c>UltimateGaugeHUD</c> already writes that same fillAmount, and two components driving
        /// one Image is a fight where whichever ran last wins that frame.
        /// </summary>
        private static BarPieces BuildBarVisuals(RectTransform parent, string art,
                                                 Color fillColour, bool withGhost)
        {
            // Order is the whole trick, and UGUI draws children in sibling order: socket, then the
            // chase bar, then the fill, then the frame OVER all three, then the label. The frames
            // ship with their channel knocked out to transparent, so the fill reads through the
            // hole while the frame's caps, rivets and segment ticks still sit on top of it. Putting
            // the frame underneath would hide the fill completely; putting the fill on top would
            // hide the art.
            Sprite frame = Load(art);
            Vector4 hole = MeasureHole(frame);

            // Each bar's own fill column, cut to that bar's channel height. A shared sprite
            // stretched to three different heights is the one place point-filtered art gets
            // resampled by a non-integer factor, and the fill's bright top row is what smears.
            Sprite fillArt = Load(art + "_Fill");
            if (fillArt == null) fillArt = Load("HUD_Fill");

            RectTransform socketRect = NewRect("Socket", parent);
            Inset(socketRect, hole);
            Image socket = AddImage(socketRect, null, new Color(0.09f, 0.08f, 0.10f, 0.85f));

            Image ghost = null;
            if (withGhost)
            {
                RectTransform ghostRect = NewRect("Ghost", parent);
                Inset(ghostRect, hole);
                ghost = AddImage(ghostRect, fillArt, ChaseColour);
                ghost.type = Image.Type.Filled;
                ghost.fillMethod = Image.FillMethod.Horizontal;
            }

            RectTransform fillRect = NewRect("Fill", parent);
            Inset(fillRect, hole);

            // A sprite, and it is not decoration: UGUI's Image ignores `type` and `fillAmount`
            // entirely when it has none and falls back to a plain quad. The bar then draws at full
            // width forever while the value moves underneath it — health read 62% and 20% as the
            // same 182px bar, with only the colour changing.
            Image fill = AddImage(fillRect, fillArt, fillColour);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            RectTransform frameRect = NewRect("Frame", parent);
            Stretch(frameRect);
            Image frameImage = AddImage(frameRect, frame, Color.white);

            Text label = AddText(parent, string.Empty, BodyText, TextAnchor.MiddleCenter);

            return new BarPieces
            {
                Frame = frameImage, Socket = socket, Ghost = ghost, Fill = fill, Label = label
            };
        }

        /// <summary>A bar plus the <see cref="StatBar"/> view that drives it.</summary>
        private static StatBar BuildBar(RectTransform parent, string art,
                                        Color fillColour, bool withGhost)
        {
            BarPieces pieces = BuildBarVisuals(parent, art, fillColour, withGhost);

            var bar = parent.gameObject.AddComponent<StatBar>();
            Wire(bar, "frame", pieces.Frame);
            Wire(bar, "socket", pieces.Socket);
            Wire(bar, "ghost", pieces.Ghost);
            Wire(bar, "fill", pieces.Fill);
            Wire(bar, "label", pieces.Label);
            Wire(bar, "fillColor", fillColour);
            Wire(bar, "ghostColor", ChaseColour);
            return bar;
        }

        // ---------------------------------------------------------------- plumbing

        private static Canvas FindOrCreateCanvas()
        {
            GameObject go = GameObject.Find(CanvasName);
            if (go == null)
            {
                go = new GameObject(CanvasName);
                go.AddComponent<Canvas>();
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
            scaler.referenceResolution = ReferenceResolution;

            // NOT ScaleWithScreenSize, which is what this used to be. That mode produces a
            // fractional factor at any window size other than the reference — 0.45 in a 906x463
            // editor Game view — and a fractional factor resamples point-filtered art off its grid
            // until the whole HUD reads as flat untextured bars. PixelPerfectHUDScale owns the mode
            // and the factor from here; see its summary for what that actually looked like.
            if (go.GetComponent<PixelPerfectHUDScale>() == null) go.AddComponent<PixelPerfectHUDScale>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Max(1, Screen.height / ReferenceHeight);

            return canvas;
        }

        /// <summary>
        /// The player rig's copy of a component, or null when the open scene has no tagged player.
        ///
        /// Every HUD class already falls back to this same tag lookup in its own <c>Awake</c>, so
        /// this is not what makes the HUD work — it is what makes each connection **visible in the
        /// Inspector** rather than discovered at runtime, which is the house rule. It lives in the
        /// tool rather than being dragged afterwards for the reason the whole layout does: a rebuild
        /// would silently throw hand-dragged references away.
        ///
        /// Null is a fine result. A HUD built in a scene with no player falls back at runtime
        /// exactly as it did before.
        /// </summary>
        private static T PlayerPart<T>() where T : Component
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.GetComponentInChildren<T>(true) : null;
        }

        private static Sprite Load(string file)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file + ".png");
            if (sprite == null)
            {
                Debug.LogWarning("Missing HUD sprite " + ArtFolder + file + ".png — the element " +
                                 "will build without its frame art.");
            }

            return sprite;
        }

        private static Vector2 SizeOf(Sprite sprite, Vector2 fallback)
        {
            return sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : fallback;
        }

        /// <summary>
        /// Where a bar frame's knocked-out interior actually is, as (left, bottom, right, top)
        /// insets in pixels from its own edges.
        ///
        /// **Measured off the art rather than authored**, and that is the point. Hand-estimated
        /// insets were wrong on all three frames — the Ultimate frame's hole starts 119px in, where
        /// the estimate said 16 — because these frames have chunky solid end caps the eye reads as
        /// part of the bar. A fill sized to the estimate spills under the frame, so the visible
        /// portion is no longer proportional to the value. Reading the alpha means re-generating a
        /// frame can never desync the layout from it again.
        ///
        /// The PNG is decoded from disk instead of read through the imported texture, because
        /// sprite imports are not marked readable and <c>GetPixels</c> would throw.
        /// </summary>
        private static Vector4 MeasureHole(Sprite frame)
        {
            if (frame == null) return Vector4.zero;

            string assetPath = AssetDatabase.GetAssetPath(frame);
            if (string.IsNullOrEmpty(assetPath)) return Vector4.zero;

            var tex = new Texture2D(2, 2);
            bool loaded = tex.LoadImage(System.IO.File.ReadAllBytes(assetPath));
            if (!loaded)
            {
                Object.DestroyImmediate(tex);
                return Vector4.zero;
            }

            int w = tex.width;
            int h = tex.height;
            Color32[] pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);

            // The longest fully transparent run across the middle of each axis. The middle row and
            // column pass through the interior panel on every frame in the kit, and taking the
            // longest run ignores the small transparent notches around the rivets.
            Vector2 horizontal = LongestClearRun(pixels, w, h, h / 2, true);
            Vector2 vertical = LongestClearRun(pixels, w, h, w / 2, false);

            if (horizontal.y <= horizontal.x || vertical.y <= vertical.x)
            {
                Debug.LogWarning("No knocked-out interior found in " + frame.name +
                                 " — its fill will cover the whole frame. Hollow the frame's " +
                                 "interior to transparency, as the rest of the kit is.");
                return Vector4.zero;
            }

            // GetPixels32 is bottom-up, which is already RectTransform's convention for y.
            return new Vector4(horizontal.x, vertical.x, w - 1 - horizontal.y, h - 1 - vertical.y);
        }

        /// <summary>First and last index of the longest fully transparent run along one line.</summary>
        private static Vector2 LongestClearRun(Color32[] pixels, int w, int h, int line, bool horizontal)
        {
            int length = horizontal ? w : h;
            int bestStart = 0, bestEnd = -1, start = -1;

            for (int i = 0; i <= length; i++)
            {
                bool clear = i < length && pixels[horizontal ? line * w + i : i * w + line].a == 0;

                if (clear && start < 0) start = i;
                else if (!clear && start >= 0)
                {
                    if (i - 1 - start > bestEnd - bestStart) { bestStart = start; bestEnd = i - 1; }
                    start = -1;
                }
            }

            return new Vector2(bestStart, bestEnd);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color colour)
        {
            Image image = rect.gameObject.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();

            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;   // the HUD is a readout, it must never eat clicks
            return image;
        }

        private static Text AddText(RectTransform parent, string content, int size, TextAnchor align)
        {
            // Always its own object. Text and Image both derive from Graphic, and Unity does not
            // support two Graphics on one GameObject — they fight over the same canvas renderer and
            // one of them silently does not draw.
            RectTransform rect = NewRect("Label", parent);
            Stretch(rect);

            var text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = PixelFont();

            // A hard drop shadow, offset by one *authored* pixel — which is two screen pixels,
            // because the font is packed at 2x (see PixelFontArt). Without it the labels sit
            // directly on the world and a pale digit over a pale floor tile is unreadable; a soft
            // shadow would be the one anti-aliased thing left in the HUD.
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

            return text;
        }

        /// <summary>
        /// The generated pixel font, or the built-in face if it has not been generated yet.
        ///
        /// Not cached: regenerating the font and rebuilding the HUD in the same editor session is
        /// the normal loop while tuning, and a cached reference there hands out the old asset.
        /// </summary>
        private static Font PixelFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "HUD_Font.fontsettings");
            if (font != null) return font;

            Debug.LogWarning("No HUD_Font.fontsettings in " + ArtFolder + " — run " +
                             "Deeper/Generate HUD Font first. Falling back to the built-in face, " +
                             "which is anti-aliased and will not match the rest of the HUD.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Fills a serialized array field with references, in order.</summary>
        private static void WireArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null || !prop.isArray)
            {
                Debug.LogWarning("No serialized array '" + field + "' on " + target.GetType().Name);
                return;
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sets a private serialized field by name, so the built HUD is wired exactly the
        /// way a human dragging references would leave it.</summary>
        private static void Wire(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning("No serialized field '" + field + "' on " + target.GetType().Name);
                return;
            }

            if (value is Color) prop.colorValue = (Color)value;
            else prop.objectReferenceValue = (Object)value;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- anchoring

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary><paramref name="by"/> is (left, bottom, right, top) in pixels.</summary>
        private static void Inset(RectTransform rect, Vector4 by)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(by.x, by.y);
            rect.offsetMax = new Vector2(-by.z, -by.w);
        }

        private static void Centre(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void AnchorTopLeft(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        private static void AnchorTopRight(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        private static void AnchorTopCentre(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        private static void AnchorBottomCentre(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }
    }
}
