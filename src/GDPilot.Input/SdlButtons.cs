using GDPilot.Core;
using SDL;

namespace GDPilot.Input;

/// <summary>
/// SDL names buttons by position rather than by printed label, so its south
/// button is A on an Xbox-layout pad whatever the pad itself calls it.
/// </summary>
internal static unsafe class SdlButtons
{
    internal static GamepadButtons Held(SDL_Gamepad* pad)
    {
        GamepadButtons held = GamepadButtons.None;

        foreach ((SDL_GamepadButton sdl, GamepadButtons ours) in Map)
        {
            if (SDL3.SDL_GetGamepadButton(pad, sdl))
            {
                held |= ours;
            }
        }

        return held;
    }

    private static readonly (SDL_GamepadButton Sdl, GamepadButtons Ours)[] Map =
    {
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH, GamepadButtons.A),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST, GamepadButtons.B),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST, GamepadButtons.X),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH, GamepadButtons.Y),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER, GamepadButtons.LeftBumper),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER, GamepadButtons.RightBumper),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK, GamepadButtons.Back),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START, GamepadButtons.Start),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK, GamepadButtons.LeftStick),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK, GamepadButtons.RightStick),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP, GamepadButtons.DPadUp),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN, GamepadButtons.DPadDown),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT, GamepadButtons.DPadLeft),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT, GamepadButtons.DPadRight),
        (SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE, GamepadButtons.Guide),
    };
}
