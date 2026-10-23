using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private GridMap gridMap;
    [SerializeField]
    private List<PlaceableObjectDefinition> placeableObjectList =
        new List<PlaceableObjectDefinition>();

    private void Awake()
    {
        if (gridMap == null)
            gridMap = FindAnyObjectByType<GridMap>();
        if (gridMap == null)
        {
            Debug.LogError("No GridMap is assigned to the GameManager.", this);
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
