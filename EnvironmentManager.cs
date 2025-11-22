using UnityEngine;
using System.Collections;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance;
    
    [System.Serializable]
    public class Environment
    {
        public string environmentName;
        public GameObject environmentPrefab;
        public Transform[] spawnPoints;
        public Material skyboxMaterial;
        public Color ambientLight = Color.white;
        public float fogDensity = 0.01f;
        public AudioClip ambientSound;
    }
    
    [Header("Available Environments")]
    public Environment[] environments;
    public int currentEnvironmentIndex = 0;
    
    [Header("Environment Transition")]
    public float transitionTime = 2f;
    public ParticleSystem transitionParticles;
    
    private GameObject currentEnvironment;
    private AudioSource ambientAudioSource;
    
    // Event for environment changes
    public System.Action<string> OnEnvironmentChanged;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        ambientAudioSource = GetComponent<AudioSource>();
        if (ambientAudioSource == null)
            ambientAudioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Start()
    {
        LoadEnvironment(0); // Start with first environment
    }
    
    public void LoadEnvironment(int environmentIndex)
    {
        if (environmentIndex < 0 || environmentIndex >= environments.Length)
        {
            Debug.LogError("Invalid environment index!");
            return;
        }
        
        StartCoroutine(TransitionToEnvironment(environmentIndex));
    }
    
    private IEnumerator TransitionToEnvironment(int newIndex)
    {
        // Play transition effect
        if (transitionParticles != null)
            transitionParticles.Play();
        
        // Fade out current environment
        yield return StartCoroutine(FadeOutEnvironment());
        
        // Load new environment
        Environment newEnv = environments[newIndex];
        currentEnvironmentIndex = newIndex;
        
        // Destroy current environment
        if (currentEnvironment != null)
            Destroy(currentEnvironment);
        
        // Instantiate new environment
        currentEnvironment = Instantiate(newEnv.environmentPrefab, Vector3.zero, Quaternion.identity);
        currentEnvironment.name = newEnv.environmentName;
        
        // Update lighting and skybox
        RenderSettings.skybox = newEnv.skyboxMaterial;
        RenderSettings.ambientLight = newEnv.ambientLight;
        RenderSettings.fogDensity = newEnv.fogDensity;
        DynamicGI.UpdateEnvironment();
        
        // Update ambient sound
        if (ambientAudioSource != null && newEnv.ambientSound != null)
        {
            ambientAudioSource.clip = newEnv.ambientSound;
            ambientAudioSource.loop = true;
            ambientAudioSource.Play();
        }
        
        // Notify environment change
        OnEnvironmentChanged?.Invoke(newEnv.environmentName);
        
        // Fade in new environment
        yield return StartCoroutine(FadeInEnvironment());
        
        Debug.Log($"Loaded environment: {newEnv.environmentName}");
    }
    
    private IEnumerator FadeOutEnvironment()
    {
        // Implement fade out logic (could use UI fade image)
        yield return new WaitForSeconds(transitionTime / 2);
    }
    
    private IEnumerator FadeInEnvironment()
    {
        // Implement fade in logic
        yield return new WaitForSeconds(transitionTime / 2);
    }
    
    public void NextEnvironment()
    {
        int nextIndex = (currentEnvironmentIndex + 1) % environments.Length;
        LoadEnvironment(nextIndex);
    }
    
    public Environment GetCurrentEnvironment()
    {
        return environments[currentEnvironmentIndex];
    }
    
    public Transform GetRandomSpawnPoint()
    {
        Environment currentEnv = environments[currentEnvironmentIndex];
        if (currentEnv.spawnPoints.Length > 0)
        {
            return currentEnv.spawnPoints[Random.Range(0, currentEnv.spawnPoints.Length)];
        }
        return null;
    }
}
