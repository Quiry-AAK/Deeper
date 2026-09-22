using System;
using Deeper.Upgrades;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Deeper.UI
{
    /// <summary>
    /// One socket in a taken-picks readout, and the only thing that knows <b>which</b> pick it is
    /// currently showing.
    ///
    /// The readout is icons alone (owner, 2026-09-20): a run has no cap on how many upgrades it
    /// takes, so spelling each one out turns the panel into a wall of text that outgrows the screen.
    /// The name and the description live in the hover popup instead, which is what this component
    /// exists to raise — <see cref="UpgradeListHUD"/> fills the slot, this reports the pointer, and
    /// <see cref="UpgradeTooltip"/> draws the words.
    ///
    /// Sits on the slot ROOT with its frame Image as a clickable child, the same shape
    /// <c>UpgradeCard</c> uses — UGUI bubbles a pointer event up to the first handler it finds, so
    /// the graphic that is hit and the component that answers need not be the same object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>The upgrade this slot is showing, or null when it is showing a Curse.</summary>
        public UpgradeDefinition Upgrade { get; private set; }

        /// <summary>The Curse this slot is showing, or null when it is showing an upgrade.</summary>
        public CurseDefinition Curse { get; private set; }

        /// <summary>Raised on pointer-enter, and never while the slot is empty.</summary>
        public event Action<UpgradeSlot> Hovered;

        /// <summary>Raised on pointer-exit, and on the way out — see <see cref="OnDisable"/>.</summary>
        public event Action<UpgradeSlot> Unhovered;

        public bool IsFilled { get { return Upgrade != null || Curse != null; } }

        public void Show(UpgradeDefinition upgrade)
        {
            Upgrade = upgrade;
            Curse = null;
        }

        public void Show(CurseDefinition curse)
        {
            Upgrade = null;
            Curse = curse;
        }

        /// <summary>Empties the slot. Its own method rather than a null <c>Show</c>, which would be
        /// ambiguous between the two overloads and would not say what it meant at the call site.</summary>
        public void Clear()
        {
            Upgrade = null;
            Curse = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // An empty slot is invisible anyway (UpgradeListHUD switches the whole object off), but
            // the guard is not redundant: a panel rebuilt while the pointer is already inside one
            // gets an enter before anything has filled it, and the popup would open blank.
            if (!IsFilled || Hovered == null) return;

            Hovered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Unhovered != null) Unhovered(this);
        }

        /// <summary>
        /// Also raises <see cref="Unhovered"/>, because UGUI does not send a pointer-exit to an
        /// object that is deactivated under the cursor. Taking the last pick of a run, or closing
        /// the pause menu with the pointer resting on an icon, would otherwise leave the popup
        /// stranded on screen with nothing under it.
        /// </summary>
        private void OnDisable()
        {
            if (Unhovered != null) Unhovered(this);
        }
    }
}
