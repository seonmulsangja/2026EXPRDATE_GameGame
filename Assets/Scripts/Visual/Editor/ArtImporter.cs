using System.IO;
using UnityEditor;
using UnityEngine;

namespace ExpiryGame.Visual.Editor
{
    // ART_MANIFEST.md 임포트 설정을 Assets/Art 아래 모든 PNG에 자동 적용한다.
    // Sprite(Single) / PPU 32 / Point / 압축 없음 / 밉맵 없음, 피벗은 종류별로 다르게.
    public class ArtImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Art/";
        private const int Ppu = 32;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetPath.EndsWith(".png")) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        [MenuItem("Tools/ShelfLife/Apply Art Import Settings")]
        public static void ApplyAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
            Debug.Log("[ShelfLife] 아트 임포트 설정 적용 완료 (PPU 32 / Point / 압축 없음)");
        }

        private static void Apply(TextureImporter importer, string path)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = Ppu;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = PivotFor(path);
            importer.SetTextureSettings(settings);

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        // 캐릭터/음식/장애물: Bottom Center, slash: 왼쪽 중앙(3/24), 나머지(타일/스파크/파티클/그림자): 중앙
        private static Vector2 PivotFor(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith("fx_slash")) return new Vector2(3f / 24f, 0.5f);
            if (name == "shadow") return new Vector2(0.5f, 0.5f);
            if (path.Contains("/food/") || path.Contains("/player/") || path.Contains("/obstacles/")) return new Vector2(0.5f, 0f);
            return new Vector2(0.5f, 0.5f);
        }
    }
}
