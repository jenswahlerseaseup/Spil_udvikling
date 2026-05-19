using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerInputReader : MonoBehaviour
{
    // Input is isolated from movement so menus, dialogue, cutscenes, and AI can disable or replace it cleanly.
    public event Action<Vector2> MoveChanged;
    public event Action InteractPressed;
    public event Action PausePressed;
    public event Action InventoryPressed;
    public event Action SavePressed;
    public event Action LoadPressed;
    public event Action JumpPressed;

    public bool RunHeld { get; private set; }

    public Vector2 MoveInput { get; private set; }

#if ENABLE_INPUT_SYSTEM
    // These methods are called by PlayerInput when Behavior is set to Send Messages.
    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
        MoveChanged?.Invoke(MoveInput);
    }

    public void OnInteract(InputValue value)
    {
        if (IsPressed(value))
        {
            InteractPressed?.Invoke();
        }
    }

    public void OnPause(InputValue value)
    {
        if (IsPressed(value)) PausePressed?.Invoke();
    }

    public void OnInventory(InputValue value)
    {
        if (IsPressed(value)) InventoryPressed?.Invoke();
    }

    public void OnSave(InputValue value)
    {
        if (IsPressed(value))
        {
            SavePressed?.Invoke();
        }
    }

    public void OnLoad(InputValue value)
    {
        if (IsPressed(value))
        {
            LoadPressed?.Invoke();
        }
    }

    public void OnJump(InputValue value)
    {
        if (IsPressed(value))
        {
            JumpPressed?.Invoke();
        }
    }

    public void OnRun(InputValue value)
    {
        RunHeld = value.Get<float>() > 0.5f;
    }

    private static bool IsPressed(InputValue value)
    {
        return value.Get<float>() > 0.5f;
    }
#else
    private void Awake()
    {
        Debug.LogWarning("PlayerInputReader needs the Unity Input System package and Active Input Handling set to Input System Package.");
    }
#endif
}
