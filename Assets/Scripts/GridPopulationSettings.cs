using UnityEngine;

public enum TreePlacementMode
{
    UniformRandom,
    PerlinClusters
}

[CreateAssetMenu(fileName = "GridPopulationSettings", menuName = "Grid/Population Settings")]
public class GridPopulationSettings : ScriptableObject
{
    [Header("Default Objects")]
    [SerializeField] private PlaceableObjectDefinition baseMachine;
    [SerializeField] private PlaceableObjectDefinition tree;

    [Header("Tree Placement")]
    [SerializeField, Min(0)] private int treeCount = 20;
    [SerializeField] private int seed = 12345;
    [SerializeField] private TreePlacementMode treePlacementMode = TreePlacementMode.PerlinClusters;
    [SerializeField, Min(0.01f)] private float noiseScale = 5f;
    [SerializeField, Range(0f, 1f)] private float clusterThreshold = 0.48f;
    [SerializeField, Range(0f, 1f)] private float densityWithinClusters = 0.75f;

    public PlaceableObjectDefinition BaseMachine => baseMachine;
    public PlaceableObjectDefinition Tree => tree;
    public int TreeCount => treeCount;
    public int Seed => seed;
    public TreePlacementMode TreePlacementMode => treePlacementMode;
    public float NoiseScale => noiseScale;
    public float ClusterThreshold => clusterThreshold;
    public float DensityWithinClusters => densityWithinClusters;

    private void OnValidate()
    {
        treeCount = Mathf.Max(0, treeCount);
        noiseScale = Mathf.Max(0.01f, noiseScale);
        clusterThreshold = Mathf.Clamp01(clusterThreshold);
        densityWithinClusters = Mathf.Clamp01(densityWithinClusters);
    }
}
