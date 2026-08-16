using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// Last-resort font for legacy <see cref="Text"/> components that were created without one.
    ///
    /// **This is the safety net, not the HUD's font.** The shipped HUD uses the generated bitmap
    /// face in <c>Art/UI/HUD_Font.fontsettings</c>, which <c>BuildRunHUD</c> assigns to every label
    /// as it builds them, so on those labels this is a no-op. It stays because a <see cref="Text"/>
    /// with no font at all draws nothing, and a component dropped on by hand should still be
    /// readable. The built-in face is anti-aliased and will look wrong beside the rest of the HUD —
    /// that is the intended signal that something was not built through the tool.
    ///
    /// TextMeshPro's essential resources are not imported in this project, which is why any of this
    /// is legacy <c>UnityEngine.UI</c> at all.
    /// </summary>
    internal static class LegacyUIFont
    {
        private static Font _font;

        public static Font Get()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        /// <summary>Assigns the built-in font if <paramref name="text"/> has none.</summary>
        public static void EnsureFont(Text text)
        {
            if (text != null && text.font == null) text.font = Get();
        }
    }
}
