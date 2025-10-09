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
    [SerializeField] EnemySpawner enemySpawnerUp;
    [SerializeField] StaticPlatformSpawner staticPlatformSpawner;

    [SerializeField] List<WaveSO> waveSOs;
    int listCounter = 0;

    public Text timerText;
    private float timer = 0f;
    private int callCount = 0;
    private int maxCalls = 4;

    void Update()
    {
        if (callCount >= maxCalls) return;

        timer += Time.deltaTime;

        // نمایش تایمر به صورت دقیقه:ثانیه
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // هر ۶۰ ثانیه فقط یکبار
        if (Mathf.FloorToInt(timer) % 60 == 0 && Mathf.FloorToInt(timer) != 0)
        {
            if (Mathf.Approximately(timer % 60f, 0f))
            {
                CallFunction();
                callCount++;
            }
        }
    }

    void CallFunction()
    {
        ChangeMode();
        StartCoroutine(PauseGameForSeconds(5f));
    }

    IEnumerator PauseGameForSeconds(float seconds)
    {
        Time.timeScale = 0f;   // بازی متوقف
        Debug.Log("بازی متوقف شد برای " + seconds + " ثانیه");

        yield return new WaitForSecondsRealtime(seconds); // ۵ ثانیه واقعی (نه وابسته به تایم اسکیل)

        Time.timeScale = 1f;   // بازی ادامه
        Debug.Log("بازی دوباره شروع شد");
    }

    void ChangeMode()
    {
        DestroyByTagAll.instance.DestroyAllWithTwoTags();
        listCounter++;
        playerController.isDobleJumpActivated = waveSOs[listCounter].isDobleJumpActivated;
        playerController.isDashActivated = waveSOs[listCounter].isDashActivated;
        playerController.isGlideActivated = waveSOs[listCounter].isGlideActivated;

        spawnCycleManager.minDestroyInterval = waveSOs[listCounter].minDestroyInterval;
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
        enemySpawnerright.maxSpawnTime = waveSOs[listCounter].maxSpawnTimeRight;
        enemySpawnerright.minSpawnTime = waveSOs[listCounter].minSpawnTimeRight;

        enemySpawnerUp.maximumCount = waveSOs[listCounter].minimumCountUp;
        enemySpawnerUp.minimumCount = waveSOs[listCounter].minimumCountUp;
        enemySpawnerUp.maxSpawnTime = waveSOs[listCounter].maxSpawnTimeUp;
        enemySpawnerUp.minSpawnTime = waveSOs[listCounter].minSpawnTimeUp;

        playerController.isChangingInputs = waveSOs[listCounter].inputChanging;

        spawnCycleManager.enableSpawning = waveSOs[listCounter].platformChanging;

        staticPlatformSpawner.spawnActive = waveSOs[listCounter].staticPlatform;

    }

}
