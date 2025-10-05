using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Enemy", fileName = "name Enemy")]
public class SOEnemy : ScriptableObject
{
    public float Speed;

    public float Damage;
    // OR
    public Vector2 KnockbackForce;
}
