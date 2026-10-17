using UnityEngine;

public class ItemMagnet : MonoBehaviour, IItem_JCY
{
    [Header("Pickup Range")]
    [SerializeField, Min(0f)] private float pickupRange = 1f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private Transform _player;

    private void OnEnable()
    {
        ItemMagnetManager.Register(this);
    }

    private void OnDisable()
    {
        ItemMagnetManager.Unregister(this);
    }

    private void Update()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position,
            pickupRange,
            playerLayer
        );

        if (playerCollider == null)
        {
            _player = null;
            return;
        }

        _player = playerCollider.transform;

        transform.position = Vector2.MoveTowards(
            transform.position,
            _player.position,
            moveSpeed * Time.deltaTime
        );
    }

    public void SetPickupRange(float range)
    {
        pickupRange = Mathf.Max(0f, range);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}