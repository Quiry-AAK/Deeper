// Procedural pixel-art flame aura.
//
// Construction, in order:
//   1. Snap to a pixel grid sized to ONE GAME PIXEL, so everything below is computed per chunky
//      pixel rather than per screen pixel. This is what keeps it pixel art instead of a smooth
//      gradient pasted over pixel art.
//   2. Sample the sprite's alpha as a mask, so the flame only exists where the character is.
//   3. Multiply by a radial falloff — the soft round shape of a default particle texture. This
//      is what makes the flame die away toward the edges, and it is why the aura no longer shows
//      the sprite's rectangular border: a radial mask cannot produce a straight cut.
//   4. Multiply by fbm noise scrolling along -Y, which is the flame motion.
//   5. Threshold, then quantise to a few levels so the colour banding reads as pixel art.
//
// One quad per layer. The sprite's own colours are never used, only its shape, and sampling is
// never offset, so it cannot bleed into neighbouring frames of a packed sheet.
Shader "Deeper/AuraSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Fringe Colour", Color) = (0.85, 0.24, 0.92, 1)
        _CoreColor ("Core Colour", Color) = (1, 0.95, 1, 1)

        // HIGHER = BIGGER tongues, the opposite of the raw noise frequency it feeds.
        // Capped in practice by resolution: the aura is only ~62x125 game pixels, so past ~2.5
        // there are too few noise cells across it to form separate tongues and it goes back to
        // being one blob. Chosen by rendering a sweep and looking.
        _FlameSize ("Flame Size", Range(0.2, 5)) = 1.6
        _Speed ("Rise Speed", Range(0, 16)) = 6

        // How far out the fire still holds together. Above 1 the core is solid and only the
        // outskirts tatter; at 1 even the centre starts breaking up.
        _Coverage ("Coverage", Range(0.4, 2)) = 1.3

        // Global bias on top of the radial cut. Higher = sparser everywhere.
        _Erode ("Erosion", Range(0, 0.8)) = 0.14

        // Radial falloff power. Higher pulls the flame in tight around her centre; lower lets it
        // spread to the rim. This replaces the old edge-fade hack.
        _Falloff ("Radial Falloff", Range(0.2, 4)) = 1.1

        _Heat ("Core Sharpness", Range(0.5, 8)) = 2.5
        _Levels ("Colour Steps", Range(2, 12)) = 4
        _Seed ("Seed", Float) = 0

        // Set per renderer. Sprite rect within its sheet as (uMin, vMin, uSize, vSize), and the
        // pixel-grid resolution across this quad.
        _SpriteRect ("Sprite Rect (uv)", Vector) = (0, 0, 1, 1)
        _Pixels ("Pixel Grid", Vector) = (32, 48, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _CoreColor;
            float _FlameSize;
            float _Speed;
            float _Coverage;
            float _Erode;
            float _Falloff;
            float _Heat;
            float _Levels;
            float _Seed;
            float4 _SpriteRect;
            float4 _Pixels;

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Normalised to 0..1. The raw amplitude sum is 0.875 with a mean near 0.44, and
            // comparing that against a 0..1 threshold quietly erodes almost nothing.
            float fbm(float2 p)
            {
                float v = 0.0, amp = 0.5, total = 0.0;
                for (int i = 0; i < 3; i++)
                {
                    v += amp * vnoise(p);
                    total += amp;
                    p *= 2.03;
                    amp *= 0.5;
                }
                return v / total;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Normalise to 0-1 ACROSS THIS SPRITE. texcoord addresses the whole sheet, so a
                // sub-sprite spans only a slice of it; using it raw leaves the noise near flat.
                float2 uv = (IN.texcoord - _SpriteRect.xy) / max(_SpriteRect.zw, 1e-5);

                // Snap to the game's pixel grid. Everything downstream is evaluated at the centre
                // of a chunky pixel, so the noise, the radial mask and the mask lookup all land on
                // the same grid and the result stays pixel art.
                float2 grid = max(_Pixels.xy, 1.0);
                float2 puv = (floor(uv * grid) + 0.5) / grid;

                // Shape mask: only burn where the character is.
                float a = tex2D(_MainTex, _SpriteRect.xy + saturate(puv) * _SpriteRect.zw).a;
                if (a < 0.02) discard;

                // The soft round particle shape. Radial in sprite space, so on a tall sprite it
                // is a tall ellipse — which is the shape a body aura wants anyway.
                float2 d = puv - 0.5;
                float radial = pow(saturate(1.0 - saturate(length(d) * 2.0)), _Falloff);

                // Noise scrolling up through the shape. Cells stretched along y turn blobs into
                // TALL tongues; square cells give a spotty dissolve, not fire.
                float freq = 14.0 / max(_FlameSize, 0.05);
                float2 nuv = puv * float2(freq, freq * 0.42) + _Seed;
                nuv.y -= _Time.y * _Speed;

                // Threshold the noise against a cut DRIVEN BY the particle falloff, rather than
                // multiplying the two and thresholding globally. Multiplying scales every pixel
                // down together, so a single cut just carves an ellipse — solid inside, nothing
                // outside. Making the cut itself fall off means the core keeps almost everything
                // while the rim keeps only the brightest noise, which is what tatters the edge
                // into tongues.
                float cut = 1.0 - radial * _Coverage + _Erode;
                float n = fbm(nuv);
                if (n < cut) discard;
                float v = saturate((n - cut) / max(0.02, 1.0 - cut));

                // Quantise, so the heat ramp bands into a few flat steps like hand-drawn fire
                // rather than a smooth gradient.
                float steps = max(_Levels, 2.0);
                v = floor(v * steps) / (steps - 1.0);

                float heat = saturate(v * _Heat);
                fixed3 col = lerp(_Color.rgb, _CoreColor.rgb, saturate(heat));

                // Alpha is flat, not feathered: a crisp on/off edge is what keeps it pixel art.
                return fixed4(col * IN.color.rgb, IN.color.a);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
