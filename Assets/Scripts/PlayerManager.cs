using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Camera placementCamera;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        if (placementCamera == null)
            placementCamera = Camera.main;
    }

    private void Update()
    {
        if (gameManager == null || placementCamera == null || Mouse.current == null)
            return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            if (!gameManager.TryPlaceFirstObjectAtScreenPosition(
                    screenPosition,
                    placementCamera,
                    out string failureReason))
            {
                Debug.LogWarning($"Tower placement failed: {failureReason}", this);
            }
        }
    }
}
