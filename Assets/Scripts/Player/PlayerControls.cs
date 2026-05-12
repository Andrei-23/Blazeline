using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    
    [SerializeField] private PlayerInput playerInput;
    private InputAction actionMove;
    private InputAction actionDash;
    private InputAction actionKick;
    private InputAction actionKickAim;
    private InputAction actionBrake;
    private InputAction actionSteering;

    private void Awake()
    {
        // Get PlayerInput component if not assigned
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
        
        // Get PlayerMovement component if not assigned
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
        
        // Find actions by name
        if (playerInput != null && playerInput.actions != null)
        {
            actionMove = playerInput.actions["Move"];
            actionDash = playerInput.actions["Dash"];
            actionKick = playerInput.actions["Kick"];
            actionKickAim = playerInput.actions["KickAim"];
            actionBrake = playerInput.actions["Brake"];
            actionSteering = playerInput.actions["Steering"];
        }
        else
        {
            Debug.LogError("PlayerInput or InputActions not found!");
        }
    }
    
    private void OnEnable()
    {
        actionMove.performed += OnMove;
        actionMove.canceled += OnMoveCanceled;
    
        actionDash.performed += OnDash;
        actionKick.performed += OnKick;
        actionKick.canceled += OnKickCanceled;
        if (actionKickAim != null)
        {
            actionKickAim.performed += OnKickAimPressed;
            actionKickAim.canceled += OnKickAimCanceled;
        }
        if (actionBrake != null)
        {
            actionBrake.performed += OnBrakePressed;
            actionBrake.canceled += OnBrakeCanceled;
        }
        if (actionSteering != null)
        {
            actionSteering.performed += OnSteeringPressed;
            actionSteering.canceled += OnSteeringCanceled;
        }
    }
    
    private void OnDisable()
    {
        actionMove.performed -= OnMove;
        actionMove.canceled -= OnMoveCanceled;

        actionDash.performed -= OnDash;
        actionKick.performed -= OnKick;
        actionKick.canceled -= OnKickCanceled;
        if (actionKickAim != null)
        {
            actionKickAim.performed -= OnKickAimPressed;
            actionKickAim.canceled -= OnKickAimCanceled;
        }
        if (actionBrake != null)
        {
            actionBrake.performed -= OnBrakePressed;
            actionBrake.canceled -= OnBrakeCanceled;
        }
        if (actionSteering != null)
        {
            actionSteering.performed -= OnSteeringPressed;
            actionSteering.canceled -= OnSteeringCanceled;
        }
    }
    
    // Input callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetMoveInput(context.ReadValue<Vector2>());
        }
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetMoveInput(Vector2.zero);
        }
    }
    
    private void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && playerMovement != null)
        {
            playerMovement.TryDash();
        }
    }
    
    private void OnKick(InputAction.CallbackContext context)
    {
        if (context.performed && playerMovement != null)
        {
            playerMovement.OnPressKick();
        }
    }

    private void OnKickCanceled(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.OnReleaseKick();
        }
    }

    private void OnKickAimPressed(InputAction.CallbackContext context)
    {
        if (context.performed && playerMovement != null)
        {
            playerMovement.SetKickAimHold(true);
        }
    }

    private void OnKickAimCanceled(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetKickAimHold(false);
        }
    }

    private void OnBrakePressed(InputAction.CallbackContext context)
    {
        if (context.performed && playerMovement != null)
        {
            playerMovement.SetBrakeHold(true);
        }
    }

    private void OnBrakeCanceled(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetBrakeHold(false);
        }
    }

    private void OnSteeringPressed(InputAction.CallbackContext context)
    {
        if (context.performed && playerMovement != null)
        {
            playerMovement.SetSteeringHold(true);
        }
    }

    private void OnSteeringCanceled(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetSteeringHold(false);
        }
    }
}

