using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    [Header("Zombie Stats")]
    public int health = 50;
    public int damage = 10;
    public float moveSpeed = 3.5f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public int experienceReward = 25;
    
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    
    protected Transform player;
    protected NavMeshAgent agent;
    protected float lastAttackTime;
    protected bool isDead = false;
    protected Renderer zombieRenderer;
    
    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        zombieRenderer = GetComponent<Renderer>();
        
        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange - 0.5f;
    }
    
    protected virtual void Update()
    {
        if (isDead || player == null) return;
        
        // Always chase player
        agent.SetDestination(player.position);
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
        
        // Visual feedback when close to player
        if (distanceToPlayer < 5f)
        {
            zombieRenderer.material.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time, 0.5f));
        }
    }
    
    protected virtual void AttackPlayer()
    {
        lastAttackTime = Time.time;
        
        // Damage player
        EnhancedPlayerController playerController = player.GetComponent<EnhancedPlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(damage);
            Debug.Log("Zombie attacked player for " + damage + " damage!");
        }
    }
    
    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        
        // Visual feedback
        zombieRenderer.material.color = Color.yellow;
        Invoke("ResetColor", 0.2f);
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    protected virtual void ResetColor()
    {
        if (!isDead)
        {
            zombieRenderer.material.color = Color.red;
        }
    }
    
    protected virtual void Die()
    {
        isDead = true;
        agent.isStopped = true;
        
        // Give experience to player
        EnhancedPlayerController playerController = player.GetComponent<EnhancedPlayerController>();
        if (playerController != null)
        {
            playerController.AddExperience(experienceReward);
            playerController.AddKill();
        }
        
        EnhancedGameManagerWithEnvironments.Instance.ZombieKilled(experienceReward);
        
        // Visual death effect
        zombieRenderer.material.color = Color.gray;
        Destroy(gameObject, 2f);
    }
}
