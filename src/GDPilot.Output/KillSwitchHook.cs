using System.ComponentModel;
using System.Runtime.InteropServices;
using GDPilot.Core;
using static GDPilot.Output.NativeHooks;

namespace GDPilot.Output;

/// <summary>
/// Watches the whole keyboard for the kill switch chord. It owns its thread and
/// message loop, so it still fires when the main loop has stalled, which is the
/// moment it matters most. Every event is passed on; nothing is swallowed.
/// </summary>
public sealed class KillSwitchHook : IDisposable
{
    private readonly ChordWatcher watcher;
    private readonly Action onTriggered;
    private readonly Thread thread;
    private readonly ManualResetEventSlim started = new();
    private HookProcedure? procedure;
    private Exception? startFailure;
    private uint threadId;
    private readonly TimeSpan renewEvery;

    /// <param name="renewEvery">How often the hook is put in afresh; a minute unless a test wants it sooner.</param>
    public KillSwitchHook(KeyChord chord, Action onTriggered, TimeSpan? renewEvery = null)
    {
        this.renewEvery = renewEvery ?? TimeSpan.FromMinutes(1);
        watcher = new ChordWatcher(chord);
        this.onTriggered = onTriggered;
        thread = new Thread(Run);
        thread.IsBackground = true;
        thread.Start();
        started.Wait();

        if (startFailure is not null)
        {
            throw new InvalidOperationException("Could not install the kill switch hook.", startFailure);
        }
    }

    public void Dispose()
    {
        PostThreadMessage(threadId, QuitMessage, IntPtr.Zero, IntPtr.Zero);
        thread.Join(TimeSpan.FromSeconds(2));
        started.Dispose();
    }

    private void Run()
    {
        threadId = GetCurrentThreadId();
        procedure = OnKey;
        IntPtr hook = SetWindowsHookEx(KeyboardLowLevel, procedure, GetModuleHandle(null), 0);

        if (hook == IntPtr.Zero)
        {
            startFailure = new Win32Exception(Marshal.GetLastWin32Error());
        }

        started.Set();

        if (hook != IntPtr.Zero)
        {
            SetTimer(IntPtr.Zero, UIntPtr.Zero, (uint)renewEvery.TotalMilliseconds, IntPtr.Zero);
        }

        while (hook != IntPtr.Zero && GetMessage(out Message message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.Id == TimerMessage)
            {
                hook = Renew(hook);
            }
        }

        UnhookWindowsHookEx(hook);
    }

    /// <summary>
    /// Windows takes a low-level hook away without a word if it is ever slow to
    /// answer, after a long stall of the whole machine for one. A kill switch that
    /// has quietly gone is the worst kind of fault, so the hook is put in afresh
    /// every so often, the new one before the old one comes out.
    /// </summary>
    private IntPtr Renew(IntPtr old)
    {
        IntPtr fresh = SetWindowsHookEx(KeyboardLowLevel, procedure!, GetModuleHandle(null), 0);

        if (fresh == IntPtr.Zero)
        {
            return old;
        }

        UnhookWindowsHookEx(old);
        Renewals++;
        return fresh;
    }


    /// <summary>How many times the hook has been put in again; for tests.</summary>
    public int Renewals { get; private set; }

    private IntPtr OnKey(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            KeyboardData key = Marshal.PtrToStructure<KeyboardData>(data);
            bool down = (key.Flags & KeyUpFlag) == 0;

            if (watcher.Observe(key.VirtualKey, down))
            {
                // Handed off rather than run here: Windows silently drops a
                // low-level hook that takes too long to return.
                ThreadPool.QueueUserWorkItem(_ => onTriggered());
            }
        }

        return CallNextHookEx(IntPtr.Zero, code, message, data);
    }
}
