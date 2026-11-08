using System.Collections.Generic;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField, Min(1)] private int gridWidth = 10;
    [SerializeField, Min(1)] private int gridHeight = 10;
    [SerializeField, Min(0.0001f)] private float cellSize = 1f;
    [SerializeField, Min(0), Tooltip("Extra cells to include around the requested cell when expanding.")]
    private int expansionMargin = 5;
    [Tooltip("Center point of the grid in world space.")]
    [SerializeField] private Vector3 origin;
    [SerializeField] private GridPopulationSettings populationSettings;

    [Header("Gridline Display")]
    [SerializeField] private Material gridLineMaterial;
    [SerializeField] private Color gridLineColor = Color.black;
    [SerializeField] private Color unbuildableGridLineColor = Color.red;
    [SerializeField, Min(0.001f)] private float gridLineWidth = 0.02f;
    [SerializeField] private float gridLineHeightOffset = 0.01f;
    [SerializeField] private bool gridLinesVisibleAtStart = true;

    private struct PlacementData
    {
        public PlaceableObjectDefinition Definition;
        public GameObject Instance;
        public Vector2Int AnchorCell;
        public int FootprintSideLength;
        public bool IsPlayerPlaced;
    }

    private int[] cellPlacementIndices;
    private GridEnemy[] enemyOccupants;
    private Vector2Int minimumCell;
    private Vector3 gridAnchorWorldPosition;
    private readonly HashSet<GridEnemy> activeEnemies = new HashSet<GridEnemy>();
    private readonly List<BehaviourScript> activePlaceableBehaviours =
        new List<BehaviourScript>();
    private readonly List<PlacementData> placements = new List<PlacementData>();
    private readonly Dictionary<GameObject, int> placementIndexByInstance =
        new Dictionary<GameObject, int>();
    private GameObject gridLinesRoot;
    private Material runtimeGridLineMaterial;
    private GameManager gameManager;
    private bool hasStarted;
    private bool isInitialized;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public Vector3 Origin => origin;
    public bool AreGridlinesVisible => gridLinesRoot != null && gridLinesRoot.activeSelf;
    public GameManager GameManager
    {
        get
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            return gameManager;
        }
    }

    private void RebuildGridlineDisplay()
    {
        bool wasVisible = AreGridlinesVisible;
        GameObject previousGridLinesRoot = gridLinesRoot;
        CreateGridlineDisplay();
        SetGridlinesVisible(wasVisible);

        if (previousGridLinesRoot != null)
        {
            Destroy(previousGridLinesRoot);
        }
    }

    private void OnValidate()
    {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(0.0001f, cellSize);
        expansionMargin = Mathf.Max(0, expansionMargin);
        gridLineWidth = Mathf.Max(0.001f, gridLineWidth);
    }

    private void Awake()
    {
        gridAnchorWorldPosition = origin + new Vector3(
            -gridWidth * cellSize * 0.5f,
            0f,
            -gridHeight * cellSize * 0.5f);
        cellPlacementIndices = new int[gridWidth * gridHeight];
        enemyOccupants = new GridEnemy[cellPlacementIndices.Length];
        CreateGridlineDisplay();
        SetGridlinesVisible(gridLinesVisibleAtStart);
        PopulateInitialObjects();
        isInitialized = true;
    }

    private void Start()
    {
        hasStarted = true;
        foreach (BehaviourScript behaviour in activePlaceableBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.Initialize(this);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (BehaviourScript behaviour in activePlaceableBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.StopBehaviour();
            }
        }

        if (runtimeGridLineMaterial != null)
        {
            Destroy(runtimeGridLineMaterial);
        }
    }

    private void Update()
    {
        int behaviourCount = activePlaceableBehaviours.Count;
        for (int i = 0; i < behaviourCount; i++)
        {
            BehaviourScript behaviour = activePlaceableBehaviours[i];
            if (behaviour != null)
            {
                behaviour.AdvanceTick(Time.deltaTime);
            }
        }

        for (int i = activePlaceableBehaviours.Count - 1; i >= 0; i--)
        {
            if (activePlaceableBehaviours[i] == null)
            {
                activePlaceableBehaviours.RemoveAt(i);
            }
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
        for (int xOffset = 0; xOffset <= gridWidth; xOffset++)
        {
            float lineX = gridStart.x + xOffset * cellSize;
            int segmentStartOffset = 0;
            bool segmentIsBuildable = IsVerticalGridlineBuildable(xOffset, 0);
            for (int yOffset = 1; yOffset <= gridHeight; yOffset++)
            {
                bool isLastSegment = yOffset == gridHeight;
                bool nextSegmentIsBuildable = !isLastSegment
                    && IsVerticalGridlineBuildable(xOffset, yOffset);
                if (isLastSegment || nextSegmentIsBuildable != segmentIsBuildable)
                {
                    CreateGridline(
                        $"Gridline X {xOffset} Segment {segmentStartOffset}",
                        new Vector3(
                            lineX,
                            lineY,
                            gridStart.z + segmentStartOffset * cellSize),
                        new Vector3(
                            lineX,
                            lineY,
                            gridStart.z + yOffset * cellSize),
                        lineMaterial,
                        segmentIsBuildable ? gridLineColor : unbuildableGridLineColor);
                }

                if (!isLastSegment && nextSegmentIsBuildable != segmentIsBuildable)
                {
                    segmentStartOffset = yOffset;
                    segmentIsBuildable = nextSegmentIsBuildable;
                }
            }
        }

        for (int yOffset = 0; yOffset <= gridHeight; yOffset++)
        {
            float lineZ = gridStart.z + yOffset * cellSize;
            int segmentStartOffset = 0;
            bool segmentIsBuildable = IsHorizontalGridlineBuildable(0, yOffset);
            for (int xOffset = 1; xOffset <= gridWidth; xOffset++)
            {
                bool isLastSegment = xOffset == gridWidth;
                bool nextSegmentIsBuildable = !isLastSegment
                    && IsHorizontalGridlineBuildable(xOffset, yOffset);
                if (isLastSegment || nextSegmentIsBuildable != segmentIsBuildable)
                {
                    CreateGridline(
                        $"Gridline Z {yOffset} Segment {segmentStartOffset}",
                        new Vector3(
                            gridStart.x + segmentStartOffset * cellSize,
                            lineY,
                            lineZ),
                        new Vector3(
                            gridStart.x + xOffset * cellSize,
                            lineY,
                            lineZ),
                        lineMaterial,
                        segmentIsBuildable ? gridLineColor : unbuildableGridLineColor);
                }

                if (!isLastSegment && nextSegmentIsBuildable != segmentIsBuildable)
                {
                    segmentStartOffset = xOffset;
                    segmentIsBuildable = nextSegmentIsBuildable;
                }
            }
        }
    }

    private bool IsVerticalGridlineBuildable(int xOffset, int yOffset)
    {
        Vector2Int leftCell = new Vector2Int(
            minimumCell.x + xOffset - 1,
            minimumCell.y + yOffset);
        return IsCellBuildable(leftCell) || IsCellBuildable(leftCell + Vector2Int.right);
    }

    private bool IsHorizontalGridlineBuildable(int xOffset, int yOffset)
    {
        Vector2Int lowerCell = new Vector2Int(
            minimumCell.x + xOffset,
            minimumCell.y + yOffset - 1);
        return IsCellBuildable(lowerCell) || IsCellBuildable(lowerCell + Vector2Int.up);
    }

    private Material GetGridlineMaterial()
    {
        if (gridLineMaterial != null)
        {
            return gridLineMaterial;
        }

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Universal Render Pipeline/Unlit");
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
        Material lineMaterial,
        Color color)
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
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.sharedMaterial = lineMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    public Vector2Int WorldPositionToCell(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - gridAnchorWorldPosition;
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
        return gridAnchorWorldPosition + new Vector3(
            minimumCell.x * cellSize,
            0f,
            minimumCell.y * cellSize);
    }

    public bool ExpandToInclude(Vector2Int cell)
    {
        if (cellPlacementIndices == null)
        {
            Debug.LogError("The grid must be initialized before it can be expanded.", this);
            return false;
        }

        if (IsCellInBounds(cell))
        {
            return true;
        }

        long requestedMinX = (long)cell.x - expansionMargin;
        long requestedMinY = (long)cell.y - expansionMargin;
        long requestedMaxX = (long)cell.x + expansionMargin + 1;
        long requestedMaxY = (long)cell.y + expansionMargin + 1;

        long oldMinX = minimumCell.x;
        long oldMinY = minimumCell.y;
        long oldMaxX = oldMinX + gridWidth;
        long oldMaxY = oldMinY + gridHeight;
        long newMinX = System.Math.Min(oldMinX, requestedMinX);
        long newMinY = System.Math.Min(oldMinY, requestedMinY);
        long newMaxX = System.Math.Max(oldMaxX, requestedMaxX);
        long newMaxY = System.Math.Max(oldMaxY, requestedMaxY);
        long newWidth = newMaxX - newMinX;
        long newHeight = newMaxY - newMinY;

        if (newMinX < int.MinValue
            || newMinY < int.MinValue
            || newMaxX > (long)int.MaxValue + 1
            || newMaxY > (long)int.MaxValue + 1
            || newWidth > int.MaxValue
            || newHeight > int.MaxValue
            || newWidth * newHeight > int.MaxValue)
        {
            Debug.LogError($"Cannot expand the grid to include cell {cell}.", this);
            return false;
        }

        int expandedWidth = (int)newWidth;
        int expandedHeight = (int)newHeight;
        int expandedMinX = (int)newMinX;
        int expandedMinY = (int)newMinY;
        int xOffset = minimumCell.x - expandedMinX;
        int yOffset = minimumCell.y - expandedMinY;
        int expandedCellCount = expandedWidth * expandedHeight;
        int[] expandedPlacementIndices = new int[expandedCellCount];
        GridEnemy[] expandedEnemyOccupants = new GridEnemy[expandedCellCount];

        for (int y = 0; y < gridHeight; y++)
        {
            int oldRowStart = y * gridWidth;
            int newRowStart = (y + yOffset) * expandedWidth + xOffset;
            System.Array.Copy(
                cellPlacementIndices,
                oldRowStart,
                expandedPlacementIndices,
                newRowStart,
                gridWidth);
            System.Array.Copy(
                enemyOccupants,
                oldRowStart,
                expandedEnemyOccupants,
                newRowStart,
                gridWidth);
        }

        minimumCell = new Vector2Int(expandedMinX, expandedMinY);
        gridWidth = expandedWidth;
        gridHeight = expandedHeight;
        cellPlacementIndices = expandedPlacementIndices;
        enemyOccupants = expandedEnemyOccupants;
        RebuildGridlineDisplay();
        return true;
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
        return cell.x >= minimumCell.x
            && cell.y >= minimumCell.y
            && (long)cell.x < (long)minimumCell.x + gridWidth
            && (long)cell.y < (long)minimumCell.y + gridHeight;
    }

    public bool IsCellBuildable(Vector2Int cell)
    {
        if (!IsCellInBounds(cell))
        {
            return false;
        }

        for (int i = 0; i < placements.Count; i++)
        {
            PlacementData placement = placements[i];
            if (placement.Definition == null || placement.Definition.BuildRadius <= 0)
            {
                continue;
            }

            long radius = placement.Definition.BuildRadius;
            long maxFootprintX = (long)placement.AnchorCell.x + placement.FootprintSideLength - 1;
            long maxFootprintY = (long)placement.AnchorCell.y + placement.FootprintSideLength - 1;
            long distanceX = cell.x < placement.AnchorCell.x
                ? (long)placement.AnchorCell.x - cell.x
                : cell.x > maxFootprintX ? cell.x - maxFootprintX : 0;
            long distanceY = cell.y < placement.AnchorCell.y
                ? (long)placement.AnchorCell.y - cell.y
                : cell.y > maxFootprintY ? cell.y - maxFootprintY : 0;
            if (System.Math.Max(distanceX, distanceY) <= radius)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsCellPathable(Vector2Int cell)
    {
        if (!IsCellInBounds(cell) || cellPlacementIndices == null)
        {
            return false;
        }

        int placementIndex = cellPlacementIndices[GetCellIndex(cell)] - 1;
        if (placementIndex < 0)
        {
            return true;
        }

        return placements[placementIndex].Definition.PathingTag == GridObjectPathingTag.Pathable;
    }

    public bool TryGetRandomEdgeSpawnCell(System.Random random, out Vector2Int spawnCell)
    {
        spawnCell = default;
        if (random == null || cellPlacementIndices == null)
        {
            return false;
        }

        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int xOffset = 0; xOffset < gridWidth; xOffset++)
        {
            for (int yOffset = 0; yOffset < gridHeight; yOffset++)
            {
                if (xOffset != 0
                    && yOffset != 0
                    && xOffset != gridWidth - 1
                    && yOffset != gridHeight - 1)
                {
                    continue;
                }

                int x = minimumCell.x + xOffset;
                int y = minimumCell.y + yOffset;
                Vector2Int cell = new Vector2Int(x, y);
                if (IsCellPathable(cell) && !IsEnemyCellUnavailable(cell))
                {
                    availableCells.Add(cell);
                }
            }
        }

        if (availableCells.Count == 0)
        {
            return false;
        }

        spawnCell = availableCells[random.Next(availableCells.Count)];
        return true;
    }

    public bool TrySpawnEnemy(
        GameObject enemyPrefab,
        GameManager gameManager,
        Vector2Int spawnCell,
        float movementSpeed,
        float retryInterval,
        float baseMachineDamage,
        float enemyMaxHealth,
        int currencyReward,
        out GridEnemy enemy)
    {
        enemy = null;
        if (enemyPrefab == null || !IsCellPathable(spawnCell) || IsEnemyCellUnavailable(spawnCell))
        {
            return false;
        }

        GameObject enemyObject = Instantiate(
            enemyPrefab,
            GetObjectWorldPosition(spawnCell, 1),
            enemyPrefab.transform.rotation,
            transform);
        enemy = enemyObject.GetComponent<GridEnemy>();
        if (enemy == null)
        {
            enemy = enemyObject.AddComponent<GridEnemy>();
        }

        enemyOccupants[GetCellIndex(spawnCell)] = enemy;
        activeEnemies.Add(enemy);
        enemy.Initialize(
            this,
            gameManager,
            spawnCell,
            movementSpeed,
            retryInterval,
            baseMachineDamage,
            enemyMaxHealth,
            currencyReward);
        return true;
    }

    public bool TryFindPathToBase(
        Vector2Int startCell,
        List<Vector2Int> path)
    {
        path.Clear();
        if (!IsCellInBounds(startCell))
        {
            return false;
        }

        if (IsBaseMachineCell(startCell))
        {
            return true;
        }

        HashSet<int> goalIndices = GetBaseMachineCells();
        if (goalIndices.Count == 0)
        {
            return false;
        }

        int startIndex = GetCellIndex(startCell);
        if (goalIndices.Contains(startIndex))
        {
            return true;
        }

        int cellCount = gridWidth * gridHeight;
        int[] movementCosts = new int[cellCount];
        int[] parentIndices = new int[cellCount];
        bool[] visited = new bool[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            movementCosts[i] = int.MaxValue;
            parentIndices[i] = -1;
        }

        List<int> openIndices = new List<int> { startIndex };
        movementCosts[startIndex] = 0;

        while (openIndices.Count > 0)
        {
            int bestOpenIndex = FindLowestCostCell(openIndices, movementCosts, goalIndices);
            int currentIndex = openIndices[bestOpenIndex];
            openIndices.RemoveAt(bestOpenIndex);

            if (goalIndices.Contains(currentIndex))
            {
                ReconstructPath(currentIndex, startIndex, parentIndices, path);
                return true;
            }

            if (visited[currentIndex])
            {
                continue;
            }

            visited[currentIndex] = true;
            Vector2Int currentCell = GetCellCoordinates(currentIndex);
            for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
            {
                Vector2Int neighbour = currentCell + CardinalDirections[directionIndex];
                bool isBaseDestination = IsBaseMachineCell(neighbour);
                if (!isBaseDestination && !IsCellPathable(neighbour))
                {
                    continue;
                }

                int neighbourIndex = GetCellIndex(neighbour);
                if (visited[neighbourIndex])
                {
                    continue;
                }

                int tentativeCost = movementCosts[currentIndex] + 1;
                if (tentativeCost >= movementCosts[neighbourIndex])
                {
                    continue;
                }

                movementCosts[neighbourIndex] = tentativeCost;
                parentIndices[neighbourIndex] = currentIndex;
                if (!openIndices.Contains(neighbourIndex))
                {
                    openIndices.Add(neighbourIndex);
                }
            }
        }

        return false;
    }

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    private HashSet<int> GetBaseMachineCells()
    {
        HashSet<int> baseCells = new HashSet<int>();
        if (populationSettings == null || populationSettings.BaseMachine == null)
        {
            return baseCells;
        }

        for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
        {
            PlacementData placement = placements[placementIndex];
            if (placement.Definition != populationSettings.BaseMachine)
            {
                continue;
            }

            for (int x = 0; x < placement.FootprintSideLength; x++)
            {
                for (int y = 0; y < placement.FootprintSideLength; y++)
                {
                    Vector2Int baseCell = placement.AnchorCell + new Vector2Int(x, y);
                    baseCells.Add(GetCellIndex(baseCell));
                }
            }
        }

        return baseCells;
    }

    public bool IsBaseMachineCell(Vector2Int cell)
    {
        if (!IsCellInBounds(cell) || cellPlacementIndices == null)
        {
            return false;
        }

        int placementIndex = cellPlacementIndices[GetCellIndex(cell)] - 1;
        return placementIndex >= 0
            && populationSettings != null
            && placements[placementIndex].Definition == populationSettings.BaseMachine;
    }

    private int FindLowestCostCell(
        List<int> openIndices,
        int[] movementCosts,
        HashSet<int> goalIndices)
    {
        int bestPosition = 0;
        int bestTotalCost = int.MaxValue;
        for (int i = 0; i < openIndices.Count; i++)
        {
            int index = openIndices[i];
            int totalCost = movementCosts[index] + GetDistanceToClosestGoal(index, goalIndices);
            if (totalCost >= bestTotalCost)
            {
                continue;
            }

            bestTotalCost = totalCost;
            bestPosition = i;
        }

        return bestPosition;
    }

    private int GetDistanceToClosestGoal(int cellIndex, HashSet<int> goalIndices)
    {
        Vector2Int cell = GetCellCoordinates(cellIndex);
        int nearestDistance = int.MaxValue;
        foreach (int goalIndex in goalIndices)
        {
            Vector2Int goal = GetCellCoordinates(goalIndex);
            int distance = Mathf.Abs(cell.x - goal.x) + Mathf.Abs(cell.y - goal.y);
            nearestDistance = Mathf.Min(nearestDistance, distance);
        }

        return nearestDistance;
    }

    private Vector2Int GetCellCoordinates(int cellIndex)
    {
        return minimumCell + new Vector2Int(cellIndex % gridWidth, cellIndex / gridWidth);
    }

    private void ReconstructPath(
        int goalIndex,
        int startIndex,
        int[] parentIndices,
        List<Vector2Int> path)
    {
        List<int> reversedIndices = new List<int>();
        int currentIndex = goalIndex;
        while (currentIndex != startIndex)
        {
            reversedIndices.Add(currentIndex);
            currentIndex = parentIndices[currentIndex];
            if (currentIndex < 0)
            {
                path.Clear();
                return;
            }
        }

        for (int i = reversedIndices.Count - 1; i >= 0; i--)
        {
            path.Add(GetCellCoordinates(reversedIndices[i]));
        }
    }

    public bool TryBeginEnemyMove(GridEnemy enemy, Vector2Int fromCell, Vector2Int toCell)
    {
        if (enemy == null
            || !IsCellInBounds(toCell)
            || (!IsBaseMachineCell(toCell) && !IsCellPathable(toCell))
            || !IsCellInBounds(fromCell)
            || IsEnemyCellUnavailable(toCell))
        {
            return false;
        }

        int fromIndex = GetCellIndex(fromCell);
        int toIndex = GetCellIndex(toCell);
        if (enemyOccupants[fromIndex] != enemy)
        {
            return false;
        }

        if (IsBaseMachineCell(toCell))
        {
            return true;
        }

        enemyOccupants[fromIndex] = null;
        enemyOccupants[toIndex] = enemy;
        return true;
    }

    public bool CompleteEnemyMove(GridEnemy enemy, Vector2Int fromCell, Vector2Int toCell)
    {
        if (enemy == null || !IsCellInBounds(fromCell) || !IsCellInBounds(toCell))
        {
            return false;
        }

        int fromIndex = GetCellIndex(fromCell);
        int toIndex = GetCellIndex(toCell);
        if (IsBaseMachineCell(toCell))
        {
            if (enemyOccupants[fromIndex] != enemy)
            {
                return false;
            }

            enemyOccupants[fromIndex] = null;
            return true;
        }

        return enemyOccupants[toIndex] == enemy;
    }

    public void UnregisterEnemy(GridEnemy enemy, Vector2Int currentCell)
    {
        activeEnemies.Remove(enemy);
        if (enemy == null || enemyOccupants == null)
        {
            return;
        }

        if (IsCellInBounds(currentCell))
        {
            int currentIndex = GetCellIndex(currentCell);
            if (enemyOccupants[currentIndex] == enemy)
            {
                enemyOccupants[currentIndex] = null;
            }
        }

        for (int i = 0; i < enemyOccupants.Length; i++)
        {
            if (enemyOccupants[i] == enemy)
            {
                enemyOccupants[i] = null;
            }
        }
    }

    private bool IsEnemyCellUnavailable(Vector2Int cell)
    {
        if (!IsCellInBounds(cell) || enemyOccupants == null)
        {
            return false;
        }

        int cellIndex = GetCellIndex(cell);
        return enemyOccupants[cellIndex] != null;
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

                if (IsEnemyCellUnavailable(cell))
                {
                    reason = $"Cell {cell} is occupied by an enemy.";
                    return false;
                }

                if (isInitialized && !IsCellBuildable(cell))
                {
                    reason = $"Cell {cell} is outside buildable territory.";
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
        return TryPlaceObject(anchorCell, definition, false, out placedObject, out reason);
    }

    public bool TryPlacePlayerObject(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        out GameObject placedObject,
        out string reason)
    {
        return TryPlaceObject(anchorCell, definition, true, out placedObject, out reason);
    }

    private bool TryPlaceObject(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        bool isPlayerPlaced,
        out GameObject placedObject,
        out string reason)
    {
        placedObject = null;
        reason = null;
        if (!IsValidPlacement(anchorCell, definition, out reason))
        {
            return false;
        }

        if (isPlayerPlaced
            && definition.PathingTag == GridObjectPathingTag.PathingBlocker
            && !PreservesEnemyPaths(anchorCell, definition, out reason))
        {
            return false;
        }

        if (!ExpandToIncludeBuildRadius(anchorCell, definition, out reason))
        {
            return false;
        }

        return TryCreatePlacement(anchorCell, definition, isPlayerPlaced, out placedObject);
    }

    private bool PreservesEnemyPaths(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        out string reason)
    {
        int placementIndex = placements.Count;
        placements.Add(new PlacementData
        {
            Definition = definition,
            AnchorCell = anchorCell,
            FootprintSideLength = definition.FootprintSideLength
        });

        for (int x = 0; x < definition.FootprintSideLength; x++)
        {
            for (int y = 0; y < definition.FootprintSideLength; y++)
            {
                Vector2Int cell = anchorCell + new Vector2Int(x, y);
                cellPlacementIndices[GetCellIndex(cell)] = placementIndex + 1;
            }
        }

        try
        {
            List<Vector2Int> path = new List<Vector2Int>();
            foreach (GridEnemy enemy in activeEnemies)
            {
                if (enemy != null
                    && enemy.IsAlive
                    && !TryFindPathToBase(enemy.CurrentCell, path))
                {
                    reason = "This placement would block an active enemy's path to the Base Machine.";
                    return false;
                }
            }

            for (int xOffset = 0; xOffset < gridWidth; xOffset++)
            {
                for (int yOffset = 0; yOffset < gridHeight; yOffset++)
                {
                    if (xOffset != 0
                        && yOffset != 0
                        && xOffset != gridWidth - 1
                        && yOffset != gridHeight - 1)
                    {
                        continue;
                    }

                    Vector2Int edgeCell = minimumCell + new Vector2Int(xOffset, yOffset);
                    if (!IsCellPathable(edgeCell))
                    {
                        continue;
                    }

                    if (!TryFindPathToBase(edgeCell, path))
                    {
                        reason = "This placement would block an enemy spawn path to the Base Machine.";
                        return false;
                    }
                }
            }
        }
        finally
        {
            for (int x = 0; x < definition.FootprintSideLength; x++)
            {
                for (int y = 0; y < definition.FootprintSideLength; y++)
                {
                    Vector2Int cell = anchorCell + new Vector2Int(x, y);
                    cellPlacementIndices[GetCellIndex(cell)] = 0;
                }
            }

            placements.RemoveAt(placementIndex);
        }

        reason = null;
        return true;
    }

    private bool ExpandToIncludeBuildRadius(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        out string reason)
    {
        reason = null;
        if (definition.BuildRadius <= 0)
        {
            return true;
        }

        long minimumX = (long)anchorCell.x - definition.BuildRadius;
        long minimumY = (long)anchorCell.y - definition.BuildRadius;
        long maximumX = (long)anchorCell.x + definition.FootprintSideLength - 1 + definition.BuildRadius;
        long maximumY = (long)anchorCell.y + definition.FootprintSideLength - 1 + definition.BuildRadius;
        if (minimumX < int.MinValue
            || minimumY < int.MinValue
            || maximumX > int.MaxValue
            || maximumY > int.MaxValue)
        {
            reason = "The buildable territory exceeds the supported grid coordinate range.";
            return false;
        }

        Vector2Int minimumTerritoryCell = new Vector2Int((int)minimumX, (int)minimumY);
        Vector2Int maximumTerritoryCell = new Vector2Int((int)maximumX, (int)maximumY);
        if (!ExpandToInclude(minimumTerritoryCell)
            || !ExpandToInclude(maximumTerritoryCell))
        {
            reason = "The grid could not expand to contain the building's buildable territory.";
            return false;
        }

        return true;
    }

    private bool TryCreatePlacement(
        Vector2Int anchorCell,
        PlaceableObjectDefinition definition,
        bool isPlayerPlaced,
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
            Instance = placedObject,
            AnchorCell = anchorCell,
            FootprintSideLength = definition.FootprintSideLength,
            IsPlayerPlaced = isPlayerPlaced
        });
        placementIndexByInstance.Add(placedObject, placementIndex);

        foreach (Vector2Int cell in GetFootprintCells(anchorCell, definition.FootprintSideLength))
        {
            cellPlacementIndices[GetCellIndex(cell)] = placementIndex + 1;
        }

        BehaviourScript[] behaviours =
            placedObject.GetComponentsInChildren<BehaviourScript>(true);
        foreach (BehaviourScript behaviour in behaviours)
        {
            activePlaceableBehaviours.Add(behaviour);
            if (hasStarted)
            {
                behaviour.Initialize(this);
            }
        }

        if (definition.BuildRadius > 0)
        {
            RebuildGridlineDisplay();
        }

        return true;
    }

    public bool TryGetClosestEnemy(Vector3 position, float range, out GridEnemy closestEnemy)
    {
        closestEnemy = null;
        if (range < 0f)
        {
            return false;
        }

        float closestDistanceSquared = range * range;
        foreach (GridEnemy enemy in activeEnemies)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            float distanceSquared = (enemy.transform.position - position).sqrMagnitude;
            if (distanceSquared <= closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestEnemy = enemy;
            }
        }

        return closestEnemy != null;
    }

    internal void UnregisterPlaceableBehaviour(BehaviourScript behaviour)
    {
        int index = activePlaceableBehaviours.IndexOf(behaviour);
        if (index >= 0)
        {
            activePlaceableBehaviours[index] = null;
        }
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

        for (int xOffset = 0; xOffset <= gridWidth - footprintSideLength; xOffset++)
        {
            for (int yOffset = 0; yOffset <= gridHeight - footprintSideLength; yOffset++)
            {
                candidates.Add(new Vector2Int(
                    minimumCell.x + xOffset,
                    minimumCell.y + yOffset));
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
        return (cell.y - minimumCell.y) * gridWidth + cell.x - minimumCell.x;
    }

    public bool TryGetPlayerPlacedObjectAtCell(
        Vector2Int cell,
        out GameObject placedObject,
        out PlaceableObjectDefinition definition)
    {
        placedObject = null;
        definition = null;
        if (!IsCellInBounds(cell))
        {
            return false;
        }

        int placementIndex = cellPlacementIndices[GetCellIndex(cell)] - 1;
        if (placementIndex < 0 || placementIndex >= placements.Count)
        {
            return false;
        }

        PlacementData placement = placements[placementIndex];
        if (!placement.IsPlayerPlaced || placement.Definition == null || placement.Instance == null)
        {
            return false;
        }

        placedObject = placement.Instance;
        definition = placement.Definition;
        return true;
    }

    public bool RemoveObject(GameObject placedObject)
    {
        if (placedObject == null
            || !placementIndexByInstance.TryGetValue(placedObject, out int placementIndex))
        {
            return false;
        }

        PlacementData placement = placements[placementIndex];
        bool expandsBuildableArea =
            placement.Definition != null && placement.Definition.BuildRadius > 0;
        BehaviourScript[] behaviours =
            placedObject.GetComponentsInChildren<BehaviourScript>(true);
        foreach (BehaviourScript behaviour in behaviours)
        {
            behaviour.StopBehaviour();
        }

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
        if (expandsBuildableArea)
        {
            RebuildGridlineDisplay();
        }

        return true;
    }
}
