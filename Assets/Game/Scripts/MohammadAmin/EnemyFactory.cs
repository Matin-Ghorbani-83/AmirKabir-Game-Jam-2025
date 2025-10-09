using UnityEngine;

namespace SimpleFactory
{
    public class EnemyFactory : MonoBehaviour
    {
        public static void CreateEnemy(EnemyType type, SpawnPointType spawnPoint, EnemyMovementType movementType, Vector3 position)
        {
            switch (type)
            {
                case EnemyType.Bird:
                    GameObject bird = Resources.Load<GameObject>("BirdEnemy");
                    bird.GetComponent<BirdEnemy>().spawnPointType = spawnPoint;
                    bird.GetComponent<BirdEnemy>().enemyMovementType = movementType;

                    Instantiate(bird, position, Quaternion.identity);
                    break;
                case EnemyType.Shooter:
                    GameObject shooter = Resources.Load<GameObject>("ShooterEnemy");
                    shooter.GetComponent<ShooterEnemy>().enemyMovementType = movementType;

                    Instantiate(shooter, position, Quaternion.identity);
                    break;
            }
        }
    }
}