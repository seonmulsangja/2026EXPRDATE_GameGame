using System;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // Health를 쓰는 적용 어댑터: Health.Died를 IDropSource.Died로 바꿔 줄 뿐이다. 적 코드는 건드리지 않는다.
    [RequireComponent(typeof(Health))]
    public class HealthDropSource : MonoBehaviour, IDropSource
    {
        [SerializeField] private DropTable table;
        [Range(0f, 1f)] [SerializeField] private float threat01 = 0.5f;

        private Health health;

        public DropTable Table => table;
        public float Threat01 => threat01;
        public Vector3 DropPosition => transform.position;

        public event Action<IDropSource> Died;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Died += OnHealthDied;
            DropSourceRegistry.Register(this);
        }

        private void OnDisable()
        {
            health.Died -= OnHealthDied;
            DropSourceRegistry.Unregister(this);
        }

        private void OnHealthDied()
        {
            Died?.Invoke(this);
        }
    }
}
