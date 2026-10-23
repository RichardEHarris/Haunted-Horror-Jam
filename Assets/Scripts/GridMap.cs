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
    [SerializeField] private GridPopulationSettings populationSettings;

    [Header("Gridline Display")]
    [SerializeField] private Material gridLineMaterial;
    [SerializeField] private Color gridLineColor = Color.white;
    [SerializeField, Min(0.001f)] private float gridLineWidth = 0.02f;
    [SerializeField] private float gridLineHeightOffset = 0.01f;
    [SerializeField] private bool gridLinesVisibleAtStart = true;

    private struct PlacementData
    {
        public PlaceableObjectDefinition Definition;
        public Vector2Int AnchorCell;
        public int FootprintSideLength;
    }

    private int[] cellPlacementIndices;
    private readonly List<PlacementData> placements = new List<PlacementData>();
    private readonly Dictionary<GameObject, int> placementIndexByInstance =
        new Dictionary<GameObject, int>();
    private GameObject gridLinesRoot;
    private Material runtimeGridLineMaterial;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public Vector3 Origin => origin;
    public bool AreGridlinesVisible => gridLinesRoot != null && gridLinesRoot.activeSelf;

    private void OnValidate()
    {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(0.0001f, cellSize);
        gridLineWidth = Mathf.Max(0.001f, gridLineWidth);
    }

    private void Awake()
    {
        cellPlacementIndices = new int[gridWidth * gridHeight];
        CreateGridlineDisplay();
        SetGridlinesVisible(gridLinesVisibleAtStart);
        PopulateInitialObjects();
    }

    private void OnDestroy()
    {
        if (runtimeGridLineMaterial != null)
        {
            Destroy(runtimeGridLineMaterial);
        }
    }

    public void ToggleGridlines()
    {
        SetGridlinesVisible(!AreGridlinesVisible);
    }

    public void SetGridlinesVisible(bool visible)
    {
        if (gridLinesRoot == null)
        {
            Debug.LogError("Gridline display has not been initialized.", this);
            return;
        }

        gridLinesRoot.SetActive(visible);
    }

    private void CreateGridlineDisplay()
    {
        gridLinesRoot = new GameObject("Gridlines");
        gridLinesRoot.transform.SetParent(transform, false);

        Material lineMaterial = GetGridlineMaterial();
        if (lineMaterial == null)
        {
            return;
        }

        Vector3 gridStart = GetGridStartPosition();
        float lineY = origin.y + gridLineHeightOffset;
        float gridEndX = gridStart.x + gridWidth * cellSize;
        float gridEndZ = gridStart.z + gridHeight * cellSize;

        for (int x = 0; x <= gridWidth; x++)
        {
            float lineX = gridStart.x + x * cellSize;
            CreateGridline(
                $"Gridline X {x}",
                new Vector3(lineX, lineY, gridStart.z),
                new Vector3(lineX, lineY, gridEndZ),
                lineMaterial);
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            float lineZ = gridStart.z + z * cellSize;
            CreateGridline(
                $"Gridline Z {z}",
                new Vector3(gridStart.x, lineY, lineZ),
                new Vector3(gridEndX, lineY, lineZ),
                lineMaterial);
        }
    }

    private Material GetGridlineMaterial()
    {
        if (gridLineMaterial != null)
        {
            return gridLineMaterial;
        }

        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Sprites/Default");
        }

        if (lineShader == null)
        {
            Debug.LogError("No supported shader was found for the gridline display.", this);
            return null;
        }

        runtimeGridLineMaterial = new Material(lineShader);
        return runtimeGridLineMaterial;
    }

    private void CreateGridline(
        string lineName,
        Vector3 startPosition,
        Vector3 endPosition,
        Material lineMaterial)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(gridLinesRoot.transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        lineRenderer.startWidth = gridLineWidth;
        lineRenderer.endWidth = gridLineWidth;
        lineRenderer.startColor = gridLineColor;
        lineRenderer.endColor = gridLineColor;
        lineRenderer.sharedMaterial = lineMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
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

                if (cellPlacementIndices[GetCellIndex(cell)] != 0)
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
        out GameObject placedObject, out string reason)
    {
        placedObject = null;
        reason = null;
        if (!IsValidPlacement(anchorCell, definition, out reason))
        {
            return false;
        }

        return TryCreatePlacement(anchorCell, definition, out placedObject);
    }

    private bool TryCreatePlacement(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        out GameObject placedObject)
    {
        Vector3 worldPosition = GetObjectWorldPosition(
            anchorCell,
            definition.FootprintSideLength);
        placedObject = Instantiate(
            definition.Prefab,
            worldPosition,
            definition.Prefab.transform.rotation,
            transform);

        int placementIndex = placements.Count;
        placements.Add(new PlacementData
        {
            Definition = definition,
            AnchorCell = anchorCell,
            FootprintSideLength = definition.FootprintSideLength
        });
        placementIndexByInstance.Add(placedObject, placementIndex);

        foreach (Vector2Int cell in GetFootprintCells(anchorCell, definition.FootprintSideLength))
        {
            cellPlacementIndices[GetCellIndex(cell)] = placementIndex + 1;
        }

        return true;
    }

    private void PopulateInitialObjects()
    {
        if (populationSettings == null)
        {
            Debug.LogWarning("No grid population settings are assigned; the grid will start empty.", this);
            return;
        }

        PlaceableObjectDefinition baseMachine = populationSettings.BaseMachine;
        if (baseMachine == null)
        {
            Debug.LogError("Grid population settings require a Base Machine definition.", this);
            return;
        }

        int baseSideLength = baseMachine.FootprintSideLength;
        Vector2Int baseAnchor = new Vector2Int(
            (gridWidth - baseSideLength) / 2,
            (gridHeight - baseSideLength) / 2);
        if (!TryPlaceObject(baseAnchor, baseMachine, out _, out string baseFailureReason))
        {
            Debug.LogError($"Could not place the Base Machine: {baseFailureReason}", this);
            return;
        }

        PlaceTrees();
    }

    private void PlaceTrees()
    {
        int requestedTreeCount = populationSettings.TreeCount;
        if (requestedTreeCount == 0)
        {
            return;
        }

        PlaceableObjectDefinition tree = populationSettings.Tree;
        if (tree == null)
        {
            Debug.LogError("Grid population settings require a Tree definition.", this);
            return;
        }

        List<Vector2Int> candidates = GetTreeAnchorCandidates(tree.FootprintSideLength);
        System.Random random = new System.Random(populationSettings.Seed);
        float noiseOffsetX = (float)random.NextDouble() * 10000f;
        float noiseOffsetY = (float)random.NextDouble() * 10000f;
        ShuffleCandidates(candidates, random);

        int placedTreeCount = 0;
        foreach (Vector2Int candidate in candidates)
        {
            if (!ShouldPlaceTreeAt(candidate, tree.FootprintSideLength, random, noiseOffsetX, noiseOffsetY))
            {
                continue;
            }

            if (!TryPlaceObject(candidate, tree, out _, out _))
            {
                continue;
            }

            placedTreeCount++;
            if (placedTreeCount >= requestedTreeCount)
            {
                break;
            }
        }

        if (placedTreeCount < requestedTreeCount)
        {
            Debug.LogWarning(
                $"Placed {placedTreeCount} of {requestedTreeCount} requested trees. " +
                "The grid may not have enough eligible unoccupied cells.",
                this);
        }
    }

    private List<Vector2Int> GetTreeAnchorCandidates(int footprintSideLength)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        if (footprintSideLength < 1
            || footprintSideLength > gridWidth
            || footprintSideLength > gridHeight)
        {
            return candidates;
        }

        for (int x = 0; x <= gridWidth - footprintSideLength; x++)
        {
            for (int y = 0; y <= gridHeight - footprintSideLength; y++)
            {
                candidates.Add(new Vector2Int(x, y));
            }
        }

        return candidates;
    }

    private bool ShouldPlaceTreeAt(
        Vector2Int anchorCell,
        int footprintSideLength,
        System.Random random,
        float noiseOffsetX,
        float noiseOffsetY)
    {
        if (populationSettings.TreePlacementMode == TreePlacementMode.UniformRandom)
        {
            return true;
        }

        float sampleX = (anchorCell.x + footprintSideLength * 0.5f)
            / populationSettings.NoiseScale + noiseOffsetX;
        float sampleY = (anchorCell.y + footprintSideLength * 0.5f)
            / populationSettings.NoiseScale + noiseOffsetY;
        float clusterValue = Mathf.PerlinNoise(sampleX, sampleY);
        if (clusterValue < populationSettings.ClusterThreshold)
        {
            return false;
        }

        return random.NextDouble() < populationSettings.DensityWithinClusters;
    }

    private static void ShuffleCandidates(List<Vector2Int> candidates, System.Random random)
    {
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            Vector2Int candidate = candidates[i];
            candidates[i] = candidates[swapIndex];
            candidates[swapIndex] = candidate;
        }
    }

    private int GetCellIndex(Vector2Int cell)
    {
        return cell.y * gridWidth + cell.x;
    }

    public bool RemoveObject(GameObject placedObject)
    {
        if (placedObject == null
            || !placementIndexByInstance.TryGetValue(placedObject, out int placementIndex))
        {
            return false;
        }

        PlacementData placement = placements[placementIndex];
        foreach (Vector2Int cell in GetFootprintCells(
                     placement.AnchorCell,
                     placement.FootprintSideLength))
        {
            int cellIndex = GetCellIndex(cell);
            if (cellPlacementIndices[cellIndex] == placementIndex + 1)
            {
                cellPlacementIndices[cellIndex] = 0;
            }
        }

        placementIndexByInstance.Remove(placedObject);
        placements[placementIndex] = default;
        Destroy(placedObject);
        return true;
    }
}
