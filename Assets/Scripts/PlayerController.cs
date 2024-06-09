using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private FloatingJoystick _joystick;
    [SerializeField] private Transform headPoint;
    [SerializeField] private GameObject plantButton;
    [SerializeField] private TextMeshProUGUI redTeamWinsText;
    [SerializeField] private TextMeshProUGUI blueTeamWinsText;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private Animator anim;

    private GameObject c4;
    public float targetTime = 10.0f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public int medAttempts = 2;
    public bool readyToHeal = true;
    public int healCooldown = 10;

    [SerializeField] private float sightRange = 10f;
    [SerializeField] private LayerMask whatIsEnemy;
    public GameObject closestEnemy;

    public float shootCooldown = 1f; // Time in seconds between shots
    private bool canShoot = true;
    private bool canPlant = false;
    private bool planted = false;
    private bool hasTheBomb = false;
    private bool shooting = false;
    private bool walking = false;
    private bool planting = false;

    public float health;
    public Slider healthSlider;

    public float projectileForce = 10f;

    private void Awake()
    {
        plantButton.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("C4"))
        {
            other.transform.position = headPoint.position;
            other.GetComponent<BoxCollider>().enabled = false;
            other.transform.SetParent(headPoint);
            plantButton.SetActive(true);
            hasTheBomb = true;
            c4 = other.gameObject;
        }
        if (other.CompareTag("EnemyBullet"))
        {
            TakeDamage(15);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PlantZone"))
        {
            if(hasTheBomb)
            canPlant = true;
        }
    }
    public void medHeal()
    {
        if(readyToHeal && medAttempts > 0)
        {
            readyToHeal = false;
            medAttempts--;
            TakeDamage(-30);
            // implement throwCooldown
            Invoke(nameof(ResetHeal), healCooldown);
        }
    }
    private void ResetHeal()
    {
        readyToHeal = true;
    }
    private GameObject FindClosestEnemy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightRange, whatIsEnemy);
        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = collider.gameObject;
            }
        }

        return closestEnemy;
    }
    private void Update()
    {
        float threshold = 0.3f;
        if (_rigidBody.velocity.magnitude < threshold * Time.deltaTime)
        {
            walking = false;
        }
        else
        {
            walking = true;
        }

        // Update animation states based on player actions and booleans
        anim.SetBool("IsIdle", !walking && !shooting && !planting && health >= 0);
        anim.SetBool("IsRunning", walking && !shooting && !planting && health >= 0);
        anim.SetBool("IsAiming", !walking && shooting && !planting && health >= 0);
        anim.SetBool("IsShootwalking", walking && shooting && !planting && health >= 0);
        anim.SetBool("IsPlanting", planting && !walking);
        anim.SetBool("IsDying", health <= 0); // Assuming health <= 0 triggers death animation

        if (planted)
        targetTime -= Time.deltaTime;

        if (targetTime <= 0.0f && planted)
        {
            timerEnded();
        }
    }
    private void FixedUpdate()
    {
        closestEnemy = FindClosestEnemy();

        // Do something with the closest enemy ( target)
        if (closestEnemy != null)
        {
            shooting = true;

            transform.LookAt(closestEnemy.transform);
        }
        else
            shooting = false;

        JoystickMovement();

        //Check if character is moving
        if ((_joystick.Horizontal != 0 || _joystick.Vertical != 0) && closestEnemy == null)
        {
            //Rotation
            transform.rotation = Quaternion.LookRotation(_rigidBody.velocity);
        }
    }
    void timerEnded()
    {
        c4.GetComponentInChildren<ParticleSystem>().Play();
        redTeamWinsText.enabled = true;
        planted = false;
    }
    public void PB()
    {
        StartCoroutine(PlantBomb());
    }
    public IEnumerator PlantBomb()
    {
        // Check if player has the bomb
        if (canPlant)
        {
            // Raycast down to find nearest ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
            {
                // Calculate position a little further from the player
                Vector3 plantingOffset = new Vector3(0f, 1f, 6f); // Adjust offset as needed

                Vector3 plantingPosition = hit.point + plantingOffset;

                // Check if new position is valid (not colliding with anything)
                if (Physics.OverlapSphere(plantingPosition, 0.2f, LayerMask.GetMask("Ground")).Length == 0)
                {
                    // Place the bomb at the valid position
                    GameObject bomb = Instantiate(c4, plantingPosition, Quaternion.identity);

                    //Play planting animation/sound effect
                    planting = true;
                    _joystick.enabled = false;
                    yield return new WaitForSeconds(5f);
                    _joystick.enabled = true;
                    planting = false;
                    // Remove bomb from player inventory
                    Destroy(c4);

                    c4 = bomb;
                    c4.transform.localScale = new Vector3(22f, 22f, 22f);

                    planted = true;

                    plantButton.SetActive(false);
                }
                else
                {
                    // Show error message or handle no valid position situation (optional)
                    Debug.Log("No valid spot to plant bomb nearby!");
                }
            }
            else
            {
                // Show error message or handle no ground found situation (optional)
                Debug.Log("No ground found to plant bomb!");
            }
        }
    }
    public void TakeDamage(int damage)
    {
        health -= damage;
        healthSlider.value = health;

        if (health <= 0)
        {
            blueTeamWinsText.enabled = true;
            Invoke(nameof(DestroyPlayer), 0.1f); 
        }
    }
    private void DestroyPlayer()
    {
        gameObject.GetComponent<CapsuleCollider>().height = 2.2f;
        gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, 2.25f, 0);
        gameObject.GetComponent<PlayerController>().enabled = false;
    }
    private void JoystickMovement()
    {
        walking = true;
        _rigidBody.velocity = new Vector3(_joystick.Horizontal * _moveSpeed, _rigidBody.velocity.y, _joystick.Vertical * _moveSpeed);
    }
    public void Shoot()
    {
        if (canShoot)
        {
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Destroy(projectile, 4f);
            // Get the player's forward direction
            Vector3 forwardDirection = transform.forward;
            // Optionally add force to the projectile in the forward direction
            projectile.GetComponent<Rigidbody>().AddForce(forwardDirection * projectileForce);

            // Start cooldown timer
            canShoot = false;
            StartCoroutine(ShootCooldown());
        }
    }
    
    IEnumerator ShootCooldown()
    {
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

}
