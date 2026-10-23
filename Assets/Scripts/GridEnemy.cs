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
    private bool initialized;

    public Vector2Int CurrentCell => currentCell;

    public void Initialize(
        GridMap owningGrid,
        GameManager owningGameManager,
        Vector2Int startingCell,
        float speed,
        float pathRetryInterval,
        float damage)
    {
        gridMap = owningGrid;
        gameManager = owningGameManager;
        currentCell = startingCell;
        movementSpeed = Mathf.Max(0.01f, speed);
        retryInterval = Mathf.Max(0.05f, pathRetryInterval);
        baseMachineDamage = Mathf.Max(0f, damage);
        initialized = true;
        StartCoroutine(MoveTowardsBase());
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

    private void DamageBaseMachineAndRemoveSelf()
    {
        initialized = false;
        if (gameManager != null)
        {
            gameManager.DamageBaseMachine(baseMachineDamage);
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
