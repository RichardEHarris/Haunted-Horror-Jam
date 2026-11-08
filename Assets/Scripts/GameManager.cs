using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;
    [SerializeField] private int currency = 5;
    [SerializeField] private GridMap gridMap;
    [SerializeField]
    private List<PlaceableObjectDefinition> placeableObjectList =
        new List<PlaceableObjectDefinition>();
    [SerializeField] private TowerSelectionMenu towerSelectionMenu;
    [SerializeField] private WaveManager waveManager;

    private PlaceableObjectDefinition selectedPlaceableObject;
    [SerializeField, Range(-1f, 1f)] private float refundMultiplier = 0.5f;

    private void Awake()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);

        if (GridMap == null)
            GridMap = FindAnyObjectByType<GridMap>();
        if (GridMap == null)
        {
            Debug.LogError("No GridMap is assigned to the GameManager.", this);
        }

        if (placeableObjectList != null)
        {
            foreach (PlaceableObjectDefinition definition in placeableObjectList)
            {
                if (definition != null)
                {
                    selectedPlaceableObject = definition;
                    break;
                }
            }
        }

        if (selectedPlaceableObject == null)
        {
            Debug.LogWarning("No valid placeable objects are assigned to the GameManager.", this);
        }

        if (towerSelectionMenu == null)
        {
            towerSelectionMenu = GetComponent<TowerSelectionMenu>();
        }

        if (towerSelectionMenu != null)
        {
            towerSelectionMenu.Initialize(this);
        }

        if (waveManager == null)
        {
            waveManager = GetComponent<WaveManager>();
        }
    }

    public float Health => health;
    public float MaxHealth => maxHealth;
    public int Currency => currency;
    public IReadOnlyList<PlaceableObjectDefinition> PlaceableObjects => placeableObjectList;
    public PlaceableObjectDefinition SelectedPlaceableObject => selectedPlaceableObject;

    public GridMap GridMap { get => gridMap; set => gridMap = value; }

    public bool SelectPlaceableObject(PlaceableObjectDefinition definition)
    {
        if (definition == null || placeableObjectList == null || !placeableObjectList.Contains(definition))
        {
            return false;
        }

        selectedPlaceableObject = definition;
        if (towerSelectionMenu != null)
        {
            towerSelectionMenu.RefreshSelection();
        }

        return true;
    }

    public void DamageBaseMachine(float damage)
    {
        if (damage < 0f)
        {
            Debug.LogError("Base Machine damage cannot be negative.", this);
            return;
        }

        health = Mathf.Clamp(health - damage, 0f, maxHealth);
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
        
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        health = Mathf.Clamp(health, 0f, maxHealth);
    }

    public void ToggleGridlines()
    {
        GridMap.ToggleGridlines();
    }

    public bool PlaceSelectedObject(
        Vector2 screenPosition,
        Camera placementCamera,
        out string failureReason)
    {
        if (GridMap == null)
        {
            failureReason = "No GridMap is assigned to the GameManager.";
            return false;
        }

        if (selectedPlaceableObject == null)
        {
            failureReason = "No placeable object is selected.";
            return false;
        }

        if (selectedPlaceableObject.Cost < 0)
        {
            Debug.LogError(
                $"Placeable object '{selectedPlaceableObject.ObjectName}' has a negative cost.",
                selectedPlaceableObject);
            failureReason = "The selected object has an invalid cost.";
            return false;
        }

        if (currency < selectedPlaceableObject.Cost)
        {
            failureReason = "Not enough currency to place the selected object.";
            return false;
        }

        if (placementCamera == null)
        {
            failureReason = "No placement camera is available.";
            return false;
        }

        Ray pointerRay = placementCamera.ScreenPointToRay(screenPosition);
        Plane gridPlane = new Plane(Vector3.up, GridMap.Origin);
        if (!gridPlane.Raycast(pointerRay, out float distance))
        {
            failureReason = "The pointer ray does not intersect the grid plane.";
            return false;
        }

        Vector3 worldPosition = pointerRay.GetPoint(distance);
        Vector2Int targetCell = GridMap.WorldPositionToCell(worldPosition);
        if (!GridMap.TryPlacePlayerObject(
                targetCell,
                selectedPlaceableObject,
                out _,
                out failureReason))
        {
            return false;
        }

        currency -= selectedPlaceableObject.Cost;
        failureReason = null;
        return true;
    }

    public bool SellPlacedObject(
        Vector2 screenPosition,
        Camera placementCamera,
        out string failureReason)
    {
        if (GridMap == null)
        {
            failureReason = "No GridMap is assigned to the GameManager.";
            return false;
        }

        if (placementCamera == null)
        {
            failureReason = "No placement camera is available.";
            return false;
        }

        Ray pointerRay = placementCamera.ScreenPointToRay(screenPosition);
        Plane gridPlane = new Plane(Vector3.up, GridMap.Origin);
        if (!gridPlane.Raycast(pointerRay, out float distance))
        {
            failureReason = "The pointer ray does not intersect the grid plane.";
            return false;
        }

        Vector3 worldPosition = pointerRay.GetPoint(distance);
        Vector2Int targetCell = GridMap.WorldPositionToCell(worldPosition);
        if (!GridMap.TryGetPlayerPlacedObjectAtCell(
                targetCell,
                out GameObject placedObject,
                out PlaceableObjectDefinition definition))
        {
            failureReason = "There is no player-placed object to sell at this location.";
            return false;
        }

        if (definition.Cost < 0)
        {
            Debug.LogError(
                $"Placeable object '{definition.ObjectName}' has a negative cost.",
                definition);
            failureReason = "The selected object has an invalid sale value.";
            return false;
        }

        if (!GridMap.RemoveObject(placedObject))
        {
            failureReason = "The selected object could not be removed.";
            return false;
        }

        int refund = definition.Cost > 0 ? Mathf.CeilToInt(definition.Cost * refundMultiplier) : 0;
        AddCurrency(refund);
        failureReason = null;
        return true;
    }
}
