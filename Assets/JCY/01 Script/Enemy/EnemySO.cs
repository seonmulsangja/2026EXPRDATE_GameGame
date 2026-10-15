using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Enemy/EnemySO")]
public class EnemySO : ScriptableObject
{
    public int health;
    public int damage;
    public float speed;
    public SpriteLibraryAsset sprite;
    public string enemyName;
    public float width;
    public float height;
}
