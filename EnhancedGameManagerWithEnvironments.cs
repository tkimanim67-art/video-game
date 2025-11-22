using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class EnhancedGameManagerWithEnvironments : MonoBehaviour
{
    public static EnhancedGameManagerWithEnvironments Instance;
    
    [Header("Game Settings")]
    public int currentWave = 1;
    public int baseZombiesPerWave = 5;
    public float timeBetweenWaves = 10f;
    public float gameStartDelay = 3f;
    
    [Header("Environment Progression")]
    public int wavesPerEnvironment = 3;
    public bool randomEnvironmentOrder = false;
    
    [Header("Zombie Settings")]
    public GameObject[] zombiePrefabs;
    public int maxZombiesAlive = 20;
    
    [Header("UI References")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI zombiesKilledText;
    public TextMeshProUGUI experienceText;
    public TextMeshProUGUI waveCountdownText;
    public TextMeshProUGUI environmentText;
    public GameObject waveStartPanel;
    public TextMeshProUGUI waveStartText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalStatsText;
    
    // Game state
    [HideInInspector] public int totalZombiesKilled = 0;
    [HideInInspector] public int totalExperience = 0;
    [HideInInspector] public bool gameRunning = false;
    
    private int zombiesAlive = 0;
    private int zombiesSpawnedThisWave = 0;
    private float waveCountdown;
    private bool waveInProgress = false;
    private EnhancedPlayerController player;
    private int environmentChangeCounter = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeGame();
    }
    
    void InitializeGame()
    {
        player = FindObjectOfType<EnhancedPlayerController>();
        
        if (player == null)
        {
            Debug.LogError("No player found in scene!");
        }
        
        // Hide UI panels
        if (waveStartPanel != null) waveStartPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Start game after delay
        Invoke("StartGame", gameStartDelay);
    }
    
    void StartGame()
    {
        gameRunning = true;
        waveCountdown = timeBetweenWaves;
        UpdateWaveUI();
        UpdateStatsUI();
        UpdateEnvironmentUI();
        
        Debug.Log("Game Started! First wave incoming...");
        ShowMessage("ZOMBIE FRONTIER: OUTBREAK", 3f);
    }
    
    void Update()
    {
        if (!gameRunning) return;
        
        if (!waveInProgress)
        {
            waveCountdown -= Time.deltaTime;
            UpdateWaveCountdown(waveCountdown);
            
            if (waveCountdown <= 0f)
            {
                StartWave();
            }
        }
    }
    
    void StartWave()
    {
        waveInProgress = true;
        zombiesSpawnedThisWave = 0;
        
        // Check for environment change
        if (ShouldChangeEnvironment())
        {
            ChangeEnvironment();
            return;
        }
        
        int zombiesThisWave = CalculateZombiesThisWave();
        StartCoroutine(SpawnWave(zombiesThisWave));
        
        ShowWaveStart(currentWave);
        UpdateWaveUI();
    }
    
    bool ShouldChangeEnvironment()
    {
        return currentWave > 1 && (currentWave - 1) % wavesPerEnvironment == 0 && environmentChangeCounter < currentWave - 1;
    }
    
    void ChangeEnvironment()
    {
        environmentChangeCounter = currentWave - 1;
        
        if (EnvironmentManager.Instance != null)
        {
            if (randomEnvironmentOrder)
            {
                // Random environment (excluding current)
                int currentIndex = EnvironmentManager.Instance.currentEnvironmentIndex;
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, EnvironmentManager.Instance.environments.Length);
                } while (newIndex == currentIndex && EnvironmentManager.Instance.environments.Length > 1);
                
                EnvironmentManager.Instance.LoadEnvironment(newIndex);
            }
            else
            {
                // Sequential environment
                EnvironmentManager.Instance.NextEnvironment();
            }
        }
        
        // Show environment change message
        ShowMessage($"ENTERING: {EnvironmentManager.Instance.GetCurrentEnvironment().environmentName.ToUpper()}", 3f);
        
        // Start wave after environment transition
        Invoke("DelayedWaveStart", 3f);
    }
    
    void DelayedWaveStart()
    {
        int zombiesThisWave = CalculateZombiesThisWave();
        StartCoroutine(SpawnWave(zombiesThisWave));
        UpdateEnvironmentUI();
    }
    
    IEnumerator SpawnWave(int zombieCount)
    {
        for (int i = 0; i < zombieCount; i++)
        {
            if (zombiesAlive < maxZombiesAlive)
            {
                SpawnZombie();
                yield return new WaitForSeconds(2f - (currentWave * 0.1f)); // Faster spawning as waves progress
            }
            else
            {
                yield return new WaitUntil(() => zombiesAlive < maxZombiesAlive);
                SpawnZombie();
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    void SpawnZombie()
    {
        Transform spawnPoint = GetSpawnPoint();
        if (spawnPoint == null) return;
        
        GameObject zombiePrefab = GetZombiePrefabForWave();
        GameObject zombie = Instantiate(zombiePrefab, spawnPoint.position, Quaternion.identity);
        zombie.name = $"Zombie_Wave{currentWave}";
        
        // Configure zombie based on environment
        ZombieController zombieController = zombie.GetComponent<ZombieController>();
        if (zombieController != null)
        {
            zombieController.experienceReward = CalculateExperienceReward();
            // ConfigureZombieForEnvironment(zombieController);
        }
        
        zombiesAlive++;
        zombiesSpawnedThisWave++;
    }
    
    Transform GetSpawnPoint()
    {
        if (EnvironmentManager.Instance != null)
        {
            return EnvironmentManager.Instance.GetRandomSpawnPoint();
        }
        
        // Fallback to old spawn system
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
        
        return null;
    }
    
    GameObject GetZombiePrefabForWave()
    {
        if (zombiePrefabs != null && zombiePrefabs.Length > 0)
        {
            int prefabIndex = Mathf.Min(currentWave - 1, zombiePrefabs.Length - 1);
            return zombiePrefabs[prefabIndex];
        }
        
        return CreateBasicZombiePrefab();
    }
    
    GameObject CreateBasicZombiePrefab()
    {
        GameObject zombie = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        zombie.name = "BasicZombie";
        
        Rigidbody rb = zombie.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        zombie.AddComponent<UnityEngine.AI.NavMeshAgent>();
        ZombieController controller = zombie.AddComponent<ZombieController>();
        
        Renderer renderer = zombie.GetComponent<Renderer>();
        renderer.material.color = GetZombieColorForWave();
        
        zombie.tag = "Enemy";
        
        return zombie;
    }
    
    Color GetZombieColorForWave()
    {
        return currentWave switch
        {
            1 => Color.red,
            2 => new Color(1f, 0.5f, 0f),
            3 => new Color(0.8f, 0.2f, 0.8f),
            _ => new Color(0.3f, 0.1f, 0.1f)
        };
    }
    
    int CalculateZombiesThisWave()
    {
        return baseZombiesPerWave + (currentWave * 2);
    }
    
    int CalculateExperienceReward()
    {
        return 20 + (currentWave * 5);
    }
    
    public void ZombieKilled(int experienceReward)
    {
        zombiesAlive--;
        totalZombiesKilled++;
        totalExperience += experienceReward;
        
        if (player != null)
        {
            player.AddExperience(experienceReward);
            player.AddKill();
        }
        
        UpdateStatsUI();
        
        if (zombiesAlive <= 0 && zombiesSpawnedThisWave >= CalculateZombiesThisWave())
        {
            EndWave();
        }
    }
    
    void EndWave()
    {
        waveInProgress = false;
        currentWave++;
        waveCountdown = timeBetweenWaves;
        
        UpdateWaveUI();
        ShowWaveComplete();
        
        Debug.Log($"Wave {currentWave-1} complete! Prepare for wave {currentWave}");
    }
    
    public void GameOver()
    {
        gameRunning = false;
        waveInProgress = false;
        
        ShowGameOver();
        Debug.Log($"Game Over! Final Stats - Waves: {currentWave-1}, Kills: {totalZombiesKilled}, XP: {totalExperience}");
    }
    
    // UI Methods
    void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = $"WAVE: {currentWave}";
        
        if (player != null)
            player.UpdateWaveUI(currentWave);
    }
    
    void UpdateStatsUI()
    {
        if (zombiesKilledText != null)
            zombiesKilledText.text = $"ZOMBIES KILLED: {totalZombiesKilled}";
        
        if (experienceText != null)
            experienceText.text = $"TOTAL XP: {totalExperience}";
    }
    
    void UpdateEnvironmentUI()
    {
        if (environmentText != null && EnvironmentManager.Instance != null)
        {
            string envName = EnvironmentManager.Instance.GetCurrentEnvironment().environmentName;
            environmentText.text = $"LOCATION: {envName.ToUpper()}";
        }
    }
    
    void UpdateWaveCountdown(float countdown)
    {
        if (waveCountdownText != null)
            waveCountdownText.text = $"NEXT WAVE: {countdown:F1}S";
    }
    
    void ShowWaveStart(int wave)
    {
        if (waveStartPanel != null && waveStartText != null)
        {
            waveStartText.text = $"WAVE {wave} INCOMING!";
            waveStartPanel.SetActive(true);
            Invoke("HideWaveStart", 3f);
        }
    }
    
    void ShowWaveComplete()
    {
        ShowMessage($"WAVE {currentWave-1} COMPLETE!", 2f);
    }
    
    void ShowGameOver()
    {
        if (gameOverPanel != null && finalStatsText != null)
        {
            finalStatsText.text = $"GAME OVER\n\nWaves Survived: {currentWave-1}\nZombies Killed: {totalZombiesKilled}\nTotal XP: {totalExperience}";
            gameOverPanel.SetActive(true);
        }
    }
    
    void ShowMessage(string message, float duration)
    {
        Debug.Log(message);
    }
    
    void HideWaveStart()
    {
        if (waveStartPanel != null)
            waveStartPanel.SetActive(false);
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
