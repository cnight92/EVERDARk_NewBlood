using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Botones de ratón comunes para unificar consulta.
/// </summary>
public enum MouseBtn
{
    Left,
    Right,
    Middle,
    Forward,  // botón adicional (si existe)
    Back      // botón adicional (si existe)
}

/// <summary>
/// Permite mapear una acción a un trío (teclado, gamepad, ratón).
/// Cualquiera de los tres puede ser null.
/// </summary>
[System.Serializable]
public struct InputBindingTriple
{
    public Key? key;
    public GamepadButton? gamepadButton;
    public MouseBtn? mouseButton;

    public InputBindingTriple(Key? k, GamepadButton? g, MouseBtn? m)
    {
        key = k;
        gamepadButton = g;
        mouseButton = m;
    }
}

/// <summary>
/// Clase maestra para consultar inputs de Teclado, Gamepad y Ratón con una API unificada.
/// Ejemplo:
///   myInput.WasPressedThisFrame(Key.Space, GamepadButton.South, MouseBtn.Left);
/// </summary>
public sealed class UnifiedInput : MonoBehaviour
{
    public static UnifiedInput Instance { get; private set; }
    [Header("Shoot Input")]
    public Key shootKey = Key.J;
    public GamepadButton shootButton = GamepadButton.A;
    public MouseBtn shootMouseBtn = MouseBtn.Left;
    [Header("Meele Input")]
    public Key meeleKey = Key.K;
    public GamepadButton meeleButton = GamepadButton.B;
    public MouseBtn meeleMouseBtn = MouseBtn.Right;
    [Header("Reload Input")]
    public Key reloadKey = Key.R;
    public GamepadButton reloadButton = GamepadButton.X;
    [Header("Interact Input")]
    public Key interactKey = Key.E;
    public GamepadButton interactButton = GamepadButton.Y;

    public InputBindingTriple shootInput = new InputBindingTriple(Key.J, GamepadButton.A, MouseBtn.Left);
    public InputBindingTriple meeleInput = new InputBindingTriple(Key.K, GamepadButton.B, MouseBtn.Right);
    public InputBindingTriple reloadInput = new InputBindingTriple(Key.R, GamepadButton.X, null);
    public InputBindingTriple interactInput = new InputBindingTriple(Key.E, GamepadButton.Y, null);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        shootInput = new InputBindingTriple(shootKey, shootButton, shootMouseBtn);
        meeleInput = new InputBindingTriple(meeleKey, meeleButton, meeleMouseBtn);
        reloadInput = new InputBindingTriple(reloadKey, reloadButton, null);
        interactInput = new InputBindingTriple(interactKey, interactButton, null);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =============== API Principal (3 parámetros sueltos) ===============

    public bool WasPressedThisFrame(Key? keyboardKey, GamepadButton? gamepadButton, MouseBtn? mouseButton)
        => CheckPressedThisFrame(keyboardKey, gamepadButton, mouseButton);

    public bool IsPressed(Key? keyboardKey, GamepadButton? gamepadButton, MouseBtn? mouseButton)
        => CheckIsPressed(keyboardKey, gamepadButton, mouseButton);

    public bool WasReleasedThisFrame(Key? keyboardKey, GamepadButton? gamepadButton, MouseBtn? mouseButton)
        => CheckReleasedThisFrame(keyboardKey, gamepadButton, mouseButton);

    public Vector2 GetLeftAxis()
    {
        if (Gamepad.current != null)
        {
            return Gamepad.current.leftStick.ReadValue();
        }
        else
        {
            int x = 0;
            int y = 0;
            if (Keyboard.current != null)
            {
                if(CheckIsPressed(Key.A,null,null) || CheckIsPressed(Key.LeftArrow, null, null))
                {
                    x = -1;
                }
                if (CheckIsPressed(Key.W, null, null) || CheckIsPressed(Key.UpArrow, null, null))
                {
                    y = 1;
                }
                if (CheckIsPressed(Key.S, null, null) || CheckIsPressed(Key.DownArrow, null, null))
                {
                    y = -1;
                }
                if (CheckIsPressed(Key.D, null, null) || CheckIsPressed(Key.RightArrow, null, null))
                {
                    x = 1;
                }
                if(CheckReleasedThisFrame(Key.A, null, null) || CheckReleasedThisFrame(Key.LeftArrow, null, null) || CheckReleasedThisFrame(Key.RightArrow, null, null) || CheckReleasedThisFrame(Key.D, null, null))
                {
                    x = 0;
                }
                if (CheckReleasedThisFrame(Key.W, null, null) || CheckReleasedThisFrame(Key.UpArrow, null, null) || CheckReleasedThisFrame(Key.DownArrow, null, null) || CheckReleasedThisFrame(Key.S, null, null))
                {
                    y = 0;
                }
            }
            return new Vector2(x, y);
        }
    }

    // =============== Overloads con Binding reutilizable ===============

    public bool WasPressedThisFrame(InputBindingTriple binding)
        => CheckPressedThisFrame(binding.key, binding.gamepadButton, binding.mouseButton);

    public bool IsPressed(InputBindingTriple binding)
        => CheckIsPressed(binding.key, binding.gamepadButton, binding.mouseButton);

    public bool WasReleasedThisFrame(InputBindingTriple binding)
        => CheckReleasedThisFrame(binding.key, binding.gamepadButton, binding.mouseButton);

    // =============== Helpers internos ===============

    private bool CheckPressedThisFrame(Key? k, GamepadButton? g, MouseBtn? m)
    {
        if (k.HasValue && Keyboard.current != null)
        {
            KeyControl kc = Keyboard.current[k.Value];
            if (kc != null && kc.wasPressedThisFrame) return true;
        }

        if (g.HasValue && Gamepad.current != null)
        {
            var gc = GetGamepadButtonControl(Gamepad.current, g.Value);
            if (gc != null && gc.wasPressedThisFrame) return true;
        }

        if (m.HasValue && Mouse.current != null)
        {
            var mb = GetMouseButtonControl(Mouse.current, m.Value);
            if (mb != null && mb.wasPressedThisFrame) return true;
        }

        return false;
    }

    private bool CheckIsPressed(Key? k, GamepadButton? g, MouseBtn? m)
    {
        if (k.HasValue && Keyboard.current != null)
        {
            var kc = Keyboard.current[k.Value];
            if (kc != null && kc.isPressed) return true;
        }

        if (g.HasValue && Gamepad.current != null)
        {
            var gc = GetGamepadButtonControl(Gamepad.current, g.Value);
            if (gc != null && gc.isPressed) return true;
        }

        if (m.HasValue && Mouse.current != null)
        {
            var mb = GetMouseButtonControl(Mouse.current, m.Value);
            if (mb != null && mb.isPressed) return true;
        }

        return false;
    }

    private bool CheckReleasedThisFrame(Key? k, GamepadButton? g, MouseBtn? m)
    {
        if (k.HasValue && Keyboard.current != null)
        {
            var kc = Keyboard.current[k.Value];
            if (kc != null && kc.wasReleasedThisFrame) return true;
        }

        if (g.HasValue && Gamepad.current != null)
        {
            var gc = GetGamepadButtonControl(Gamepad.current, g.Value);
            if (gc != null && gc.wasReleasedThisFrame) return true;
        }

        if (m.HasValue && Mouse.current != null)
        {
            var mb = GetMouseButtonControl(Mouse.current, m.Value);
            if (mb != null && mb.wasReleasedThisFrame) return true;
        }

        return false;
    }

    private static ButtonControl GetGamepadButtonControl(Gamepad pad, GamepadButton b)
    {
        if (pad == null) return null;
        switch (b)
        {
            case GamepadButton.South: return pad.buttonSouth;    // A/Cruz
            case GamepadButton.East: return pad.buttonEast;     // B/Círculo
            case GamepadButton.West: return pad.buttonWest;     // X/Cuadrado
            case GamepadButton.North: return pad.buttonNorth;    // Y/Triángulo
            case GamepadButton.Start: return pad.startButton;
            case GamepadButton.Select: return pad.selectButton;
            case GamepadButton.LeftShoulder: return pad.leftShoulder;
            case GamepadButton.RightShoulder: return pad.rightShoulder;
            case GamepadButton.LeftStick: return pad.leftStickButton;
            case GamepadButton.RightStick: return pad.rightStickButton;
            case GamepadButton.DpadUp: return pad.dpad.up;
            case GamepadButton.DpadDown: return pad.dpad.down;
            case GamepadButton.DpadLeft: return pad.dpad.left;
            case GamepadButton.DpadRight: return pad.dpad.right;
            default: return null;
        }
    }

    private static ButtonControl GetMouseButtonControl(Mouse mouse, MouseBtn b)
    {
        if (mouse == null) return null;
        switch (b)
        {
            case MouseBtn.Left: return mouse.leftButton;
            case MouseBtn.Right: return mouse.rightButton;
            case MouseBtn.Middle: return mouse.middleButton;
            case MouseBtn.Forward: return mouse.forwardButton; // puede ser null si no existe
            case MouseBtn.Back: return mouse.backButton;    // puede ser null si no existe
            default: return null;
        }
    }

    // =============== Extras útiles (sticks y ejes) ===============

    public Vector2 ReadLeftStick() => Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
    public Vector2 ReadRightStick() => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
    public Vector2 ReadDpad() => Gamepad.current != null ? Gamepad.current.dpad.ReadValue() : Vector2.zero;

    public float ReadMouseScroll() => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
    public Vector2 ReadMouseDelta() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
}
