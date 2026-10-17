using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ExpiryGame.UI.Editor
{
    // 한글 픽셀 폰트 파일(.ttf/.otf)로 TMP 폰트 에셋을 만들고 UITheme.font에 연결한다. OS 폰트에 의존하지 않는다.
    // 사용: 폰트 파일을 Assets/Fonts/ 에 넣고 (또는 Project 창에서 폰트 파일을 선택하고) 메뉴 실행.
    // 도트가 번지지 않게 래스터(비-SDF) 렌더링 + Point 필터로 만들고, 프로젝트 안에서 쓰는 한글 글자를 미리 아틀라스에 넣는다.
    public static class UIFontMenu
    {
        public const string FontDir = "Assets/Fonts";
        private const string AssetDir = "Assets/ShelfLife/UI/Fonts";
        private const int DefaultPixelSize = 16;

        [MenuItem("Tools/ShelfLife/UI/Create TMP Font From Project Font")]
        public static void CreateFromProjectFont()
        {
            var source = Selection.activeObject as Font ?? FindFontInFolder();
            if (source == null)
            {
                EditorUtility.DisplayDialog("폰트 파일 없음",
                    $"{FontDir} 에 한글 픽셀 폰트 파일(.ttf/.otf)을 넣거나, Project 창에서 폰트 파일을 선택한 뒤 다시 실행하세요.\n" +
                    "(지금은 기존 폰트를 그대로 씁니다. 제출 전 폰트 파일 추가 필수 — docs/handoff.md 맨 위 참고)", "확인");
                return;
            }

            var theme = UISetup.LoadOrCreateTheme();
            // 도트 폰트의 원본 크기. 글자 크기를 이 값의 정수배로 맞추면 번지지 않는다 (UITheme.fontSnapUnit에 기록)
            var pixelSize = theme.fontSnapUnit > 0 ? theme.fontSnapUnit : DefaultPixelSize;

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var assetPath = $"{AssetDir}/{source.name} Pixel SDF.asset";
            if (!AssetDatabase.IsValidFolder(AssetDir)) AssetDatabase.CreateFolder("Assets/ShelfLife/UI", "Fonts");

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null)
            {
                // RASTER = SDF가 아닌 비트맵 렌더링 (가장자리가 부드럽게 퍼지지 않는다). 아틀라스가 차면 자동으로 추가 아틀라스를 만든다
                fontAsset = TMP_FontAsset.CreateFontAsset(source, pixelSize, 1, GlyphRenderMode.RASTER, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                fontAsset.material.name = $"{source.name} Pixel Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                foreach (var texture in fontAsset.atlasTextures)
                {
                    texture.name = $"{source.name} Pixel Atlas";
                    AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }

            // 도트가 번지지 않게 Point 샘플링
            foreach (var texture in fontAsset.atlasTextures)
            {
                texture.filterMode = FilterMode.Point;
                EditorUtility.SetDirty(texture);
            }

            var characters = CollectProjectCharacters();
            fontAsset.TryAddCharacters(characters, out var missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[ShelfLife] 폰트에 없는 글자 {missing.Length}개: {missing}");
            EditorUtility.SetDirty(fontAsset);

            theme.font = fontAsset;
            theme.fontSnapUnit = pixelSize;
            EditorUtility.SetDirty(theme);

            // 열린 씬의 텍스트도 바로 새 폰트로
            foreach (var themed in Object.FindObjectsByType<ThemedText>(FindObjectsSortMode.None))
            {
                themed.Apply();
                EditorUtility.SetDirty(themed.GetComponent<TMP_Text>());
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ShelfLife] TMP 픽셀 폰트 생성/연결 완료: {source.name} ({sourcePath}), 원본 {pixelSize}px, 미리 넣은 글자 {characters.Length}개 → UITheme.font / fontSnapUnit");
        }

        private static Font FindFontInFolder()
        {
            if (!AssetDatabase.IsValidFolder(FontDir)) return null;
            foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { FontDir }))
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
                if (font != null) return font;
            }
            return null;
        }

        // 기본 ASCII + 프로젝트의 코드/에셋/씬에 나오는 한글 음절과 자주 쓰는 기호
        private static string CollectProjectCharacters()
        {
            var set = new SortedSet<char>();
            for (var c = ' '; c <= '~'; c++) set.Add(c);
            foreach (var c in "·→←↑↓…×÷±°％“”‘’「」『』※★☆♥♡") set.Add(c);

            foreach (var pattern in new[] { "*.cs", "*.asset", "*.prefab", "*.unity" })
            {
                foreach (var file in Directory.EnumerateFiles(Application.dataPath, pattern, SearchOption.AllDirectories))
                {
                    string text;
                    try { text = File.ReadAllText(file, Encoding.UTF8); }
                    catch (IOException) { continue; }
                    foreach (var c in text)
                        if (c >= 0xAC00 && c <= 0xD7A3) set.Add(c);
                }
            }

            var builder = new StringBuilder(set.Count);
            foreach (var c in set) builder.Append(c);
            return builder.ToString();
        }
    }
}
