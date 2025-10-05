using SimpleFactory;
using UnityEngine;

public class hfbvghy : MonoBehaviour
{
    private void Start()
    {
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Normal, new Vector3(10, 3, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Redirect, new Vector3(-10, -3.5f, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Top, EnemyMovementType.Normal, new Vector3(7, 5, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Top, EnemyMovementType.Fake, new Vector3(-2, 5, 0));
    }
}
