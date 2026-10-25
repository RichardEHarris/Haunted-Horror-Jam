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
    [SerializeField] private int cost = 1;
    [SerializeField, Range(1, 2)] private int footprintSideLength = 1;
    [SerializeField] private GridObjectPathingTag pathingTag = GridObjectPathingTag.PathingBlocker;

    public GameObject Prefab => prefab;
    public int FootprintSideLength => footprintSideLength;
    public int FootprintCellCount => footprintSideLength * footprintSideLength;
    public GridObjectPathingTag PathingTag => pathingTag;

    private void OnValidate()
    {
        footprintSideLength = Mathf.Clamp(footprintSideLength, 1, 2);
    }
}
