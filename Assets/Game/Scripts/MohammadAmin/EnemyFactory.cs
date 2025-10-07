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
                    GameObject prefab = Resources.Load<GameObject>("BirdEnemy");
                    prefab.GetComponent<BirdEnemy>().spawnPointType = spawnPoint;
                    prefab.GetComponent<BirdEnemy>().enemyMovementType = movementType;

                    Instantiate(prefab, position, Quaternion.identity);
                    break;
                case EnemyType.Runner:

                    break;
            }
        }
    }
}