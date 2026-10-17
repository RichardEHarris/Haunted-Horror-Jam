using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableObject", menuName = "Grid/Placeable Object")]
public class PlaceableObjectDefinition : ScriptableObject
{
    [SerializeField] private GameObject prefab;
    [SerializeField, Range(1, 2)] private int footprintSideLength = 1;

    public GameObject Prefab => prefab;
    public int FootprintSideLength => footprintSideLength;
    public int FootprintCellCount => footprintSideLength * footprintSideLength;

    private void OnValidate()
    {
        footprintSideLength = Mathf.Clamp(footprintSideLength, 1, 2);
    }
}
