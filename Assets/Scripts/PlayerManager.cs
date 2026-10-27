using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private GameManager gameManager;
    private Camera placementCamera;
    private InputAction displayGridlinesAction;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        if (placementCamera == null)
            placementCamera = Camera.main;

        if (inputActions == null)
        {
            Debug.LogError("PlayerManager requires an InputActionAsset.", this);
            return;
        }

        displayGridlinesAction = inputActions.FindAction("Player/DisplayGridlines");
        if (displayGridlinesAction == null)
            Debug.LogError("The Player/Display Gridlines input action could not be found.", this);
    }

    private void OnEnable()
    {
        if (displayGridlinesAction == null)
            return;

        displayGridlinesAction.performed += OnDisplayGridlines;
        displayGridlinesAction.Enable();
    }

    private void OnDisable()
    {
        if (displayGridlinesAction == null)
            return;

        displayGridlinesAction.performed -= OnDisplayGridlines;
        displayGridlinesAction.Disable();
    }

    private void Update()
    {
        if (gameManager == null || placementCamera == null || Mouse.current == null)
            return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            if (!gameManager.PlaceSelectedObject(
                    screenPosition,
                    placementCamera,
                    out string failureReason))
            {
                Debug.LogWarning($"Tower placement failed: {failureReason}", this);
            }
        }
    }

    private void OnDisplayGridlines(InputAction.CallbackContext context)
    {
        if (gameManager == null)
        {
            Debug.LogError("PlayerManager requires a GameManager reference.", this);
            return;
        }

        gameManager.ToggleGridlines();
    }
}
