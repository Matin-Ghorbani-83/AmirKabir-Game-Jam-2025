using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Shooter Enemy", fileName = "ShooterEnemy")]
public class SOShooterEnemy : ScriptableObject
{
    [Header("Enemy")]
    public float Speed;
    public float FireRate;
    public float FireCount;
    [Header("Bullet Enemy")]
    public float BulletSpeed;
}
