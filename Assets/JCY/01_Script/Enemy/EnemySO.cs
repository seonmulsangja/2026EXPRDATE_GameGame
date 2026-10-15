using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Scriptable Objects/EnemySO")]
public class EnemySO : ScriptableObject
{
    public int health;
    public int damage;
    public float speed;
    public Sprite sprite;
    public string enemyName;
}
