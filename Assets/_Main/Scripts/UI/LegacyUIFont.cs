using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// Supplies Unity's built-in font to legacy <see cref="Text"/> components that were created
    /// without one. TextMeshPro's essential resources are not imported into this project, so the
    /// dev-facing UI uses the built-in font instead of pulling ~2 MB of assets into the repo.
    /// Replace this whole approach when the real UI art pass happens (ART_DIRECTION §5).
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
