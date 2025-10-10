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
    [SerializeField] GameObject changePanel;
    [SerializeField] GameObject skillOne;
    [SerializeField] GameObject skillTwo;
    [SerializeField] GameObject skillThree;
    [SerializeField] Text Abilitytext;
    [SerializeField] Text nextrandtext;
    private GameObject backGround;
    private GameObject tempbg;
    [SerializeField] List<WaveSO> waveSOs;
    int listCounter = -1;

    [Header("Timer Settings")]
    public Text timerText;           // Reference to UI TextMeshPro
    [Tooltip("Interval between function calls in seconds")]
    public float interval = 60f;         // Change in Inspector (e.g. 10 for 10 seconds)
    [Tooltip("Maximum number of times the function should be called")]
    public int maxCalls = 3;

    [Header("Pause Settings")]
    [Tooltip("How long the game should pause (in seconds, real time)")]
    public float pauseDuration = 5f;

    private float timer = 0f;            // Timer for UI
    private int callCount = 0;           // Number of times function was called

    private void Start()
    {
        ChangeMode();
        changePanel.SetActive(false);
    }
    void Update()
    {
        if (callCount < maxCalls)
        {
            // Update timer (this stops when timeScale = 0)
            timer += Time.deltaTime;

            // Display timer in mm:ss format
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // Calculate next trigger time based on current interval
            float nextCallTime = (callCount + 1) * Mathf.Max(0.0001f, interval);

            // If it's time to trigger
            if (timer >= nextCallTime)
            {
                CallFunctionOnce();
            }
        }
    }

    private void CallFunctionOnce()
    {
        callCount++;

        Debug.Log($"Function called -> Count: {callCount}");

        // Put your custom code here...
        ChangeMode();
        changePanel.SetActive(true);
        // Pause game for specified duration
        StartCoroutine(PauseGameForSeconds(pauseDuration));
    }

    private IEnumerator PauseGameForSeconds(float seconds)
    {
        Time.timeScale = 0f;  // Pause game
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = 1f;
        changePanel.SetActive(false);// Resume game
    }

    void ChangeMode()
    {
        DestroyByTagAll.instance.DestroyAllWithTwoTags();
        listCounter++;

        changeUI();

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

        if (tempbg != null)
        {
            Destroy(tempbg );
            backGround = waveSOs[listCounter].backGround;
            tempbg = Instantiate(backGround);
        }
        else
        {
            backGround = waveSOs[listCounter].backGround;
             tempbg = Instantiate(backGround);
        }

        Abilitytext.text = waveSOs[listCounter].givenSkillDiscriptionstxt;
        nextrandtext.text = waveSOs[listCounter].nextUnstableThingtxt;
    }

    void changeUI()
    {
        if (waveSOs[listCounter].isDobleJumpActivated)
            skillOne.SetActive(false);

        if (waveSOs[listCounter].isDashActivated)
            skillTwo.SetActive(false);

        if (waveSOs[listCounter].isGlideActivated)
            skillThree.SetActive(false);
    }

}
