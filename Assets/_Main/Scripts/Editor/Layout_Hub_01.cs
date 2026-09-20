namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of the Hub — the surface camp at the mine mouth (GDD §Game Loop 1: *"Player
    /// starts in a small surface camp"*).
    ///
    /// Written in the same ASCII form as every Combat Room and painted through the same projection
    /// (<see cref="RoomLayout.CellCentre"/>), so the camp's fixtures land on the same diamonds its
    /// tilemap draws. What it does *not* share is the legend: a camp's cells mean things no room
    /// has, so the extra characters are passed to Validate for this map alone.
    ///
    /// **Fixtures are authored cells, never a random scatter.** A Combat Room re-rolls its props on
    /// every mount because it is pooled and disposable; the camp is one place the player learns the
    /// shape of, and a weapon rack that moved between visits would be hostile. It is also load
    /// bearing — each fixture's trigger volume has to sit where its art is drawn.
    /// </summary>
    public static class Layout_Hub_01
    {
        /// <summary>
        /// Characters legal here on top of the shared legend. `,` grass, `f` campfire,
        /// `l` lantern post, `c` crates, `W` weapon rack, `S` stat shrine, `M` mine shaft,
        /// `C` the Codex tent.
        ///
        /// Lower case is scenery and upper case is a station you can use — which is the whole rule
        /// for reading this map at a glance.
        ///
        /// **The tent used to be scenery and is now the Codex**, because at 3.6 x 3.5 world units it
        /// is the largest object in the camp and nothing that big should be inert — the owner's
        /// note was that it is "too big for decoration". It earns the space by being a station the
        /// design already calls for: CORE_SYSTEMS §15 banks Memory Fragments into a **Hub Codex**,
        /// and MVP lists a Codex UI stub. Putting it in her tent is a placement decision, not an
        /// invented mechanic — but it is a decision, and it is recorded in the change brief.
        /// </summary>
        public const string Legend = ",flcWSMC";

        /// <summary>
        /// Top row first, so the string reads the way the camp looks.
        ///
        /// The shape is a bowl: trodden dirt in the middle where she actually walks, grass thinning
        /// out to the stone retaining wall. That is not decoration — the dirt *is* the path, and
        /// putting every station on it means the camp teaches its own route with no signage.
        ///
        /// The shaft sits at the near edge and the rack on the far side of the fire, so the walk
        /// from "choose a weapon" to "descend" crosses the camp rather than being two doors side by
        /// side. She spawns at `P`, beside the fire.
        ///
        /// **`P` is placed outside every station's reach**, which is a real constraint now that
        /// reach is derived from each fixture's footprint rather than being a flat 1.6: the mine
        /// shaft's is 2.65 units and the Codex tent's 2.6, and two earlier spawns each landed inside
        /// one of them — she arrived with a keycap already showing, before taking a step. At (6, 7)
        /// the nearest station is the rack at 3.2 units. Anything moved here has to be re-checked
        /// against all four, because the reaches are no longer the same size as each other.
        ///
        /// **Nothing stands within two cells of the wall.** That became a rule once the wall was
        /// built as two courses instead of one: at 1.75 world units it is tall enough to swallow a
        /// fixture placed against it, and the stat shrine — which used to sit one cell from the east
        /// wall — was half buried in the stonework.
        ///
        /// **Every lantern post stands beside a station, and nowhere else.** They began as free
        /// scenery flanking the path, which made them a lie — the camp had lit things you could use
        /// and lit things you could not. Moved here they cost no new art and make "lit means usable"
        /// true by construction, as a quiet second layer under the floating markers. Anything added
        /// to this map later has to keep that promise or drop it deliberately.
        ///
        /// **"Beside" is a diagonal step, not an adjacent character.** Screen position here is
        /// `x - y` across and `x + y` back, so the cell that sits level and one step to the side of
        /// `(x, y)` is `(x + 1, y - 1)` — a neighbour in the map string is *diagonal* on screen and
        /// reads as standing in front. Every lantern below is placed on that diagonal from the
        /// station it lights.
        /// </summary>
        public static readonly string[] Map =
        {
            "################",   // y = 13
            "#,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,#",
            "#,,,...C......,#",   // y = 10 — the Codex tent, centre-north, opposite the shaft
            "#,,.....l.....,#",
            "#,,c.....f..l.,#",   // crates west, campfire centre, the shrine's lantern east
            "#,,W..P......S,#",   // y = 7 — rack west, she spawns mid-camp, shrine on the east wall
            "#,,.l.........,#",   // the rack's lantern
            "#,,...........,#",
            "#,,...........,#",
            "#,,,.........,,#",
            "#,,,,..M..,,,,,#",   // y = 2 — the mine shaft, down against the south wall
            "#,,,,,,,l,,,,,,#",   // the shaft's lantern, beside it
            "################",   // y = 0
        };

        public const int Width = 16;
        public const int Height = 14;
    }
}
