using System.Collections.Generic;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField, Min(1)] private int gridWidth = 10;
    [SerializeField, Min(1)] private int gridHeight = 10;
    [SerializeField, Min(0.0001f)] private float cellSize = 1f;
    [Tooltip("Center point of the grid in world space.")]
    [SerializeField] private Vector3 origin;

    private readonly Dictionary<Vector2Int, GameObject> occupiedCells =
        new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<GameObject, List<Vector2Int>> cellsByObject =
        new Dictionary<GameObject, List<Vector2Int>>();

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public Vector3 Origin => origin;

    private void OnValidate()
    {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(0.0001f, cellSize);
    }

    public Vector2Int WorldPositionToCell(Vector3 worldPosition)
    {
        Vector3 gridStart = GetGridStartPosition();
        Vector3 localPosition = worldPosition - gridStart;
        return new Vector2Int(
            Mathf.FloorToInt(localPosition.x / cellSize),
            Mathf.FloorToInt(localPosition.z / cellSize));
    }

    public Vector3 GetObjectWorldPosition(Vector2Int anchorCell, int footprintSideLength)
    {
        float footprintOffset = footprintSideLength * cellSize * 0.5f;
        return GetGridStartPosition() + new Vector3(
            anchorCell.x * cellSize + footprintOffset,
            0f,
            anchorCell.y * cellSize + footprintOffset);
    }

    private Vector3 GetGridStartPosition()
    {
        return origin + new Vector3(
            -gridWidth * cellSize * 0.5f,
            0f,
            -gridHeight * cellSize * 0.5f);
    }

    public List<Vector2Int> GetFootprintCells(Vector2Int anchorCell, int footprintSideLength)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        if (footprintSideLength < 1)
        {
            return cells;
        }

        for (int x = 0; x < footprintSideLength; x++)
        {
            for (int y = 0; y < footprintSideLength; y++)
            {
                cells.Add(new Vector2Int(anchorCell.x + x, anchorCell.y + y));
            }
        }

        return cells;
    }

    public bool IsCellInBounds(Vector2Int cell)
    {
        return cell.x >= 0
            && cell.y >= 0
            && cell.x < gridWidth
            && cell.y < gridHeight;
    }

    public bool IsValidPlacement(Vector2Int anchorCell, PlaceableObjectDefinition definition)
    {
        return IsValidPlacement(anchorCell, definition, out _);
    }

    public bool IsValidPlacement(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        out string reason)
    {
        if (definition == null)
        {
            reason = "A placeable object definition is required.";
            return false;
        }

        if (definition.Prefab == null)
        {
            reason = "The placeable object definition has no prefab assigned.";
            return false;
        }

        int footprintSideLength = definition.FootprintSideLength;
        if (footprintSideLength < 1)
        {
            reason = "The footprint side length must be at least one cell.";
            return false;
        }

        for (int x = 0; x < footprintSideLength; x++)
        {
            for (int y = 0; y < footprintSideLength; y++)
            {
                Vector2Int cell = new Vector2Int(anchorCell.x + x, anchorCell.y + y);
                if (!IsCellInBounds(cell))
                {
                    reason = $"Cell {cell} is outside the grid.";
                    return false;
                }

                if (occupiedCells.ContainsKey(cell))
                {
                    reason = $"Cell {cell} is already occupied.";
                    return false;
                }
            }
        }

        reason = null;
        return true;
    }

    public bool TryPlaceObject(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        out GameObject placedObject)
    {
        placedObject = null;
        if (!IsValidPlacement(anchorCell, definition))
        {
            return false;
        }

        Vector3 worldPosition = GetObjectWorldPosition(
            anchorCell,
            definition.FootprintSideLength);
        placedObject = Instantiate(
            definition.Prefab,
            worldPosition,
            definition.Prefab.transform.rotation,
            transform);

        List<Vector2Int> cells = GetFootprintCells(
            anchorCell,
            definition.FootprintSideLength);
        foreach (Vector2Int cell in cells)
        {
            occupiedCells.Add(cell, placedObject);
        }

        cellsByObject.Add(placedObject, cells);
        return true;
    }

    public bool RemoveObject(GameObject placedObject)
    {
        if (placedObject == null || !cellsByObject.TryGetValue(placedObject, out List<Vector2Int> cells))
        {
            return false;
        }

        foreach (Vector2Int cell in cells)
        {
            if (occupiedCells.TryGetValue(cell, out GameObject occupant) && occupant == placedObject)
            {
                occupiedCells.Remove(cell);
            }
        }

        cellsByObject.Remove(placedObject);
        Destroy(placedObject);
        return true;
    }
}
