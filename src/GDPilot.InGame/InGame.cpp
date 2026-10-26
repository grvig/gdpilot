// GDPilot's in-game part. Loaded into Grim Dawn by GDPilot.exe, it finds what it
// needs by the names the game's own DLLs export, checks each function still
// starts the way it was mapped, and stays off if anything differs. It never
// changes how the game plays out: only the camera, and only while asked.
//
// The camera's field map and the order the fields are written in, around the
// game's own SetZoom, come from GrimAction by Ophemist (MIT licence,
// github.com/Ophemist/GrimAction), which mapped them first.

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include <detours.h>
#include <share.h>

#include <cmath>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <string>

namespace
{
FILE* journal = nullptr;

// camera.ini, beside the log.
std::wstring settingsPath;

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
    settingsPath = path + L"\\camera.ini";
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

// Camera fields, as offsets into the game's camera object. SetZoom's checked
// code reads the second zoom endpoint at 0x594 and AdjustYaw's the yaw at
// 0x59c, the same as in the build GrimAction mapped these from.
constexpr std::size_t DistanceDefault = 0x10c;
constexpr std::size_t PitchDefault = 0x110;
constexpr std::size_t DistanceMinimum = 0x570;
constexpr std::size_t DistanceMaximum = 0x574;
constexpr std::size_t PitchMinimum = 0x578;
constexpr std::size_t PitchMaximum = 0x57c;
// The game eases the live zoom toward the target zoom, both as blends between
// the near and far distances, so both must be set or it eases straight back.
constexpr std::size_t ZoomLive = 0x580;
constexpr std::size_t ZoomTarget = 0x584;
constexpr std::size_t ZoomNear = 0x590;
constexpr std::size_t ZoomFar = 0x594;
constexpr std::size_t FieldOfView = 0x14;

constexpr std::size_t Fields[] = {DistanceDefault, PitchDefault, DistanceMinimum, DistanceMaximum, PitchMinimum, PitchMaximum, ZoomLive, ZoomTarget, ZoomNear, ZoomFar, FieldOfView};

float Read(const void* camera, std::size_t offset)
{
    float value = 0;
    std::memcpy(&value, static_cast<const unsigned char*>(camera) + offset, sizeof(value));
    return value;
}

void Write(void* camera, std::size_t offset, float value)
{
    std::memcpy(static_cast<unsigned char*>(camera) + offset, &value, sizeof(value));
}

// The game's own values, captured as third person turns on and put back as it turns off.
float native[std::size(Fields)]{};
bool thirdPerson = false;
bool togglePressed = false;

float Setting(const wchar_t* key, float fallback)
{
    wchar_t text[32]{};
    GetPrivateProfileStringW(L"camera", key, L"", text, 32, settingsPath.c_str());

    if (text[0] == 0)
    {
        return fallback;
    }

    return std::wcstof(text, nullptr);
}

// The third-person camera, read from camera.ini beside the log each time it
// turns on, so it can be tuned in a running game. Distances are in world units
// and angles in degrees; the game keeps its field of view in radians. The game
// tilts the camera between the two pitches as the zoom moves between the two
// distances, so a close camera also looks along the ground.
void Behind(float values[])
{
    float nearest = Setting(L"distance_min", 4);
    float farthest = Setting(L"distance_max", 30);
    float start = Setting(L"distance_start", 10);
    float blend = (start - nearest) / (farthest - nearest);
    values[0] = start;
    values[1] = Setting(L"pitch_min", 12);
    values[2] = nearest;
    values[3] = farthest;
    values[4] = Setting(L"pitch_min", 12);
    values[5] = Setting(L"pitch_max", 40);
    values[6] = blend;
    values[7] = blend;
    values[8] = nearest;
    values[9] = farthest;
    values[10] = Setting(L"field_of_view", 50) * 3.14159265f / 180;
}

using SetZoom = void (*)(void* camera, float distance);

bool Plausible(const float values[])
{
    for (std::size_t index = 0; index < std::size(Fields); index++)
    {
        if (!std::isfinite(values[index]) || values[index] < 0 || values[index] > 1000)
        {
            return false;
        }
    }

    return true;
}

// Sets every field, lets the game's own SetZoom recalculate what depends on
// them, then sets them again, because SetZoom also rewrites the zoom fields.
void Apply(void* camera, const float values[])
{
    for (std::size_t index = 0; index < std::size(Fields); index++)
    {
        Write(camera, Fields[index], values[index]);
    }

    float distance = values[8] + (values[9] - values[8]) * values[7];
    reinterpret_cast<SetZoom>(required[1].address)(camera, distance);

    for (std::size_t index = 0; index < std::size(Fields); index++)
    {
        Write(camera, Fields[index], values[index]);
    }
}

void TurnOn(void* camera)
{
    float captured[std::size(Fields)]{};

    for (std::size_t index = 0; index < std::size(Fields); index++)
    {
        captured[index] = Read(camera, Fields[index]);
    }

    // Values outside any sane camera mean the layout is not what was mapped; touch nothing.
    if (!Plausible(captured))
    {
        Say("Third person refused: the camera's values do not look like a camera.");
        return;
    }

    std::memcpy(native, captured, sizeof(native));
    Say("Native camera: distance %.1f to %.1f from %.1f, pitch %.1f to %.1f from %.1f, zoom %.3f heading to %.3f between %.1f and %.1f, view %.3f.",
        native[2], native[3], native[0], native[4], native[5], native[1], native[6], native[7], native[8], native[9], native[10]);
    float behind[std::size(Fields)]{};
    Behind(behind);

    if (!Plausible(behind) || behind[2] >= behind[3] || behind[0] < behind[2] || behind[0] > behind[3])
    {
        Say("Third person refused: camera.ini needs distance_min < distance_start < distance_max.");
        return;
    }

    Apply(camera, behind);
    Say("Third person: distance %.1f to %.1f from %.1f, pitch %.1f to %.1f, view %.1f degrees.", behind[2], behind[3], behind[0], behind[4], behind[5], behind[10] * 180 / 3.14159265f);
    thirdPerson = true;
    Say("Third person on.");
}

void TurnOff(void* camera)
{
    Apply(camera, native);
    thirdPerson = false;
    Say("Third person off: the game's own camera is back exactly as it was.");
}

bool GameInFront()
{
    DWORD owner = 0;
    GetWindowThreadProcessId(GetForegroundWindow(), &owner);
    return owner == GetCurrentProcessId();
}

using CameraUpdate = void (*)(void* camera);
CameraUpdate originalUpdate = nullptr;

// Runs on the game's own thread, once per camera update, after the game's code.
void UpdatedCamera(void* camera)
{
    originalUpdate(camera);

    bool pressed = GameInFront() && (GetAsyncKeyState(VK_F8) & 0x8000) != 0;

    if (pressed && !togglePressed)
    {
        if (thirdPerson)
        {
            TurnOff(camera);
        }
        else
        {
            TurnOn(camera);
        }
    }

    togglePressed = pressed;
}

bool Attach()
{
    originalUpdate = reinterpret_cast<CameraUpdate>(required[0].address);
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach(reinterpret_cast<PVOID*>(&originalUpdate), reinterpret_cast<PVOID>(UpdatedCamera));
    LONG result = DetourTransactionCommit();

    if (result != NO_ERROR)
    {
        Say("Off: attaching to the camera update failed with error %ld.", result);
        return false;
    }

    Say("Attached to the camera update. F8 switches third person on and off.");
    return true;
}

DWORD WINAPI Start(LPVOID)
{
    OpenJournal();
    Say("GDPilot in-game part loaded.");

    if (FindEverything())
    {
        Attach();
    }

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
