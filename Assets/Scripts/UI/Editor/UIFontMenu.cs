using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ExpiryGame.UI.Editor
{
    // 프로젝트 안의 폰트 파일(.ttf/.otf)로 TMP 폰트 에셋을 만들고 UITheme.font에 연결한다. OS 폰트에 의존하지 않는다.
    // 사용: 폰트 파일을 Assets/ShelfLife/UI/Fonts/ 에 넣고 (또는 Project 창에서 선택하고) 메뉴 실행.
    public static class UIFontMenu
    {
        private const string FontDir = "Assets/ShelfLife/UI/Fonts";
        private const string LegacyPlaceholder = "PlaceholderKorean";

        [MenuItem("Tools/ShelfLife/UI/Create TMP Font From Project Font")]
        public static void CreateFromProjectFont()
        {
            var source = Selection.activeObject as Font ?? FindFontInFolder();
            if (source == null)
            {
                EditorUtility.DisplayDialog("폰트 파일 없음",
                    $"{FontDir} 에 폰트 파일(.ttf/.otf)을 넣거나, Project 창에서 폰트 파일을 선택한 뒤 다시 실행하세요.", "확인");
                return;
            }

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var assetPath = $"{FontDir}/{source.name} SDF.asset";
            if (!AssetDatabase.IsValidFolder(FontDir)) AssetDatabase.CreateFolder("Assets/ShelfLife/UI", "Fonts");

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null)
            {
                // 동적 아틀라스: 한글처럼 글자 수가 많은 폰트도 쓰는 글자만 채워진다
                fontAsset = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                fontAsset.material.name = $"{source.name} SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                foreach (var texture in fontAsset.atlasTextures)
                {
                    texture.name = $"{source.name} SDF Atlas";
                    AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }

            var theme = UISetup.LoadOrCreateTheme();
            theme.font = fontAsset;
            EditorUtility.SetDirty(theme);

            // 열린 씬의 텍스트도 바로 새 폰트로
            foreach (var themed in Object.FindObjectsByType<ThemedText>(FindObjectsSortMode.None))
            {
                themed.Apply();
                EditorUtility.SetDirty(themed.GetComponent<TMP_Text>());
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ShelfLife] TMP 폰트 에셋 생성/연결 완료: {source.name} ({sourcePath}) → UITheme.font");
        }

        private static Font FindFontInFolder()
        {
            if (!AssetDatabase.IsValidFolder(FontDir)) return null;
            foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { FontDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(LegacyPlaceholder)) continue;
                var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font != null) return font;
            }
            return null;
        }
    }
}
