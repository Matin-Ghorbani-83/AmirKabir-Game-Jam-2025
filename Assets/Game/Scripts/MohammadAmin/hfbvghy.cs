using SimpleFactory;
using UnityEngine;

public class hfbvghy : MonoBehaviour
{
    private void Start()
    {
        //EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Redirect, new Vector3(10, 4, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Redirect, new Vector3(10, -4, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Redirect, new Vector3(-10, 3.5f, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Normal, new Vector3(-10, -3.5f, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Side, EnemyMovementType.Normal, new Vector3(10, -3.5f, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Top, EnemyMovementType.Redirect, new Vector3(7, -5, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Top, EnemyMovementType.Redirect, new Vector3(-2 - 5, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Top, EnemyMovementType.Redirect, new Vector3(-7, 5, 0));
        EnemyFactory.CreateEnemy(EnemyType.Bird, SpawnPointType.Top, EnemyMovementType.Redirect, new Vector3(7, 5, 0));

        //EnemyFactory.CreateEnemy(EnemyType.Shooter, SpawnPointType.Top, EnemyMovementType.Moving, new Vector3(8, 6, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Shooter, SpawnPointType.Top, EnemyMovementType.Const, new Vector3(-8, -6, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Shooter, SpawnPointType.Side, EnemyMovementType.Moving, new Vector3(8, -6, 0));
        //EnemyFactory.CreateEnemy(EnemyType.Shooter, SpawnPointType.Side, EnemyMovementType.Const, new Vector3(-8, 6, 0));
    }
}
