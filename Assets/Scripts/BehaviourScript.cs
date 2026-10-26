using UnityEngine;

public abstract class BehaviourScript : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float tickInterval = 0.25f;

    private GridMap gridMap;
    private float elapsedTickTime;
    private bool initialized;

    protected GridMap GridMap => gridMap;
    protected GameManager GameManager => gridMap != null ? gridMap.GameManager : null;

    protected virtual void OnValidate()
    {
        tickInterval = Mathf.Max(0.01f, tickInterval);
    }

    internal void Initialize(GridMap owningGrid)
    {
        if (initialized)
        {
            return;
        }

        if (owningGrid == null)
        {
            Debug.LogError("A placeable object behaviour requires a GridMap.", this);
            return;
        }

        gridMap = owningGrid;
        initialized = true;
        elapsedTickTime = 0f;
        OnPlaced();
    }

    internal void AdvanceTick(float deltaTime)
    {
        if (!initialized || !isActiveAndEnabled)
        {
            return;
        }

        elapsedTickTime += deltaTime;
        if (elapsedTickTime < tickInterval)
        {
            return;
        }

        elapsedTickTime %= tickInterval;
        OnTick();
    }

    internal void StopBehaviour()
    {
        if (!initialized)
        {
            return;
        }

        initialized = false;
        OnRemoved();
        if (gridMap != null)
        {
            gridMap.UnregisterPlaceableBehaviour(this);
            gridMap = null;
        }
    }

    protected virtual void OnPlaced()
    {
    }

    protected abstract void OnTick();

    protected virtual void OnRemoved()
    {
    }

    private void OnDestroy()
    {
        StopBehaviour();
    }
}
