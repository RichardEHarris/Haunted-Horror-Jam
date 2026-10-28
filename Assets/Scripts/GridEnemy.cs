using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridEnemy : MonoBehaviour
{
    private GridMap gridMap;
    private GameManager gameManager;
    private Vector2Int currentCell;
    private float movementSpeed;
    private float retryInterval;
    private float baseMachineDamage;
    private float maxHealth;
    private float currentHealth;
    private int currencyReward;
    private bool initialized;
    private bool isDead;

    public Vector2Int CurrentCell => currentCell;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => initialized && !isDead;

    public void Initialize(
        GridMap owningGrid,
        GameManager owningGameManager,
        Vector2Int startingCell,
        float speed,
        float pathRetryInterval,
        float damage,
        float startingHealth,
        int killReward)
    {
        gridMap = owningGrid;
        gameManager = owningGameManager;
        currentCell = startingCell;
        movementSpeed = Mathf.Max(0.01f, speed);
        retryInterval = Mathf.Max(0.05f, pathRetryInterval);
        baseMachineDamage = Mathf.Max(0f, damage);
        maxHealth = Mathf.Max(0.01f, startingHealth);
        currentHealth = maxHealth;
        currencyReward = Mathf.Max(0, killReward);

        initialized = true;
        StartCoroutine(MoveTowardsBase());
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        if (damage < 0f)
        {
            Debug.LogError("Enemy damage cannot be negative.", this);
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        SetHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator MoveTowardsBase()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        WaitForSeconds retryWait = new WaitForSeconds(retryInterval);

        while (initialized)
        {
            if (gridMap.IsBaseMachineCell(currentCell))
            {
                DamageBaseMachineAndRemoveSelf();
                yield break;
            }

            if (!gridMap.TryFindPathToBase(this, currentCell, path) || path.Count == 0)
            {
                yield return retryWait;
                continue;
            }

            Vector2Int nextCell = path[0];
            if (!gridMap.TryBeginEnemyMove(this, currentCell, nextCell))
            {
                yield return retryWait;
                continue;
            }

            Vector3 targetPosition = gridMap.GetObjectWorldPosition(nextCell, 1);
            while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    movementSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
            if (!gridMap.CompleteEnemyMove(this, currentCell, nextCell))
            {
                yield return retryWait;
                continue;
            }

            currentCell = nextCell;
            if (gridMap.IsBaseMachineCell(currentCell))
            {
                DamageBaseMachineAndRemoveSelf();
                yield break;
            }
        }
    }

    public void SetHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth < 0f)
        {
            maxHealth = 0f;
        }

        RectTransform fillImage = transform.Find("Healthbar/Background/Fill").GetComponent<RectTransform>();

        if (fillImage != null)
        {
            fillImage.sizeDelta = new Vector2(Mathf.Clamp01(currentHealth / maxHealth) * 100, fillImage.sizeDelta.y);
        }
    }

    private void DamageBaseMachineAndRemoveSelf()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        initialized = false;
        if (gameManager != null)
        {
            gameManager.DamageBaseMachine(baseMachineDamage);
        }

        Destroy(gameObject);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        initialized = false;
        if (gameManager != null)
        {
            gameManager.AddCurrency(currencyReward);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        initialized = false;
        if (gridMap != null)
        {
            gridMap.UnregisterEnemy(this, currentCell);
        }
    }
}
