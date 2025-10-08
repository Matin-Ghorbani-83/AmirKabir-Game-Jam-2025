using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Bird Enemy", fileName = "BirdEnemy")]
public class SOBirdEnemy : ScriptableObject
{
    public float Speed;
    public Vector2 KnockbackForce;
}
