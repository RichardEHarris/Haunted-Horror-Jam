using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private int currency = 0;
    [SerializeField] private GridMap gridMap;
    [SerializeField]
    private List<PlaceableObjectDefinition> placeableObjectList =
        new List<PlaceableObjectDefinition>();
    [SerializeField] private GridEnemySettings enemySettings;

    private void Awake()
    {
        if (gridMap == null)
            gridMap = FindAnyObjectByType<GridMap>();
        if (gridMap == null)
        {
            Debug.LogError("No GridMap is assigned to the GameManager.", this);
        }
    }

    public float Health => health;
    public int Currency => currency;

    public void DamageBaseMachine(float damage)
    {
        if (damage < 0f)
        {
            Debug.LogError("Base Machine damage cannot be negative.", this);
            return;
        }

        health = Mathf.Max(0f, health - damage);
    }

    public void AddCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Currency reward cannot be negative.", this);
            return;
        }

        currency += amount;
    }

    private void Start()
    {
        SpawnInitialEnemies();
    }

    private void SpawnInitialEnemies()
    {
        if (enemySettings == null || enemySettings.InitialEnemyCount == 0)
        {
            return;
        }

        if (gridMap == null)
        {
            Debug.LogError("Cannot spawn enemies because no GridMap is assigned.", this);
            return;
        }

        if (enemySettings.EnemyPrefab == null)
        {
            Debug.LogError("Enemy settings do not have an enemy prefab assigned.", this);
            return;
        }

        System.Random random = new System.Random(enemySettings.Seed);
        int spawnedCount = 0;
        for (int i = 0; i < enemySettings.InitialEnemyCount; i++)
        {
            if (!gridMap.TryGetRandomEdgeSpawnCell(random, out Vector2Int spawnCell)
                || !gridMap.TrySpawnEnemy(
                    enemySettings.EnemyPrefab,
                    this,
                    spawnCell,
                    enemySettings.MovementSpeed,
                    enemySettings.RetryInterval,
                    enemySettings.BaseMachineDamage,
                    enemySettings.MaxHealth,
                    enemySettings.CurrencyReward,
                    out _))
            {
                break;
            }

            spawnedCount++;
        }

        if (spawnedCount < enemySettings.InitialEnemyCount)
        {
            Debug.LogWarning(
                $"Spawned {spawnedCount} of {enemySettings.InitialEnemyCount} requested enemies. " +
                "There may not be enough walkable edge cells.",
                this);
        }
    }

    public void ToggleGridlines()
    {
        gridMap.ToggleGridlines();
    }

    public bool PlaceSelectedObject(
        Vector2 screenPosition,
        Camera placementCamera,
        out string failureReason)
    {
        if (gridMap == null)
        {
            failureReason = "No GridMap is assigned to the GameManager.";
            return false;
        }

        if (placeableObjectList == null || placeableObjectList.Count == 0)
        {
            failureReason = "The placeable object list is empty.";
            return false;
        }

        PlaceableObjectDefinition definition = placeableObjectList[0];
        if (definition == null)
        {
            failureReason = "The first placeable object definition is not assigned.";
            return false;
        }

        if (placementCamera == null)
        {
            failureReason = "No placement camera is available.";
            return false;
        }

        Ray pointerRay = placementCamera.ScreenPointToRay(screenPosition);
        Plane gridPlane = new Plane(Vector3.up, gridMap.Origin);
        if (!gridPlane.Raycast(pointerRay, out float distance))
        {
            failureReason = "The pointer ray does not intersect the grid plane.";
            return false;
        }

        Vector3 worldPosition = pointerRay.GetPoint(distance);
        Vector2Int targetCell = gridMap.WorldPositionToCell(worldPosition);
        if (!gridMap.TryPlaceObject(targetCell, definition, out _, out failureReason))
        {
            return false;
        }

        failureReason = null;
        return true;
    }
}
