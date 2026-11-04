using UnityEngine;

public enum GridObjectPathingTag
{
    Pathable = 0,
    PathingBlocker = 1
}

[CreateAssetMenu(fileName = "PlaceableObject", menuName = "Grid/Placeable Object")]
public class PlaceableObjectDefinition : ScriptableObject
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private string objectName = "New Placeable Object";
    [SerializeField] private string description = "Description of the placeable object.";
    [SerializeField] private Sprite icon;
    [SerializeField] private int cost = 1;
    [SerializeField, Range(1, 2)] private int footprintSideLength = 1;
    [SerializeField, Min(0), Tooltip("Square build radius this object adds to player buildable territory.")]
    private int buildRadius;
    [SerializeField] private GridObjectPathingTag pathingTag = GridObjectPathingTag.PathingBlocker;

    public GameObject Prefab => prefab;
    public string ObjectName => objectName;
    public Sprite Icon => icon;
    public int Cost => cost;
    public int FootprintSideLength => footprintSideLength;
    public int FootprintCellCount => footprintSideLength * footprintSideLength;
    public int BuildRadius => buildRadius;
    public GridObjectPathingTag PathingTag => pathingTag;

    private void OnValidate()
    {
        footprintSideLength = Mathf.Clamp(footprintSideLength, 1, 2);
        buildRadius = Mathf.Max(0, buildRadius);
    }
}
