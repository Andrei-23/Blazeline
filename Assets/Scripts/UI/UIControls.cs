using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class UIControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;

    [SerializeField] private PlayerInput playerInput;

    private InputActionMap uiMap;
    private InputAction actionBack;
    private InputAction actionPause;
    private InputAction actionMap;
    private InputAction actionZoomIn;
    private InputAction actionZoomOut;
    private InputAction actionZoom;
    private InputAction actionPin;

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (uiManager == null)
        {
            uiManager = GetComponent<UIManager>();
        }

        if (playerInput != null && playerInput.actions != null)
        {
            uiMap = playerInput.actions.FindActionMap("UI");
            if (uiMap != null)
            {
                actionBack = uiMap.FindAction("Back");
                actionPause = uiMap.FindAction("Pause");
                actionMap = uiMap.FindAction("Map");
                actionZoomIn = uiMap.FindAction("ZoomIn");
                actionZoomOut = uiMap.FindAction("ZoomOut");
                actionZoom = uiMap.FindAction("Zoom");
                actionPin = uiMap.FindAction("Pin");
            }
        }

        if (uiMap == null || actionBack == null || actionPause == null || actionMap == null ||
            actionZoomIn == null || actionZoomOut == null || actionZoom == null || actionPin == null)
        {
            Debug.LogError("UIControls: UI action map or required actions (Back, Pause, Map, ZoomIn, ZoomOut, Zoom, Pin) not found.");
        }
    }

    private void OnEnable()
    {
        if (actionBack != null)
        {
            actionBack.performed += OnBack;
        }

        if (actionPause != null)
        {
            actionPause.performed += OnPause;
        }

        if (actionMap != null)
        {
            actionMap.performed += OnMap;
        }

        if (actionZoomIn != null)
        {
            actionZoomIn.performed += OnZoomIn;
        }

        if (actionZoomOut != null)
        {
            actionZoomOut.performed += OnZoomOut;
        }

        if (actionZoom != null)
        {
            actionZoom.performed += OnZoom;
        }

        if (actionPin != null)
        {
            actionPin.performed += OnPin;
        }
    }

    private void OnDisable()
    {
        if (actionBack != null)
        {
            actionBack.performed -= OnBack;
        }

        if (actionPause != null)
        {
            actionPause.performed -= OnPause;
        }

        if (actionMap != null)
        {
            actionMap.performed -= OnMap;
        }

        if (actionZoomIn != null)
        {
            actionZoomIn.performed -= OnZoomIn;
        }

        if (actionZoomOut != null)
        {
            actionZoomOut.performed -= OnZoomOut;
        }

        if (actionZoom != null)
        {
            actionZoom.performed -= OnZoom;
        }

        if (actionPin != null)
        {
            actionPin.performed -= OnPin;
        }
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        uiManager.OnUIBack();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        uiManager.OnUIPause();
    }

    private void OnMap(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        uiManager.OnUIMap();
    }

    private void OnZoomIn(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        uiManager.OnUIZoomIn();
    }

    private void OnZoomOut(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        uiManager.OnUIZoomOut();
    }

    private void OnZoom(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        float delta = context.ReadValue<float>();
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        uiManager.OnUIZoom(delta);
    }

    private void OnPin(InputAction.CallbackContext context)
    {
        if (!context.performed || uiManager == null)
        {
            return;
        }

        uiManager.OnUIPin();
    }
}
