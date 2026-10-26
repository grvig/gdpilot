// GDPilot's in-game part. Loaded into Grim Dawn by GDPilot.exe, it finds what it
// needs by the names the game's own DLLs export, checks each function still
// starts the way it was mapped, and stays off if anything differs. It never
// changes how the game plays out: only the camera, and only while asked.

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include <share.h>

#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <string>

namespace
{
FILE* journal = nullptr;

void Say(const char* format, ...)
{
    if (journal == nullptr)
    {
        return;
    }

    SYSTEMTIME now{};
    GetLocalTime(&now);
    std::fprintf(journal, "%02d:%02d:%02d ", now.wHour, now.wMinute, now.wSecond);
    va_list arguments;
    va_start(arguments, format);
    std::vfprintf(journal, format, arguments);
    va_end(arguments);
    std::fputc('\n', journal);
    std::fflush(journal);
}

void OpenJournal()
{
    wchar_t folder[MAX_PATH]{};

    if (GetEnvironmentVariableW(L"LOCALAPPDATA", folder, MAX_PATH) == 0)
    {
        return;
    }

    std::wstring path = std::wstring(folder) + L"\\GDPilot";
    CreateDirectoryW(path.c_str(), nullptr);
    path += L"\\in-game.log";
    // Shared, so the log can be read while the game is still running.
    journal = _wfsopen(path.c_str(), L"a", _SH_DENYNO);
}

// A game function found by name, with the bytes it must start with. The bytes
// carry the offsets of the camera fields the function touches, so matching
// them is what makes it safe to touch the same fields directly.
struct Required
{
    const char* name;
    const char* prefix;
    void* address;
};

Required required[] = {
    {"?UpdateFromInputImpl@GameCamera@GAME@@MEAAXXZ", "40534883ec40488bd9", nullptr},
    {"?SetZoom@GameCamera@GAME@@QEAAXM@Z", "40534883ec30f30f109194050000488bd9", nullptr},
    {"?AdjustYaw@GameCamera@GAME@@QEAAXM@Z", "f30f58899c050000f30f11899c050000c3", nullptr},
};

unsigned int Nibble(char digit)
{
    if (digit >= 'a')
    {
        return static_cast<unsigned int>(digit - 'a' + 10);
    }

    return static_cast<unsigned int>(digit - '0');
}

bool StartsWith(const void* code, const char* hex)
{
    const unsigned char* bytes = static_cast<const unsigned char*>(code);
    std::size_t count = std::strlen(hex) / 2;

    for (std::size_t index = 0; index < count; index++)
    {
        unsigned int expected = Nibble(hex[index * 2]) * 16 + Nibble(hex[index * 2 + 1]);

        if (bytes[index] != expected)
        {
            return false;
        }
    }

    return true;
}

bool FindEverything()
{
    HMODULE game = GetModuleHandleW(L"Game.dll");

    if (game == nullptr)
    {
        Say("Off: Game.dll is not loaded in this process.");
        return false;
    }

    for (Required& entry : required)
    {
        entry.address = reinterpret_cast<void*>(GetProcAddress(game, entry.name));

        if (entry.address == nullptr)
        {
            Say("Off: the game no longer exports %s.", entry.name);
            return false;
        }

        if (!StartsWith(entry.address, entry.prefix))
        {
            Say("Off: %s has changed since it was mapped.", entry.name);
            return false;
        }
    }

    Say("Found all %zu camera functions by name; their code matches.", std::size(required));
    return true;
}

DWORD WINAPI Start(LPVOID)
{
    OpenJournal();
    Say("GDPilot in-game part loaded.");
    FindEverything();
    return 0;
}
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    // Nothing beyond starting a thread happens under the loader lock.
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(module);
        HANDLE thread = CreateThread(nullptr, 0, Start, nullptr, 0, nullptr);

        if (thread != nullptr)
        {
            CloseHandle(thread);
        }
    }

    return TRUE;
}
