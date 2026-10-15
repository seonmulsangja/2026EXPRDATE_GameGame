using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;

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
        //Attack();
    }

    public void Move()
    {
        _dir = targetTrm.position - gameObject.transform.position;
        _rb.linearVelocity = _dir * _speed;
    }

    public void TackDamage(int amount)
    {
        _health -= amount;
        if (_health <= 0)
        {
            _enemyPooling.ReturnEnemy(gameObject);
        }
    }

    // public PlayerHealth Detect()
    // {
    //     var detect = Physics2D.OverlapBox(
    //         transform.position,
    //         transform.localScale,
    //         0,
    //         _whatIsPlayer
    //     );
    //
    //     if (detect != null && detect.TryGetComponent<PlayerHealth>(out var health))
    //     {
    //         return health;
    //     }
    //
    //     return null;
    // }
    
    // public void Attack()
    // {
    //     PlayerHealth health = Detect();
    //
    //     if (health != null)
    //     {
    //         health.TakeDamage(10);
    //     }
    // }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position , new Vector3(_width , _height , 1));
    }
}
