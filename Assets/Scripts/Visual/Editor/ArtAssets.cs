using UnityEditor;
using UnityEngine;

namespace ExpiryGame.Visual.Editor
{
    // 아트 에셋 경로/로드 헬퍼. 파일명 규칙은 docs/art_pack/ART_MANIFEST.md 참고.
    public static class ArtAssets
    {
        public const string SpriteRoot = "Assets/Art/Sprites";
        public const string ThemeDir = "Assets/ShelfLife/Art";
        public const string ThemePath = ThemeDir + "/ArtTheme.asset";
        public const string LitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        public const string UnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        public static Sprite Load(string folder, string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteRoot}/{folder}/{name}.png");
        }

        public static Sprite[] LoadFrames(string folder, string prefix, int count)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++) frames[i] = Load(folder, $"{prefix}_{i}");
            return frames;
        }

        public static ArtTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ArtTheme>(ThemePath);
            if (theme != null) return theme;
            if (!AssetDatabase.IsValidFolder(ThemeDir)) AssetDatabase.CreateFolder("Assets/ShelfLife", "Art");
            theme = ScriptableObject.CreateInstance<ArtTheme>();
            theme.ApplyDarkDefaults();
            AssetDatabase.CreateAsset(theme, ThemePath);
            return theme;
        }
    }
}
