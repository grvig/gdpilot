using GDPilot.Core;
using SDL;
using static SDL.SDL3;

namespace GDPilot.Input;

/// <summary>
/// Reads the first connected pad through SDL, which maps XInput and DirectInput
/// pads onto one layout. A raw XInput reader would see nothing from a pad
/// switched to D mode.
/// </summary>
public sealed unsafe class SdlGamepadSource : IGamepadSource, IDisposable
{
    private SDL_Gamepad* pad;

    public SdlGamepadSource()
    {
        // This process never owns the focused window; without this hint SDL
        // would stop reporting input the moment the game took focus.
        SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");

        if (!SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD))
        {
            throw new InvalidOperationException($"SDL could not start its gamepad support: {SDL_GetError()}");
        }
    }

    public bool IsConnected => pad != null && SDL_GamepadConnected(pad);

    public GamepadSnapshot Read()
    {
        SDL_UpdateGamepads();

        if (!IsConnected && !Reconnect())
        {
            return GamepadSnapshot.Rest;
        }

        return new GamepadSnapshot(
            SdlButtons.Held(pad),
            RawAxis.Stick(Axis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX), Axis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY)),
            RawAxis.Stick(Axis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX), Axis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY)),
            RawAxis.Trigger(Axis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER)),
            RawAxis.Trigger(Axis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER)));
    }

    public void Dispose()
    {
        Close();
        SDL_Quit();
    }

    /// <summary>Picks up a pad plugged in, or switched between X and D mode, after start.</summary>
    private bool Reconnect()
    {
        Close();
        int count;
        SDL_JoystickID* ids = SDL_GetGamepads(&count);

        if (count > 0)
        {
            pad = SDL_OpenGamepad(ids[0]);
        }

        SDL_free(ids);
        return IsConnected;
    }

    private void Close()
    {
        if (pad != null)
        {
            SDL_CloseGamepad(pad);
            pad = null;
        }
    }

    private short Axis(SDL_GamepadAxis axis)
    {
        return SDL_GetGamepadAxis(pad, axis);
    }
}
