// GDPilot's drop-in loader. Copied into Grim Dawn's x64 folder as dinput8.dll by
// "GDPilot.exe --install-loader", it stands in for the system's DirectInput 8,
// which the game's own DirectInput.dll asks for, and hands every call straight on
// to the real one in the system folder. Its only other work is to load GDPilot's
// in-game part once the game has finished starting, from the path GDPilot leaves
// in %LOCALAPPDATA%\GDPilot\in-game\current.txt. With no such file it does
// nothing more, and the game runs exactly as without it. It changes no file of
// the game's, and "--remove-loader" takes it away again.
#include <windows.h>
#include <shlobj.h>
#include <string>

// Lets GDPilot recognise its own copy before replacing or removing it, and never touch anyone else's dinput8.dll.
extern "C" __declspec(dllexport) const char GDPilotDropInLoader[] = "GDPilot drop-in loader 1";

namespace
{
HMODULE realModule = nullptr;
INIT_ONCE realOnce = INIT_ONCE_STATIC_INIT;

BOOL CALLBACK LoadReal(PINIT_ONCE, PVOID, PVOID*)
{
    wchar_t path[MAX_PATH];
    UINT length = GetSystemDirectoryW(path, MAX_PATH);

    if (length == 0 || length > MAX_PATH - 16)
    {
        return TRUE;
    }

    wcscat_s(path, L"\\dinput8.dll");
    realModule = LoadLibraryW(path);
    return TRUE;
}

FARPROC Real(const char* name)
{
    InitOnceExecuteOnce(&realOnce, LoadReal, nullptr, nullptr);

    if (realModule == nullptr)
    {
        return nullptr;
    }

    return GetProcAddress(realModule, name);
}

// Time for the game's own libraries to finish starting, which the in-game part looks through.
constexpr DWORD Settle = 20000;

std::wstring CurrentPart()
{
    wchar_t* local = nullptr;

    if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &local)))
    {
        return L"";
    }

    std::wstring pointer = std::wstring(local) + L"\\GDPilot\\in-game\\current.txt";
    CoTaskMemFree(local);
    HANDLE file = CreateFileW(pointer.c_str(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);

    if (file == INVALID_HANDLE_VALUE)
    {
        return L"";
    }

    char bytes[2048] = {};
    DWORD read = 0;
    ReadFile(file, bytes, sizeof(bytes) - 1, &read, nullptr);
    CloseHandle(file);

    int wide = MultiByteToWideChar(CP_UTF8, 0, bytes, static_cast<int>(read), nullptr, 0);
    std::wstring path(wide, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, bytes, static_cast<int>(read), path.data(), wide);

    while (!path.empty() && (path.back() == L'\r' || path.back() == L'\n' || path.back() == L' '))
    {
        path.pop_back();
    }

    return path;
}

// A line in %LOCALAPPDATA%\GDPilot\loader.log, so a start that goes wrong can be followed.
void Note(const char* text, unsigned long value)
{
    wchar_t* local = nullptr;

    if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &local)))
    {
        return;
    }

    std::wstring path = std::wstring(local) + L"\\GDPilot\\loader.log";
    CoTaskMemFree(local);
    HANDLE file = CreateFileW(path.c_str(), FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS, 0, nullptr);

    if (file == INVALID_HANDLE_VALUE)
    {
        return;
    }

    SYSTEMTIME now;
    GetLocalTime(&now);
    char line[256];
    int length = sprintf_s(line, "%02d:%02d:%02d %s %lu\r\n", now.wHour, now.wMinute, now.wSecond, text, value);
    DWORD written = 0;
    WriteFile(file, line, static_cast<DWORD>(length), &written, nullptr);
    CloseHandle(file);
}

BOOL CALLBACK FindOwnWindow(HWND window, LPARAM found)
{
    DWORD process = 0;
    GetWindowThreadProcessId(window, &process);

    if (process == GetCurrentProcessId() && IsWindowVisible(window) && GetWindow(window, GW_OWNER) == nullptr)
    {
        *reinterpret_cast<bool*>(found) = true;
        return FALSE;
    }

    return TRUE;
}

// The game's window first, then time for it to settle, as GDPilot itself waits before loading the part.
void WaitForTheGame()
{
    bool found = false;

    for (int tries = 0; tries < 600 && !found; tries++)
    {
        EnumWindows(FindOwnWindow, reinterpret_cast<LPARAM>(&found));

        if (!found)
        {
            Sleep(500);
        }
    }

    Note("game window seen", found);
    Sleep(Settle);
}

DWORD WINAPI LoadPartLater(LPVOID)
{
    WaitForTheGame();
    std::wstring part = CurrentPart();

    if (part.empty() || GetFileAttributesW(part.c_str()) == INVALID_FILE_ATTRIBUTES)
    {
        Note("no in-game part named in current.txt", 0);
        return 0;
    }

    HMODULE loaded = LoadLibraryW(part.c_str());
    unsigned long error = 0;

    if (loaded == nullptr)
    {
        error = GetLastError();
    }

    Note("in-game part loaded, error", error);
    return 0;
}
}

extern "C" HRESULT WINAPI DirectInput8Create(HINSTANCE instance, DWORD version, REFIID wanted, LPVOID* made, LPUNKNOWN outer)
{
    using Create = HRESULT(WINAPI*)(HINSTANCE, DWORD, REFIID, LPVOID*, LPUNKNOWN);
    Create create = reinterpret_cast<Create>(Real("DirectInput8Create"));

    if (create == nullptr)
    {
        Note("the system's DirectInput8Create was not found", 0);
        return E_FAIL;
    }

    HRESULT result = create(instance, version, wanted, made, outer);
    Note("DirectInput8Create passed on, result", static_cast<unsigned long>(result));
    return result;
}

extern "C" HRESULT WINAPI ProxyDllCanUnloadNow()
{
    using Ask = HRESULT(WINAPI*)();
    Ask ask = reinterpret_cast<Ask>(Real("DllCanUnloadNow"));

    if (ask == nullptr)
    {
        return S_FALSE;
    }

    return ask();
}

extern "C" HRESULT WINAPI ProxyDllGetClassObject(REFCLSID kind, REFIID wanted, LPVOID* made)
{
    using Get = HRESULT(WINAPI*)(REFCLSID, REFIID, LPVOID*);
    Get get = reinterpret_cast<Get>(Real("DllGetClassObject"));

    if (get == nullptr)
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    return get(kind, wanted, made);
}

extern "C" HRESULT WINAPI ProxyDllRegisterServer()
{
    using Register = HRESULT(WINAPI*)();
    Register call = reinterpret_cast<Register>(Real("DllRegisterServer"));

    if (call == nullptr)
    {
        return E_FAIL;
    }

    return call();
}

extern "C" HRESULT WINAPI ProxyDllUnregisterServer()
{
    using Unregister = HRESULT(WINAPI*)();
    Unregister call = reinterpret_cast<Unregister>(Real("DllUnregisterServer"));

    if (call == nullptr)
    {
        return E_FAIL;
    }

    return call();
}

extern "C" const void* WINAPI ProxyGetdfDIJoystick()
{
    using Get = const void*(WINAPI*)();
    Get get = reinterpret_cast<Get>(Real("GetdfDIJoystick"));

    if (get == nullptr)
    {
        return nullptr;
    }

    return get();
}

// Only the game is ever given the in-game part: a copy beside any other program stays a plain DirectInput.
bool InsideTheGame()
{
    wchar_t path[MAX_PATH];
    DWORD length = GetModuleFileNameW(nullptr, path, MAX_PATH);

    if (length == 0 || length >= MAX_PATH)
    {
        return false;
    }

    std::wstring name(path, length);
    size_t slash = name.find_last_of(L"\\/");

    if (slash != std::wstring::npos)
    {
        name = name.substr(slash + 1);
    }

    return _wcsicmp(name.c_str(), L"Grim Dawn.exe") == 0;
}

BOOL WINAPI DllMain(HINSTANCE module, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH && InsideTheGame())
    {
        DisableThreadLibraryCalls(module);

        // The game loads and frees DirectInput more than once while it starts, read on
        // 2026-11-08. Freed, this file would be gone from under the thread below, which
        // took the game down when it woke; pinned, it stays until the game exits, and
        // is only ever attached once.
        HMODULE pinned = nullptr;

        if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN | GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, reinterpret_cast<LPCWSTR>(&DllMain), &pinned))
        {
            return TRUE;
        }

        // Never load anything while the loader lock is held; a thread of its own waits instead.
        HANDLE thread = CreateThread(nullptr, 0, LoadPartLater, nullptr, 0, nullptr);

        if (thread != nullptr)
        {
            CloseHandle(thread);
        }
    }

    return TRUE;
}
