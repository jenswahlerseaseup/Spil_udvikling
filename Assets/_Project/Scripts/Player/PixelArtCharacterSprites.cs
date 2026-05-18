using System.Collections.Generic;
using UnityEngine;

// Generates 16x16 pixel art character sprites at runtime.
// Replace CreateDownWalk / CreateUpWalk / CreateSideWalk return values with
// real sprite arrays once proper art is ready — everything else stays the same.
public static class PixelArtCharacterSprites
{
    public const int PixelsPerUnit = 16;
    private const int W = 16, H = 16;

    private static readonly Dictionary<char, Color32> Palette = new()
    {
        [' '] = new Color32(0,   0,   0,   0),   // transparent
        ['K'] = new Color32(240, 190, 148, 255),  // skin
        ['H'] = new Color32(72,  44,  14,  255),  // hair
        ['B'] = new Color32(66,  114, 188, 255),  // shirt
        ['b'] = new Color32(44,  82,  144, 255),  // shirt shadow
        ['P'] = new Color32(44,  44,  82,  255),  // pants
        ['F'] = new Color32(84,  50,  18,  255),  // shoes
        ['E'] = new Color32(32,  22,  60,  255),  // eye
        ['A'] = new Color32(80,  108, 172, 255),  // arm/sleeve
    };

    // Rows are in top-to-bottom visual order; each string must be exactly W chars.
    // Frame 0 = idle/neutral, frames 1-3 = walk cycle.

    private static readonly string[][] DownFrames =
    {
        new[] // 0 — idle
        {
            "                ",
            "    HHHHHH      ",
            "   HKKKKKKH     ",
            "   HKEKKEKH     ",
            "   HKKKKKKH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PP  PP      ",
            "    PP  PP      ",
            "    FF  FF      ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 1 — left foot forward
        {
            "                ",
            "    HHHHHH      ",
            "   HKKKKKKH     ",
            "   HKEKKEKH     ",
            "   HKKKKKKH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "   PPP  PP      ",
            "   PP    PP     ",
            "   FF     F     ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 2 — mid-step neutral (reuse idle)
        {
            "                ",
            "    HHHHHH      ",
            "   HKKKKKKH     ",
            "   HKEKKEKH     ",
            "   HKKKKKKH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PP  PP      ",
            "    PP  PP      ",
            "    FF  FF      ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 3 — right foot forward
        {
            "                ",
            "    HHHHHH      ",
            "   HKKKKKKH     ",
            "   HKEKKEKH     ",
            "   HKKKKKKH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PP  PPP     ",
            "    PP    PP    ",
            "     F     FF   ",
            "                ",
            "                ",
            "                ",
        },
    };

    private static readonly string[][] UpFrames =
    {
        new[] // 0 — idle (shows character's back)
        {
            "                ",
            "    HHHHHH      ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PP  PP      ",
            "    PP  PP      ",
            "    FF  FF      ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 1 — left foot forward
        {
            "                ",
            "    HHHHHH      ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "   PPP  PP      ",
            "   PP    PP     ",
            "   FF     F     ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 2 — mid-step neutral
        {
            "                ",
            "    HHHHHH      ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PP  PP      ",
            "    PP  PP      ",
            "    FF  FF      ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 3 — right foot forward
        {
            "                ",
            "    HHHHHH      ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "   HHHHHHHH     ",
            "    HKKKKHH     ",
            "  AABBBBBBAA    ",
            "  AABBBBBBAA    ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PP  PPP     ",
            "    PP    PP    ",
            "     F     FF   ",
            "                ",
            "                ",
            "                ",
        },
    };

    // Right-facing profile; DirectionalSpriteAnimator flips X for left.
    private static readonly string[][] SideFrames =
    {
        new[] // 0 — idle
        {
            "                ",
            "   HHHH         ",
            "  HKKKKHH       ",
            "  HKEKKKH       ",
            "  HKKKKKH       ",
            "   HKKKKHH      ",
            "   BBBBBBA      ",
            "  ABBBBBBA      ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PPPP        ",
            "    PP PP       ",
            "    FF FF       ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 1 — front leg forward
        {
            "                ",
            "   HHHH         ",
            "  HKKKKHH       ",
            "  HKEKKKH       ",
            "  HKKKKKH       ",
            "   HKKKKHH      ",
            "   BBBBBBA      ",
            "  ABBBBBBA      ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "  PPPP          ",
            "   PP PP        ",
            "   FF  F        ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 2 — mid-step neutral
        {
            "                ",
            "   HHHH         ",
            "  HKKKKHH       ",
            "  HKEKKKH       ",
            "  HKKKKKH       ",
            "   HKKKKHH      ",
            "   BBBBBBA      ",
            "  ABBBBBBA      ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PPPP        ",
            "    PP PP       ",
            "    FF FF       ",
            "                ",
            "                ",
            "                ",
        },
        new[] // 3 — back leg steps
        {
            "                ",
            "   HHHH         ",
            "  HKKKKHH       ",
            "  HKEKKKH       ",
            "  HKKKKKH       ",
            "   HKKKKHH      ",
            "   BBBBBBA      ",
            "  ABBBBBBA      ",
            "    BBBBBB      ",
            "    PPPPPP      ",
            "    PPPP        ",
            "     PP PP      ",
            "     F   FF     ",
            "                ",
            "                ",
            "                ",
        },
    };

    public static Sprite[] CreateDownWalk() => MakeFrames(DownFrames);
    public static Sprite[] CreateUpWalk()   => MakeFrames(UpFrames);
    public static Sprite[] CreateSideWalk() => MakeFrames(SideFrames);

    // Builds an in-memory CharacterSpriteSet from the generated placeholders.
    // Used by DirectionalSpriteAnimator when no real sprite set is assigned.
    public static CharacterSpriteSet CreateDefault()
    {
        var set = ScriptableObject.CreateInstance<CharacterSpriteSet>();

        var down = CreateDownWalk(); // [0]=idle, [1]=left-foot, [2]=neutral, [3]=right-foot
        set.frontIdle  = down[0];
        set.frontWalk  = new[] { down[1], down[3] };

        var up = CreateUpWalk();
        set.backIdle   = up[0];
        set.backWalk   = new[] { up[1], up[3] };

        var side = CreateSideWalk();
        set.sideRightIdle  = side[0];
        set.sideRightWalk  = new[] { side[1], side[3] };
        set.sideLeftIdle   = side[0];
        set.sideLeftWalk   = new[] { side[1], side[3] };

        return set;
    }

    private static Sprite[] MakeFrames(string[][] frameData)
    {
        var sprites = new Sprite[frameData.Length];
        for (var i = 0; i < frameData.Length; i++)
            sprites[i] = GridToSprite(frameData[i]);
        return sprites;
    }

    private static Sprite GridToSprite(string[] rows)
    {
        var pixels = new Color32[W * H];
        for (var row = 0; row < H; row++)
        {
            var texRow = H - 1 - row; // visual top-to-bottom → Unity bottom-to-top
            var line = row < rows.Length ? rows[row] : "";
            for (var col = 0; col < W; col++)
            {
                var c = col < line.Length ? line[col] : ' ';
                pixels[texRow * W + col] = Palette.TryGetValue(c, out var color) ? color : new Color32(0, 0, 0, 0);
            }
        }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, // no blurring — keep pixels sharp
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels32(pixels);
        tex.Apply();

        // Pivot at feet level (row 12 from top = row 3 from bottom → 3/16 ≈ 0.2).
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.2f), PixelsPerUnit);
    }
}
