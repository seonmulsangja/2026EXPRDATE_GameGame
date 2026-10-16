using System;
using ExpiryGame;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemySO _enemySo;

    [Header("SO로 초기화 되는 정보")]
    public int _health;
    public int _damage;
    public float _speed;
    public SpriteLibraryAsset _sprite;
    public string _enemyName;
    public float _width;
    public float _height;
    
    public EnemyPooling _enemyPooling;
    public Transform targetTrm;
    
    [SerializeField] private LayerMask _whatIsPlayer;
    
    private Vector2 _dir;
    private Rigidbody2D _rb;
    private SpriteLibrary _spriteLibrary;
    
    public void Initialize(EnemySO enemySo)
    {
        _enemySo = enemySo;

        _health = _enemySo.health;
        _damage = _enemySo.damage;
        _speed = _enemySo.speed;
        _sprite = _enemySo.sprite;
        _enemyName = _enemySo.enemyName;
        _width = _enemySo.width;
        _height = _enemySo.height;

        _spriteLibrary.spriteLibraryAsset = _sprite;
    }

    private void OnEnable()
    {
        EnemySO randomData =
            EnemyPooling.Instance.enemyDatas[Random.Range(0, EnemyPooling.Instance.enemyDatas.Count)];

        this.Initialize(randomData);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteLibrary = GetComponent<SpriteLibrary>();
    }

    private void Update()
    {
        if (targetTrm == null)
            return;
        Move();
       // Attack();
    }

    public void Move()
    {
        _dir = targetTrm.position - transform.position;

        Vector3 scale = transform.localScale;

        if (_dir.x > 0)
        {
            scale.x = Mathf.Abs(scale.x);
        }
        else if (_dir.x < 0)
        {
            scale.x = -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;
        _dir.Normalize();
        _rb.linearVelocity = _dir * _speed;
    }
    
    // public Health Detect()
    // {
    //     var detect = Physics2D.OverlapBox(
    //         transform.position,
    //         transform.localScale,
    //         0,
    //         _whatIsPlayer
    //     );
    //
    //     if (detect != null && detect.TryGetComponent<Health>(out var health))
    //     {
    //         return health;
    //     }
    //
    //     return null;
    // }
    //
    // public void Attack()
    // {
    //     Health health = Detect();
    //
    //     if (health != null)
    //     {
    //         health.TakeDamage(_damage);
    //     }
    // }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position , new Vector3(_width , _height , 1));
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && other.gameObject.TryGetComponent<Health>(out var health))
        {
            health.TakeDamage(_damage);
        }
    }
}
