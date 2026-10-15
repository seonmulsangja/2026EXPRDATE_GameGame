using UnityEditor;
using UnityEngine;

namespace ExpiryGame.Visual.Editor
{
    // 음식 10종 FoodData를 보장하고 sprite / spriteRotten을 연결한다. 이미 있는 SO의 수치는 건드리지 않는다.
    public static class FoodSpriteLinker
    {
        private const string DataDir = "Assets/ShelfLife/Data";

        private readonly struct Entry
        {
            public readonly string Key;
            public readonly string AssetName;
            public readonly string Display;
            public readonly float ShelfLife;

            public Entry(string key, string assetName, string display, float shelfLife)
            {
                Key = key;
                AssetName = assetName;
                Display = display;
                ShelfLife = shelfLife;
            }
        }

        // 기존 3종(빵/통조림/생선)은 기존 에셋 이름을 유지해 GUID 참조가 깨지지 않게 한다
        private static readonly Entry[] Entries =
        {
            new Entry("bread", "Food_Bread", "Bread", 120f),
            new Entry("can", "Food_Canned_Bean", "Canned Bean", 300f),
            new Entry("fish", "Food_Fresh_Fish", "Fresh Fish", 30f),
            new Entry("milk", "Food_Milk", "Milk", 50f),
            new Entry("egg", "Food_Egg", "Egg", 70f),
            new Entry("apple", "Food_Apple", "Apple", 100f),
            new Entry("cheese", "Food_Cheese", "Cheese", 220f),
            new Entry("sausage", "Food_Sausage", "Sausage", 160f),
            new Entry("ramen", "Food_Ramen", "Ramen", 280f),
            new Entry("jam", "Food_Jam", "Jam", 240f),
        };

        [MenuItem("Tools/ShelfLife/Link Food Sprites")]
        public static void Link()
        {
            var linked = 0;
            foreach (var entry in Entries)
            {
                var food = LoadOrCreate(entry);
                food.sprite = ArtAssets.Load("food", $"food_{entry.Key}");
                food.spriteRotten = ArtAssets.Load("food", $"food_{entry.Key}_rotten");
                if (food.icon == null || food.icon.name == "Knob") food.icon = food.sprite;
                EditorUtility.SetDirty(food);
                if (food.sprite != null) linked++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShelfLife] 음식 스프라이트 연결: {linked}/{Entries.Length}");
        }

        public static FoodData[] LoadAll()
        {
            var result = new FoodData[Entries.Length];
            for (var i = 0; i < Entries.Length; i++) result[i] = LoadOrCreate(Entries[i]);
            return result;
        }

        // 휘두를 때 즙이 튀는 음식 키
        public static bool IsJuicy(FoodData food)
        {
            var n = food.name.ToLowerInvariant();
            return n.Contains("fish") || n.Contains("apple") || n.Contains("milk") || n.Contains("egg") || n.Contains("jam");
        }

        private static FoodData LoadOrCreate(Entry entry)
        {
            var path = $"{DataDir}/{entry.AssetName}.asset";
            var food = AssetDatabase.LoadAssetAtPath<FoodData>(path);
            if (food != null) return food;

            if (!AssetDatabase.IsValidFolder(DataDir)) AssetDatabase.CreateFolder("Assets/ShelfLife", "Data");
            food = ScriptableObject.CreateInstance<FoodData>();
            food.displayName = entry.Display;
            food.shelfLifeSeconds = entry.ShelfLife;
            AssetDatabase.CreateAsset(food, path);
            return food;
        }
    }
}
