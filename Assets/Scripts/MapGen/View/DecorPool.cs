using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 장식의 밤 진행도(0~1). MapGen은 낮밤 시스템을 모르므로, 값을 넣어 주는 쪽(World의 DecorNightController)이 Set만 부른다.
    public static class DecorLighting
    {
        public static float Night01 { get; private set; }
        public static event System.Action<float> Changed;

        public static void Set(float night01)
        {
            Night01 = night01;
            Changed?.Invoke(night01);
        }
    }

    // 장식 한 개. night 스프라이트가 있으면 같은 자리에 겹쳐 알파로 크로스페이드, 없으면 밤 틴트를 곱한다.
    public class DecorInstance : MonoBehaviour
    {
        public int TypeIndex;
        public SpriteRenderer Day;
        public SpriteRenderer Night;
        public Color NightTint = Color.white;

        private void OnEnable()
        {
            DecorLighting.Changed += Apply;
            Apply(DecorLighting.Night01);
        }

        private void OnDisable()
        {
            DecorLighting.Changed -= Apply;
        }

        private void Apply(float night01)
        {
            if (Night != null)
            {
                var color = Night.color;
                color.a = night01;
                Night.color = color;
            }
            else
            {
                Day.color = Color.Lerp(Color.white, NightTint, night01);
            }
        }
    }

    // 장식 GameObject 풀 (충돌 없음, 스프라이트만). 종류별 스택.
    public class DecorPool
    {
        private readonly MapGenSettings settings;
        private readonly Transform root;
        private readonly Stack<DecorInstance>[] free;

        public DecorPool(MapGenSettings settings, Transform root)
        {
            this.settings = settings;
            this.root = root;
            free = new Stack<DecorInstance>[settings.decor.Length];
            for (var i = 0; i < free.Length; i++) free[i] = new Stack<DecorInstance>();
        }

        public DecorInstance Get(DecorPlacement placement)
        {
            var stack = free[placement.TypeIndex];
            var decor = stack.Count > 0 ? stack.Pop() : Create(placement.TypeIndex);
            decor.transform.position = placement.Position;
            decor.gameObject.SetActive(true);
            return decor;
        }

        public void Release(DecorInstance decor)
        {
            decor.gameObject.SetActive(false);
            free[decor.TypeIndex].Push(decor);
        }

        private DecorInstance Create(int typeIndex)
        {
            var definition = settings.decor[typeIndex];
            var go = new GameObject($"Decor_{definition.name}");
            go.SetActive(false); // AddComponent 시점에 OnEnable이 돌지 않도록, 렌더러 연결 후 활성화
            go.transform.SetParent(root, false);

            var decor = go.AddComponent<DecorInstance>();
            decor.TypeIndex = typeIndex;
            decor.NightTint = definition.nightTint;
            decor.Day = AddRenderer(go.transform, "Day", definition.day, settings.decorSortingOrder);
            if (definition.night != null)
            {
                decor.Night = AddRenderer(go.transform, "Night", definition.night, settings.decorSortingOrder + 1);
                decor.Night.color = new Color(1f, 1f, 1f, 0f);
            }
            return decor; // 활성화는 Get()에서 SetActive(true)
        }

        private SpriteRenderer AddRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (settings.decorMaterial != null) renderer.sharedMaterial = settings.decorMaterial;
            return renderer;
        }
    }
}
