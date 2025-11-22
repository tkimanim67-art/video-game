using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    public string weaponName;
    public int damage = 10;
    public float range = 100f;
    public float fireRate = 0.5f;
    public int maxAmmo = 30;
    public int currentAmmo;
    
    [Header("Upgrade System")]
    public int upgradeLevel = 1;
    public int damagePerUpgrade = 5;
    
    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    public Camera playerCamera;
    private AudioSource audioSource;
    
    private float nextFireTime;
    
    void Start()
    {
        currentAmmo = maxAmmo;
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        audioSource = GetComponent<AudioSource>();
        
        // Create visual effects if they don't exist
        if (muzzleFlash == null)
        {
            CreateMuzzleFlash();
        }
    }
    
    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && currentAmmo > 0)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
        
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            Reload();
        }
    }
    
    void Shoot()
    {
        currentAmmo--;
        
        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // Play sound
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        // Raycast for hit detection
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            // Create impact effect
            if (impactEffect != null)
            {
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
            
            // Damage enemy
            ZombieController zombie = hit.transform.GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                Debug.Log($"Hit zombie for {damage} damage!");
            }
        }
    }
    
    void Reload()
    {
        Debug.Log("Reloading...");
        currentAmmo = maxAmmo;
    }
    
    public void UpgradeDamage(int extraDamage = 0)
    {
        if (extraDamage == 0)
            extraDamage = damagePerUpgrade;
            
        damage += extraDamage;
        upgradeLevel++;
        Debug.Log($"{weaponName} upgraded to level {upgradeLevel}! Damage: {damage}");
    }
    
    void CreateMuzzleFlash()
    {
        GameObject flashObj = new GameObject("MuzzleFlash");
        flashObj.transform.SetParent(transform);
        flashObj.transform.localPosition = Vector3.zero;
        
        muzzleFlash = flashObj.AddComponent<ParticleSystem>();
        
        var main = muzzleFlash.main;
        main.startSpeed = 5f;
        main.startLifetime = 0.1f;
        main.startSize = 0.1f;
        main.startColor = Color.yellow;
    }
}
