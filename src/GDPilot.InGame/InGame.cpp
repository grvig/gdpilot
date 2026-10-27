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

#include <atomic>
#include <mutex>
#include <vector>
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

// What the pipe thread hands the game's thread, and back.
std::atomic<bool> toggleRequested{false};
std::atomic<float> yawPerSecond{0};
std::atomic<float> zoomPerSecond{0};
std::atomic<bool> linked{false};
HANDLE reportWanted = nullptr;

using AdjustYaw = void (*)(void* camera, float radians);

// Lines waiting for the pipe thread to send. The game's thread only appends and
// signals, so it never waits on the pipe.
std::mutex outboxGuard;
std::vector<std::string> outbox;

void Post(const std::string& line)
{
    {
        std::lock_guard<std::mutex> hold(outboxGuard);
        outbox.push_back(line);
    }

    if (reportWanted != nullptr)
    {
        SetEvent(reportWanted);
    }
}

std::string CameraLine();

void ReportCamera()
{
    Post(CameraLine());
}

// Which panels are open, as flag bytes in the game's UI object. The offsets are
// GrimAction's, mapped on this same game build: the UI object hangs off the game
// engine at 0x19b0, and each panel has one byte that reads exactly 1 while open.
// The inventory byte is also raised by the stash and vendor windows, and the
// skills window holds devotion too, so those are reported as what they show.
void** engineSlot = nullptr;
std::string lastPanels = "[]";
float sincePanelCheck = 0;

// Set by the pipe thread on connecting; the game's thread, which owns lastPanels, answers it.
std::atomic<bool> panelsWanted{false};

struct PanelFlag
{
    std::size_t offset;
    const char* mode;
};

constexpr PanelFlag Panels[] = {
    {0x95d2, "Inventory"},
    {0x991a, "Dialog"},
    {0x9f9a, "Skills"},
    {0xa2da, "WorldMap"},
};

constexpr std::size_t UiOffset = 0x19b0;
constexpr std::size_t MenuFlag = 0x17a9;
constexpr std::size_t MenuConfirmFlag = 0x9c5a;

// Guarded, because a UI object that is being rebuilt during a load can briefly
// point nowhere; a fault then means "unknown", never a crash.
bool ReadPanelBytes(unsigned char bytes[], std::size_t count)
{
    __try
    {
        const unsigned char* engine = static_cast<const unsigned char*>(*engineSlot);

        if (engine == nullptr)
        {
            return false;
        }

        const unsigned char* ui = *reinterpret_cast<const unsigned char* const*>(engine + UiOffset);

        if (ui == nullptr)
        {
            return false;
        }

        for (std::size_t index = 0; index < std::size(Panels); index++)
        {
            bytes[index] = ui[Panels[index].offset];
        }

        bytes[count - 2] = ui[MenuFlag];
        bytes[count - 1] = ui[MenuConfirmFlag];
        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

std::string PanelsLine(const std::string& open)
{
    return "{\"protocol\":1,\"type\":\"panels\",\"open\":" + open + "}\n";
}

// About ten times a second is plenty to follow a panel opening and costs nothing.
void CheckPanels(float elapsed)
{
    sincePanelCheck += elapsed;

    if (engineSlot == nullptr || sincePanelCheck < 0.1f)
    {
        return;
    }

    sincePanelCheck = 0;
    unsigned char bytes[std::size(Panels) + 2]{};

    if (!ReadPanelBytes(bytes, std::size(bytes)))
    {
        return;
    }

    std::string open = "[";

    for (std::size_t index = 0; index < std::size(Panels); index++)
    {
        if (bytes[index] == 1)
        {
            if (open.size() > 1)
            {
                open += ",";
            }

            open += std::string("\"") + Panels[index].mode + "\"";
        }
    }

    // Escape's menu raises two bytes together; either alone is something else.
    if (bytes[std::size(Panels)] == 1 && bytes[std::size(Panels) + 1] == 1)
    {
        if (open.size() > 1)
        {
            open += ",";
        }

        open += "\"Menu\"";
    }

    open += "]";

    if (panelsWanted.exchange(false))
    {
        Post(PanelsLine(open));
    }

    if (open != lastPanels)
    {
        lastPanels = open;
        Say("Panels open: %s", open.c_str());
        Post(PanelsLine(open));
    }
}

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
    ReportCamera();
}

void TurnOff(void* camera)
{
    Apply(camera, native);
    thirdPerson = false;
    ReportCamera();
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

LARGE_INTEGER lastFrame{};
LARGE_INTEGER ticksPerSecond{};

float SecondsSinceLastFrame()
{
    LARGE_INTEGER now{};
    QueryPerformanceCounter(&now);
    float seconds = 0;

    if (lastFrame.QuadPart != 0)
    {
        seconds = static_cast<float>(now.QuadPart - lastFrame.QuadPart) / static_cast<float>(ticksPerSecond.QuadPart);
    }

    lastFrame = now;

    // A long gap is a loading screen or a pause, not a turn the stick asked for.
    if (seconds > 0.1f)
    {
        return 0;
    }

    return seconds;
}

// Runs on the game's own thread, once per camera update, after the game's code.
void UpdatedCamera(void* camera)
{
    originalUpdate(camera);
    float elapsed = SecondsSinceLastFrame();
    CheckPanels(elapsed);

    if (thirdPerson)
    {
        float yaw = yawPerSecond.load();

        if (yaw != 0 && elapsed > 0)
        {
            reinterpret_cast<AdjustYaw>(required[2].address)(camera, yaw * elapsed);
        }

        // Zooming in lowers the target blend toward the near distance; the game eases the live zoom after it.
        float zoom = zoomPerSecond.load();

        if (zoom != 0 && elapsed > 0)
        {
            float target = Read(camera, ZoomTarget) - zoom * elapsed;
            Write(camera, ZoomTarget, std::fmin(1.0f, std::fmax(0.0f, target)));
        }
    }

    bool pressed = GameInFront() && (GetAsyncKeyState(VK_F8) & 0x8000) != 0;

    if ((pressed && !togglePressed) || toggleRequested.exchange(false))
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

// The pipe GDPilot listens on, opened for overlapped I/O so a pending read
// never holds up a write, and nothing here ever waits on the game's thread.
constexpr wchar_t PipePath[] = L"\\\\.\\pipe\\GDPilot.Game";

bool WriteLine(HANDLE pipe, HANDLE done, const std::string& line)
{
    OVERLAPPED overlapped{};
    overlapped.hEvent = done;
    DWORD written = 0;

    if (!WriteFile(pipe, line.data(), static_cast<DWORD>(line.size()), nullptr, &overlapped) && GetLastError() != ERROR_IO_PENDING)
    {
        return false;
    }

    return GetOverlappedResult(pipe, &overlapped, &written, TRUE) != 0;
}

std::string CameraLine()
{
    if (thirdPerson)
    {
        return "{\"protocol\":1,\"type\":\"camera\",\"third\":true}\n";
    }

    return "{\"protocol\":1,\"type\":\"camera\",\"third\":false}\n";
}

float NumberAfter(const std::string& line, const char* key)
{
    std::size_t at = line.find(key);

    if (at == std::string::npos)
    {
        return 0;
    }

    return std::strtof(line.c_str() + at + std::strlen(key), nullptr);
}

void Obey(const std::string& line)
{
    if (line.find("\"protocol\":1") == std::string::npos)
    {
        Say("Ignored a command from another protocol: %s", line.c_str());
        return;
    }

    if (line.find("\"type\":\"toggle-camera\"") != std::string::npos)
    {
        toggleRequested = true;
        return;
    }

    if (line.find("\"type\":\"look\"") != std::string::npos)
    {
        yawPerSecond = NumberAfter(line, "\"yaw\":");
        zoomPerSecond = NumberAfter(line, "\"zoom\":");
    }
}

void Converse(HANDLE pipe)
{
    HANDLE readDone = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE writeDone = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    char buffer[512];
    std::string pending;
    bool healthy = WriteLine(pipe, writeDone, CameraLine());
    panelsWanted = true;

    while (healthy)
    {
        OVERLAPPED overlapped{};
        overlapped.hEvent = readDone;
        ResetEvent(readDone);

        if (!ReadFile(pipe, buffer, sizeof(buffer), nullptr, &overlapped) && GetLastError() != ERROR_IO_PENDING)
        {
            break;
        }

        bool reading = true;

        while (reading && healthy)
        {
            HANDLE waits[] = {readDone, reportWanted};
            DWORD woke = WaitForMultipleObjects(2, waits, FALSE, INFINITE);

            if (woke == WAIT_OBJECT_0 + 1)
            {
                std::vector<std::string> lines;

                {
                    std::lock_guard<std::mutex> hold(outboxGuard);
                    lines.swap(outbox);
                }

                for (const std::string& line : lines)
                {
                    healthy = healthy && WriteLine(pipe, writeDone, line);
                }

                continue;
            }

            DWORD received = 0;

            if (!GetOverlappedResult(pipe, &overlapped, &received, FALSE) || received == 0)
            {
                healthy = false;
                break;
            }

            pending.append(buffer, received);
            reading = false;
        }

        for (std::size_t end = pending.find('\n'); end != std::string::npos; end = pending.find('\n'))
        {
            Obey(pending.substr(0, end));
            pending.erase(0, end + 1);
        }
    }

    CancelIoEx(pipe, nullptr);
    CloseHandle(readDone);
    CloseHandle(writeDone);
}

DWORD WINAPI Link(LPVOID)
{
    reportWanted = CreateEventW(nullptr, FALSE, FALSE, nullptr);

    for (;;)
    {
        HANDLE pipe = CreateFileW(PipePath, GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, nullptr);

        if (pipe == INVALID_HANDLE_VALUE)
        {
            Sleep(2000);
            continue;
        }

        linked = true;
        Say("Connected to GDPilot.");
        Converse(pipe);
        CloseHandle(pipe);
        linked = false;

        // A stick held when GDPilot went must not keep turning the camera.
        yawPerSecond = 0;
        zoomPerSecond = 0;
        Say("GDPilot went away; listening for it again.");
    }
}

DWORD WINAPI Start(LPVOID)
{
    OpenJournal();
    Say("GDPilot in-game part loaded.");
    QueryPerformanceFrequency(&ticksPerSecond);

    if (FindEverything() && Attach())
    {
        engineSlot = reinterpret_cast<void**>(GetProcAddress(GetModuleHandleW(L"Game.dll"), "?gGameEngine@GAME@@3PEAVGameEngine@1@EA"));

        if (engineSlot == nullptr)
        {
            Say("Panel reports off: the game no longer exports gGameEngine.");
        }

        HANDLE thread = CreateThread(nullptr, 0, Link, nullptr, 0, nullptr);

        if (thread != nullptr)
        {
            CloseHandle(thread);
        }
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
