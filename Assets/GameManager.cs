using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] SpawnCycleManager spawnCycleManager;
    [SerializeField] RailEnemySpawner railEnemySpawner;
    [SerializeField] EnemySpawner enemySpawnerleft;
    [SerializeField] EnemySpawner enemySpawnerright;    

    [SerializeField] List<WaveSO> waveSOs;
    int listCounter;

    [SerializeField] Text timer;
    private float totalTime;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        totalTime += Time.deltaTime;
        timer.text = totalTime.ToString();

        if(totalTime%6 == 0)
        {
            ChangeMode();
        }
    }
    void ChangeMode()
    {
        listCounter++;
        playerController.isDobleJumpActivated = waveSOs[listCounter].isDobleJumpActivated;
        playerController.isDashActivated = waveSOs[listCounter].isDashActivated;
        playerController.isGlideActivated = waveSOs[listCounter].isGlideActivated;

        spawnCycleManager.minDestroyInterval = waveSOs[listCounter ].minDestroyInterval;
        spawnCycleManager.maxDestroyInterval = waveSOs[listCounter].maxDestroyInterval;
        spawnCycleManager.initialDelay = waveSOs[listCounter].initialDelay;
        spawnCycleManager.respawnDelay = waveSOs[listCounter].respawnDelay;

        railEnemySpawner.maxSpawnTime = waveSOs[listCounter].maxSpawnTimeforrail;
        railEnemySpawner.minSpawnTime = waveSOs[listCounter].minSpawnTimeforrail;

        enemySpawnerleft.maximumCount = waveSOs[listCounter].maximumCountLeft;
        enemySpawnerleft.minimumCount = waveSOs[listCounter].minimumCountLeft;
        enemySpawnerleft.maxSpawnTime = waveSOs[listCounter].maxSpawnTimeLeft;
        enemySpawnerleft.minSpawnTime = waveSOs[listCounter].minSpawnTimeLeft;

        enemySpawnerright.maximumCount = waveSOs[listCounter].minimumCountRight;
        enemySpawnerright.minimumCount = waveSOs[listCounter].maximumCountRight;
        enemySpawnerright.maxSpawnTime = waveSOs[listCounter ].maxSpawnTimeRight;
        enemySpawnerright.minSpawnTime = waveSOs[listCounter].minSpawnTimeRight;


    }
}
