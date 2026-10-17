using System.IO;
using UnityEditor;
using UnityEngine;

namespace ExpiryGame.Flow.Editor
{
    // Assets/Audio 안의 클립을 파일 이름으로 AudioCatalog에 채워 넣는다. 비어 있는 칸만 채우므로 직접 지정한 값은 유지된다.
    // 이름 규칙(대소문자 무시): day / night / danger / gameover, eat / hit(attack) / rot / pickup / break.
    // Assets/Audio에 없을 때만 팀원 효과음(Assets/JCY/06 Sounds)을 먹기/타격에 임시로 연결한다.
    public static class AudioCatalogBuilder
    {
        private const string AudioDir = "Assets/Audio";
        private const string TeamSoundDir = "Assets/JCY/06 Sounds";

        public static AudioCatalog LoadOrCreate(string dataDir)
        {
            var path = $"{dataDir}/AudioCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AudioCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            if (catalog.bgmDay == null) catalog.bgmDay = Find(AudioDir, "bgm", "day");
            if (catalog.bgmNight == null) catalog.bgmNight = Find(AudioDir, "bgm", "night");
            if (catalog.bgmDanger == null) catalog.bgmDanger = Find(AudioDir, "bgm", "danger");
            if (catalog.bgmGameOver == null) catalog.bgmGameOver = Find(AudioDir, "bgm", "gameover") ?? Find(AudioDir, "bgm", "game_over");

            if (catalog.sfxEat == null) catalog.sfxEat = Find(AudioDir, "sfx", "eat") ?? Find(TeamSoundDir, null, "eat");
            if (catalog.sfxHit == null) catalog.sfxHit = Find(AudioDir, "sfx", "hit") ?? Find(AudioDir, "sfx", "attack") ?? Find(TeamSoundDir, null, "attack");
            if (catalog.sfxRot == null) catalog.sfxRot = Find(AudioDir, "sfx", "rot");
            if (catalog.sfxPickup == null) catalog.sfxPickup = Find(AudioDir, "sfx", "pickup") ?? Find(AudioDir, "sfx", "pick");
            if (catalog.sfxBreak == null) catalog.sfxBreak = Find(AudioDir, "sfx", "break");

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // folder 안에서 이름에 keyword(와 선택적으로 group)가 들어간 첫 클립. 폴더가 없으면 null.
        private static AudioClip Find(string folder, string group, string keyword)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return null;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                var fullLower = path.ToLowerInvariant();
                if (!lower.Contains(keyword)) continue;
                if (group != null && !fullLower.Contains(group)) continue;
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return null;
        }
    }
}
