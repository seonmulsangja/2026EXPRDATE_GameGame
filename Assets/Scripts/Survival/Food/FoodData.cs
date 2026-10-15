using UnityEngine;
using UnityEngine.Serialization;

namespace ExpiryGame
{
    [CreateAssetMenu(menuName = "ShelfLife/Food Data", fileName = "FoodData")]
    public class FoodData : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        [FormerlySerializedAs("worldSprite")]
        [Tooltip("신선한 상태의 32x32 스프라이트 (월드/손에 든 모습/휘두르는 무기). 이 필드만 교체하면 아트가 바뀐다.")]
        public Sprite sprite;
        [Tooltip("썩은 뒤의 스프라이트. 비어 있으면 신선 스프라이트에 썩음 오버레이만 얹는다.")]
        public Sprite spriteRotten;
        [Min(1f)] public float shelfLifeSeconds = 120f;

        public Sprite SpriteFor(bool isRotten)
        {
            return isRotten && spriteRotten != null ? spriteRotten : sprite;
        }
    }
}
