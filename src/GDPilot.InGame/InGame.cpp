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

// A first run finds camera.ini already there, holding every setting at its
// default with a word on each, so tuning the view never starts from nothing.
void WriteDefaultSettings()
{
    if (GetFileAttributesW(settingsPath.c_str()) != INVALID_FILE_ATTRIBUTES)
    {
        return;
    }

    FILE* file = nullptr;

    if (_wfopen_s(&file, settingsPath.c_str(), L"w") != 0 || file == nullptr)
    {
        return;
    }

    std::fputs(
        "; GDPilot's third-person camera. Read each time third person turns on.\n"
        "; Distances are in world units; the game's own camera runs from 20 to 48.\n"
        "[camera]\n"
        "distance_min=4\n"
        "distance_max=30\n"
        "distance_start=10\n"
        "; Tilt in degrees at the nearest and the farthest zoom.\n"
        "pitch_min=12\n"
        "pitch_max=40\n"
        "field_of_view=50\n"
        "; 1 keeps the camera in front of anything between it and the character.\n"
        "collision=1\n"
        "collision_skin=0.35\n"
        "collision_extend=18\n"
        "; Lock-on: how far above an enemy's feet the cursor and the camera aim,\n"
        "; and how quickly the camera turns to face it.\n"
        "lock_aim_height=1\n"
        "lock_turn_speed=8\n"
        "; How far to the right of the character a locked enemy is framed, in degrees.\n"
        "lock_offset=6\n",
        file);
    std::fclose(file);
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
    WriteDefaultSettings();
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

// Camera collision's settings, from camera.ini, and its state: how far out the
// camera may be this frame, and how often the game has refused a level query.
bool collisionOn = false;
float collisionSkin = 0.35f;
float collisionExtend = 18;
float arm = 0;
int refusals = 0;
bool pulledIn = false;
bool collideObjects = false;
unsigned int collisionSurface = 1;
float collisionStart = 1.5f;
bool togglePressed = false;

// Lock-on's settings, from camera.ini: how far above a target's feet the cursor
// and the camera aim, and how quickly the camera turns to face it.
float aimHeight = 1;
float lockTurn = 8;

// How far to one side of the character a locked enemy is framed, in radians,
// so the character never stands between the camera and its target. Positive
// frames it to the right; six degrees clears the character's shoulder.
float lockOffset = 0.105f;

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

// The way to the player's bags: the main player, its controller by id through
// the game's object manager, then the controller's inventory. The id offset,
// the unexported lookup and its first bytes are GrimAction's, mapped on this
// same build; the lookup is only called if those bytes still match.
using GetMainPlayer = void* (*)(void* engine);
using GetManager = void* (*)();
using FindObject = void* (*)(void* manager, unsigned int id);
using GetInventoryCtrl = void* (*)(void* controller);
using GetSack = const void* (*)(const void* inventory, int sack);

GetMainPlayer mainPlayer = nullptr;
GetManager objectManager = nullptr;
FindObject findObject = nullptr;
GetInventoryCtrl inventoryCtrl = nullptr;
GetSack sackItems = nullptr;
const void* controllerPlayerTable = nullptr;

using GetSackObject = void* (*)(const void* inventory, int sack);
using GetGridSide = unsigned int (*)(const void* sack);
using GetCellSize = void (*)(void* engine, float& width, float& height);

GetSackObject sackObject = nullptr;
GetGridSide gridWidth = nullptr;
GetGridSide gridHeight = nullptr;
GetCellSize cellSize = nullptr;

constexpr std::size_t PlayerControllerId = 0x16c0;
constexpr std::size_t FindObjectAddress = 0x19d40;
constexpr char FindObjectPrefix[] = "48895c2408488974241889542410574883ec208bda488bf1";

bool FindInventoryRoute()
{
    HMODULE game = GetModuleHandleW(L"Game.dll");
    HMODULE engine = GetModuleHandleW(L"Engine.dll");
    mainPlayer = reinterpret_cast<GetMainPlayer>(GetProcAddress(game, "?GetMainPlayer@GameEngine@GAME@@QEBAPEAVPlayer@2@XZ"));
    objectManager = reinterpret_cast<GetManager>(GetProcAddress(engine, "?Get@?$Singleton@VObjectManager@GAME@@@GAME@@SAPEAVObjectManager@2@XZ"));
    inventoryCtrl = reinterpret_cast<GetInventoryCtrl>(GetProcAddress(game, "?GetInventoryCtrl@ControllerPlayer@GAME@@QEAAAEAVPlayerInventoryCtrl@2@XZ"));
    sackItems = reinterpret_cast<GetSack>(GetProcAddress(game, "?GetInventoryInSack@PlayerInventoryCtrl@GAME@@QEBAAEBV?$map@IVRect@GAME@@@mem@@H@Z"));
    sackObject = reinterpret_cast<GetSackObject>(GetProcAddress(game, "?GetSack@PlayerInventoryCtrl@GAME@@QEAAPEAVInventorySack@2@H@Z"));
    gridWidth = reinterpret_cast<GetGridSide>(GetProcAddress(game, "?GetGridWidth@InventorySack@GAME@@QEBAIXZ"));
    gridHeight = reinterpret_cast<GetGridSide>(GetProcAddress(game, "?GetGridHeight@InventorySack@GAME@@QEBAIXZ"));
    cellSize = reinterpret_cast<GetCellSize>(GetProcAddress(game, "?GetInventoryCellSize@GameEngine@GAME@@QEAAXAEAM0@Z"));
    controllerPlayerTable = reinterpret_cast<const void*>(GetProcAddress(game, "??_7ControllerPlayer@GAME@@6B@"));
    void* find = reinterpret_cast<unsigned char*>(game) + FindObjectAddress;

    if (mainPlayer == nullptr || objectManager == nullptr || inventoryCtrl == nullptr || sackItems == nullptr || controllerPlayerTable == nullptr || sackObject == nullptr || gridWidth == nullptr || gridHeight == nullptr || cellSize == nullptr)
    {
        sackObject = nullptr;
        Say("Item reports off: an inventory function is no longer exported.");
        return false;
    }

    if (!StartsWith(find, FindObjectPrefix))
    {
        sackObject = nullptr;
        Say("Item reports off: the object lookup has changed since it was mapped.");
        return false;
    }

    findObject = reinterpret_cast<FindObject>(find);
    Say("Found the way to the player's bags.");
    return true;
}

// The player's inventory controller, or null. Guarded throughout, and the
// controller is only used if its type is the player controller's.
const void* PlayerInventory()
{
    __try
    {
        void* engine = *engineSlot;

        if (engine == nullptr)
        {
            return nullptr;
        }

        unsigned char* player = static_cast<unsigned char*>(mainPlayer(engine));

        if (player == nullptr)
        {
            return nullptr;
        }

        unsigned int id = *reinterpret_cast<unsigned int*>(player + PlayerControllerId);
        void* manager = objectManager();

        if (manager == nullptr)
        {
            return nullptr;
        }

        void* controller = findObject(manager, id);

        if (controller == nullptr || *reinterpret_cast<const void**>(controller) != controllerPlayerTable)
        {
            return nullptr;
        }

        return inventoryCtrl(controller);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return nullptr;
    }
}

// One item as the game records it: its id, then its rectangle in the bag, in
// the same units as an inventory cell's size.
struct RawItem
{
    unsigned int id;
    float x;
    float y;
    float width;
    float height;
};

// A bag's items live in a standard ordered tree. The map holds the head node,
// whose left link is the first item. Every node has left, parent and right
// links, a flag at 0x19 marking the head, the item id at 0x1c and its
// rectangle at 0x20. Read live on 2026-10-28, where the second bag held exactly
// the nine items, with the sizes, that its capture shows.
constexpr std::size_t NodeLeft = 0x0;
constexpr std::size_t NodeParent = 0x8;
constexpr std::size_t NodeRight = 0x10;
constexpr std::size_t NodeIsHead = 0x19;
constexpr std::size_t NodeId = 0x1c;
constexpr std::size_t NodeRect = 0x20;
constexpr int MostItems = 200;

const unsigned char* Link(const unsigned char* node, std::size_t offset)
{
    return *reinterpret_cast<const unsigned char* const*>(node + offset);
}

// The next node in order: the leftmost below the right link, or else the first
// ancestor reached from its left.
const unsigned char* Next(const unsigned char* node)
{
    const unsigned char* right = Link(node, NodeRight);

    if (right[NodeIsHead] == 0)
    {
        node = right;

        while (Link(node, NodeLeft)[NodeIsHead] == 0)
        {
            node = Link(node, NodeLeft);
        }

        return node;
    }

    const unsigned char* parent = Link(node, NodeParent);

    while (parent[NodeIsHead] == 0 && node == Link(parent, NodeRight))
    {
        node = parent;
        parent = Link(parent, NodeParent);
    }

    return parent;
}

struct SackReading
{
    unsigned int columns;
    unsigned int rows;
    float cellWidth;
    float cellHeight;
    int count;
    RawItem items[MostItems];
};

// Reads one bag whole, or returns false. Guarded throughout, and nothing here
// owns memory, so a fault leaves nothing behind.
bool ReadSack(const void* inventory, int sack, SackReading& reading)
{
    __try
    {
        void* bag = sackObject(inventory, sack);
        const void* map = sackItems(inventory, sack);

        if (bag == nullptr || map == nullptr)
        {
            return false;
        }

        reading.columns = gridWidth(bag);
        reading.rows = gridHeight(bag);
        cellSize(*engineSlot, reading.cellWidth, reading.cellHeight);
        const unsigned char* head = *static_cast<const unsigned char* const*>(map);
        const unsigned char* node = Link(head, NodeLeft);
        reading.count = 0;

        while (node[NodeIsHead] == 0 && reading.count < MostItems)
        {
            RawItem& item = reading.items[reading.count];
            item.id = *reinterpret_cast<const unsigned int*>(node + NodeId);
            std::memcpy(&item.x, node + NodeRect, sizeof(float) * 4);
            reading.count++;
            node = Next(node);
        }

        return reading.cellWidth > 0 && reading.cellHeight > 0;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

int Cells(float units, float cell)
{
    return static_cast<int>(std::lround(units / cell));
}

// The grids each bag is calibrated as: the inventory shows the same two bags
// alone, beside a trader and beside the stash, in three different places.
struct SackNames
{
    int sack;
    const char* names[3];
};

constexpr SackNames Bags[] = {
    {0, {"Inventory", "Vendor.Bag1", "Stash.Bag1"}},
    {1, {"Inventory.Bag2", "Vendor.Bag2", "Stash.Bag2"}},
};

std::string sentLayouts[std::size(Bags)];
float sinceLayoutCheck = 0;
std::atomic<bool> layoutsWanted{false};
SackReading bagReading{};

std::string LayoutLine(const char* grid, const SackReading& bag)
{
    std::string line = std::string("{\"protocol\":1,\"type\":\"layout\",\"grid\":\"") + grid + "\",\"columns\":" + std::to_string(bag.columns) + ",\"rows\":" + std::to_string(bag.rows) + ",\"items\":[";

    for (int index = 0; index < bag.count; index++)
    {
        const RawItem& item = bag.items[index];

        if (index > 0)
        {
            line += ",";
        }

        line += "{\"id\":" + std::to_string(item.id) + ",\"column\":" + std::to_string(Cells(item.x, bag.cellWidth)) + ",\"row\":" + std::to_string(Cells(item.y, bag.cellHeight)) + ",\"width\":" + std::to_string(Cells(item.width, bag.cellWidth)) + ",\"height\":" + std::to_string(Cells(item.height, bag.cellHeight)) + "}";
    }

    return line + "]}\n";
}

// Four times a second while the inventory is open, each bag is read and sent
// again only if anything in it moved.
void CheckLayouts(float elapsed)
{
    sinceLayoutCheck += elapsed;

    if (sackObject == nullptr || sinceLayoutCheck < 0.25f || lastPanels.find("Inventory") == std::string::npos)
    {
        return;
    }

    sinceLayoutCheck = 0;
    const void* inventory = PlayerInventory();

    if (inventory == nullptr)
    {
        return;
    }

    if (layoutsWanted.exchange(false))
    {
        for (std::string& sent : sentLayouts)
        {
            sent.clear();
        }
    }

    for (std::size_t bag = 0; bag < std::size(Bags); bag++)
    {
        if (!ReadSack(inventory, Bags[bag].sack, bagReading))
        {
            continue;
        }

        std::string lines;

        for (const char* name : Bags[bag].names)
        {
            lines += LayoutLine(name, bagReading);
        }

        if (lines != sentLayouts[bag])
        {
            sentLayouts[bag] = lines;
            Say("Bag %d: %d items in %u by %u cells.", Bags[bag].sack, bagReading.count, bagReading.columns, bagReading.rows);
            Post(lines);
        }
    }
}

// The player's health, read through the game's own accessors: current life
// from the character, its limit from the character's bio. Reported as a
// fraction five times a second, and only when it has moved, so GDPilot can
// make the pad rumble when life runs low or a heavy blow lands.
using CurrentLife = double (*)(const void* character);
using CharacterBio = const void* (*)(const void* character);
using LifeLimit = float (*)(const void* bio);

CurrentLife currentLife = nullptr;
CharacterBio characterBio = nullptr;
LifeLimit lifeLimit = nullptr;
float sinceHealthCheck = 0;
std::atomic<int> sentHealth{-1};

void FindHealth()
{
    HMODULE game = GetModuleHandleW(L"Game.dll");
    currentLife = reinterpret_cast<CurrentLife>(GetProcAddress(game, "?GetCurrentLife@Character@GAME@@QEBA?BNXZ"));
    characterBio = reinterpret_cast<CharacterBio>(GetProcAddress(game, "?GetCharacterBio@Character@GAME@@QEBAAEBVCharacterBio@2@XZ"));
    lifeLimit = reinterpret_cast<LifeLimit>(GetProcAddress(game, "?GetLifeLimit@CharacterBio@GAME@@QEBA?BMXZ"));

    if (currentLife == nullptr || characterBio == nullptr || lifeLimit == nullptr)
    {
        currentLife = nullptr;
        Say("Health reports off: a life function is no longer exported.");
    }
}

// Life as a fraction of its limit, or a negative number when it cannot be read.
double HealthFraction()
{
    __try
    {
        const void* player = mainPlayer(*engineSlot);

        if (player == nullptr)
        {
            return -1;
        }

        double life = currentLife(player);
        float limit = lifeLimit(characterBio(player));

        if (!(limit > 0) || !std::isfinite(life))
        {
            return -1;
        }

        return life / limit;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return -1;
    }
}

void CheckHealth(float elapsed)
{
    sinceHealthCheck += elapsed;

    if (currentLife == nullptr || mainPlayer == nullptr || sinceHealthCheck < 0.2f)
    {
        return;
    }

    sinceHealthCheck = 0;
    double fraction = HealthFraction();

    if (fraction < 0)
    {
        return;
    }

    // Whole percent, so a regenerating trickle does not send a line every check.
    int percent = static_cast<int>(std::lround(std::fmin(1.0, fraction) * 100));

    if (percent != sentHealth)
    {
        // Logged in tens, enough to follow a fight without a line per point.
        if (sentHealth < 0 || percent / 10 != sentHealth / 10)
        {
            Say("Health: %d%%", percent);
        }

        sentHealth = percent;
        Post("{\"protocol\":1,\"type\":\"health\",\"percent\":" + std::to_string(percent) + "}\n");
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
    arm = behind[0];
    refusals = 0;
    collisionOn = Setting(L"collision", 1) != 0;
    collisionSkin = Setting(L"collision_skin", 0.35f);
    collisionExtend = Setting(L"collision_extend", 18);
    collideObjects = Setting(L"collision_objects", 1) != 0;
    collisionSurface = static_cast<unsigned int>(Setting(L"collision_surface", 1));
    collisionStart = Setting(L"collision_start", 1.5f);
    lockTurn = Setting(L"lock_turn_speed", 8);
    lockOffset = Setting(L"lock_offset", 6) * 3.14159265f / 180;
    aimHeight = Setting(L"lock_aim_height", 1);
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

// Camera collision, through the game's own level query: a ray from the
// character out to the camera, ignoring the character. The functions are
// Engine.dll's exports, checked against the bytes GrimAction mapped them from
// on this build; the camera's world position sits at 0x94, and a ray is an
// origin then a direction, three floats each.
struct WorldPoint
{
    void* region;
    float x;
    float y;
    float z;
};

struct LevelRay
{
    float origin[3];
    float direction[3];
};

using GetRegion = void* (*)(const void* camera);
using ViewOffset = WorldPoint* (*)(const void* camera, WorldPoint* result, const WorldPoint* focus);
using LevelOf = void* (*)(const void* region);
using Intersect = void (*)(void* level, const LevelRay* ray, void* intersection, unsigned int surface, void** hit, float most, const void* ignore, bool entities);

GetRegion cameraRegion = nullptr;
ViewOffset viewOffset = nullptr;
LevelOf regionLevel = nullptr;
Intersect intersect = nullptr;

constexpr std::size_t CameraPosition = 0x94;
constexpr int MostRefusals = 30;


struct CollisionEntry
{
    const char* name;
    const char* prefix;
    void** slot;
};

void FindCollision()
{
    HMODULE engine = GetModuleHandleW(L"Engine.dll");
    CollisionEntry entries[] = {
        {"?GetRegion@WorldCamera@GAME@@QEBAPEAVRegion@2@XZ", "4883ec48488b01488d542420", reinterpret_cast<void**>(&cameraRegion)},
        {"?CalculateViewPosition@WorldCamera@GAME@@MEBA?AVWorldVec3@2@AEBV32@@Z", "48895c24084889742410574883ec60", reinterpret_cast<void**>(&viewOffset)},
        {"?GetLevelPtr@Region@GAME@@QEBAPEAVLevel@2@XZ", "488b4168c3", reinterpret_cast<void**>(&regionLevel)},
        {"?GetIntersection@Level@GAME@@QEBAXAEBVRay@2@AEAVIntersection@2@W4PhysicsSurface@2@PEAPEAVEntity@2@MPEBV62@_N@Z", "488bc4488958084889681048897018", reinterpret_cast<void**>(&intersect)},
    };

    for (CollisionEntry& entry : entries)
    {
        void* address = reinterpret_cast<void*>(GetProcAddress(engine, entry.name));

        if (address == nullptr || !StartsWith(address, entry.prefix))
        {
            Say("Collision off: %s is missing or has changed.", entry.name);
            intersect = nullptr;
            return;
        }

        *entry.slot = address;
    }

    Say("Found the level query; the camera will stay out of walls.");
}

// How far the camera can be from the character before the level is in the way,
// infinity when nothing is, or false when the game refused the query.
bool ClearDistance(void* camera, const void* player, float most, float& distance)
{
    __try
    {
        void* region = cameraRegion(camera);

        if (region == nullptr)
        {
            return false;
        }

        WorldPoint zero{region, 0, 0, 0};
        WorldPoint offset{};
        viewOffset(camera, &offset, &zero);
        const float* position = reinterpret_cast<const float*>(static_cast<const unsigned char*>(camera) + CameraPosition);
        float length = std::sqrt(offset.x * offset.x + offset.y * offset.y + offset.z * offset.z);

        if (!std::isfinite(length) || length < 0.0001f)
        {
            return false;
        }

        // The camera sits at the character plus the offset, so the character is the
        // camera minus it. The ray starts a little way out, past the character's own
        // body, which the query would otherwise meet first.
        float along[3] = {offset.x / length, offset.y / length, offset.z / length};
        LevelRay ray{{position[0] - offset.x + along[0] * collisionStart, position[1] - offset.y + along[1] * collisionStart, position[2] - offset.z + along[2] * collisionStart}, {along[0], along[1], along[2]}};
        void* level = regionLevel(region);

        if (level == nullptr)
        {
            return false;
        }

        alignas(16) unsigned char intersection[128]{};
        void* hit = nullptr;
        intersect(level, &ray, intersection, collisionSurface, &hit, most, player, collideObjects);
        std::memcpy(&distance, intersection, sizeof(distance));
        distance += collisionStart;
        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

const void* MainPlayer()
{
    __try
    {
        if (mainPlayer == nullptr || *engineSlot == nullptr)
        {
            return nullptr;
        }

        return mainPlayer(*engineSlot);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return nullptr;
    }
}

// Pulls the camera in front of anything between it and the character at once,
// and lets it back out gradually, so it never sits inside a wall and never
// snaps outward. The player's own zoom stays the target throughout.
void Collide(void* camera, float elapsed)
{
    if (!collisionOn || intersect == nullptr || refusals >= MostRefusals)
    {
        return;
    }

    float nearest = Read(camera, ZoomNear);
    float farthest = Read(camera, ZoomFar);
    float chosen = Read(camera, ZoomTarget);
    float desired = nearest + (farthest - nearest) * chosen;
    float hit = 0;

    if (!ClearDistance(camera, MainPlayer(), desired + collisionSkin, hit))
    {
        refusals++;

        if (refusals == MostRefusals)
        {
            Say("Collision off for this session: the game refused the level query %d times.", MostRefusals);
        }

        return;
    }

    float allowed = desired;

    if (std::isfinite(hit) && hit < desired + collisionSkin)
    {
        allowed = std::fmax(nearest, hit - collisionSkin);
    }

    if (allowed < arm)
    {
        arm = allowed;
    }
    else
    {
        arm = std::fmin(allowed, arm + collisionExtend * elapsed);
    }

    bool inside = arm < desired - 0.01f;

    // Logged only as it changes, so the log shows each obstacle once.
    if (inside != pulledIn)
    {
        pulledIn = inside;

        if (inside)
        {
            Say("Collision: pulled in to %.1f of %.1f, something at %.1f.", arm, desired, hit);
        }
        else
        {
            Say("Collision: clear again at %.1f.", desired);
        }
    }

    if (inside)
    {
        // SetZoom moves the live and target zoom together; the target is then given back to the player.
        reinterpret_cast<SetZoom>(required[1].address)(camera, arm);
        Write(camera, ZoomTarget, chosen);
    }
}

// Lock-on's eyes: the enemies near the player, found with the game's own
// target query, so nothing the game would refuse a skill is ever offered. Its
// filter takes characters only, never townsfolk, and only those alive and
// seen; asked without friends, it asks the game's faction rules for foes. Each id is then resolved to its character, and projected onto
// the screen through the camera the game draws with.
using TargetsInRadius = void (*)(const void* engine, const void* character, const WorldPoint* centre, float radius, void* found, unsigned int ignore, bool friends, bool hidden);
using PathPosition = WorldPoint* (*)(const void* character, WorldPoint* result);
using ProjectPoint = float* (*)(const void* camera, float* result, const WorldPoint* point, const int* viewport);
using ObjectId = unsigned int (*)(const void* object);
using ClassOf = const unsigned char* (*)(const void* object);
using ActorHeight = float (*)(const void* actor);

TargetsInRadius targetsInRadius = nullptr;
PathPosition pathPosition = nullptr;
ProjectPoint projectPoint = nullptr;
ObjectId objectId = nullptr;
ActorHeight actorHeight = nullptr;
const unsigned char* characterClass = nullptr;
const unsigned char* npcClass = nullptr;

// The game appends to a vector of its own kind. This one lives for the whole
// session and is emptied before each query, so only the game's allocator ever
// grows or frees its storage.
struct IdList
{
    unsigned int* first;
    unsigned int* last;
    unsigned int* end;
};

IdList found{};

// A region's corner sits at whole world units, at these offsets in the region,
// as the game's own Project reads them; a position is relative to its region.
constexpr std::size_t RegionCorner = 0x3c;
constexpr std::size_t ClassParent = 0x10;
constexpr int Up = 1;
constexpr float ScanRadius = 40;
constexpr int MostTargets = 24;

void FindTargets()
{
    HMODULE game = GetModuleHandleW(L"Game.dll");
    HMODULE engine = GetModuleHandleW(L"Engine.dll");
    CollisionEntry entries[] = {
        {"?GetTargetsInRadius@GameEngine@GAME@@QEBAXAEBVCharacter@2@AEBVWorldVec3@2@MAEAV?$vector@I@mem@@I_N3@Z", "48895c24084889742410574883ec60488bf10f29742450488bca488d9af01900", reinterpret_cast<void**>(&targetsInRadius)},
        {"?GetPathPosition@Character@GAME@@QEBA?AVWorldVec3@2@XZ", "40534883ec20488bdaff15", reinterpret_cast<void**>(&pathPosition)},
    };
    CollisionEntry engineEntries[] = {
        {"?Project@WorldCamera@GAME@@QEBA?AVVec2@2@AEBVWorldVec3@2@AEBVViewport@2@@Z", "48895c240848896c24104889742418574883ec40488b0148", reinterpret_cast<void**>(&projectPoint)},
        {"?GetObjectId@Object@GAME@@QEBAIXZ", "8b412cc3", reinterpret_cast<void**>(&objectId)},
        {"?GetHeight@Actor@GAME@@QEBAMXZ", "f30f1081a0040000f30f5981ac030000c3", reinterpret_cast<void**>(&actorHeight)},
    };
    characterClass = reinterpret_cast<const unsigned char*>(GetProcAddress(game, "?classInfo@Character@GAME@@1VRTTI_ClassInfo@2@B"));
    npcClass = reinterpret_cast<const unsigned char*>(GetProcAddress(game, "?classInfo@Npc@GAME@@1VRTTI_ClassInfo@2@B"));

    // A viewport is four whole numbers, as its constructor writes them; one is built here the same way.
    void* viewport = reinterpret_cast<void*>(GetProcAddress(engine, "??0Viewport@GAME@@QEAA@HHHH@Z"));
    bool fine = characterClass != nullptr && npcClass != nullptr && viewport != nullptr && StartsWith(viewport, "8b44242889410c488bc189114489410444894908c3");

    for (CollisionEntry& entry : entries)
    {
        void* address = reinterpret_cast<void*>(GetProcAddress(game, entry.name));
        fine = fine && address != nullptr && StartsWith(address, entry.prefix);
        *entry.slot = address;
    }

    for (CollisionEntry& entry : engineEntries)
    {
        void* address = reinterpret_cast<void*>(GetProcAddress(engine, entry.name));
        fine = fine && address != nullptr && StartsWith(address, entry.prefix);
        *entry.slot = address;
    }

    if (!fine || mainPlayer == nullptr || findObject == nullptr || currentLife == nullptr || viewOffset == nullptr)
    {
        targetsInRadius = nullptr;
        Say("Lock-on off: a targeting function is missing or has changed.");
        return;
    }

    // Read here as well as on each switch, since lock-on aims in the game's own camera too.
    aimHeight = Setting(L"lock_aim_height", 1);
    Say("Found the game's target query; lock-on can see enemies.");
}

bool IsA(const void* object, const unsigned char* wanted)
{
    const void* const* table = *static_cast<const void* const* const*>(object);
    const unsigned char* kind = reinterpret_cast<ClassOf>(table[0])(object);

    for (int depth = 0; kind != nullptr && depth < 16; depth++)
    {
        if (kind == wanted)
        {
            return true;
        }

        kind = *reinterpret_cast<const unsigned char* const*>(kind + ClassParent);
    }

    return false;
}

void InWorld(const WorldPoint& point, float world[3])
{
    const int* corner = reinterpret_cast<const int*>(static_cast<const unsigned char*>(point.region) + RegionCorner);
    world[0] = point.x + static_cast<float>(corner[0]);
    world[1] = point.y + static_cast<float>(corner[1]);
    world[2] = point.z + static_cast<float>(corner[2]);
}

// One enemy as lock-on sees it: where it is on the screen, as fractions of the
// window, how far from the player, and how much life it has left.
struct Seen
{
    unsigned int id;
    float x;
    float y;
    float distance;
    int health;
    bool onScreen;
};

HWND gameWindow = nullptr;

BOOL CALLBACK ConsiderWindow(HWND window, LPARAM)
{
    DWORD owner = 0;
    GetWindowThreadProcessId(window, &owner);

    if (owner == GetCurrentProcessId() && IsWindowVisible(window) && GetWindow(window, GW_OWNER) == nullptr)
    {
        gameWindow = window;
        return FALSE;
    }

    return TRUE;
}

bool Viewport(int viewport[4])
{
    if (gameWindow == nullptr || !IsWindow(gameWindow))
    {
        gameWindow = nullptr;
        EnumWindows(ConsiderWindow, 0);
    }

    RECT client{};

    if (gameWindow == nullptr || !GetClientRect(gameWindow, &client) || client.right <= 0 || client.bottom <= 0)
    {
        return false;
    }

    viewport[0] = 0;
    viewport[1] = 0;
    viewport[2] = client.right;
    viewport[3] = client.bottom;
    return true;
}

bool described = false;

// Whether the way from the player to a target is clear, through the same level
// query collision uses, from chest height to chest height, so a lock is never
// taken through a wall. Like collision's, the ray starts a little way out, past
// the player's own body, and the target's body ends it, so only a hit well
// short of the target counts. Without the level query, everything is in sight.
bool InSight(const WorldPoint& from, const float relative[3], const void* player)
{
    if (intersect == nullptr)
    {
        return true;
    }

    void* level = regionLevel(from.region);
    float length = std::sqrt(relative[0] * relative[0] + relative[1] * relative[1] + relative[2] * relative[2]);

    if (level == nullptr || length < 2)
    {
        return true;
    }

    float along[3] = {relative[0] / length, relative[1] / length, relative[2] / length};
    LevelRay ray{{from.x + along[0] * collisionStart, from.y + aimHeight + along[1] * collisionStart, from.z + along[2] * collisionStart}, {along[0], along[1], along[2]}};
    alignas(16) unsigned char intersection[128]{};
    void* hit = nullptr;
    intersect(level, &ray, intersection, collisionSurface, &hit, length, player, true);
    float clear = 0;
    std::memcpy(&clear, intersection, sizeof(clear));
    return !std::isfinite(clear) || clear + collisionStart >= length - 2;
}

// Reads every enemy in range, or returns -1 when the game cannot say. Guarded
// throughout; nothing here owns memory, so a fault leaves nothing behind.
int ScanTargets(void* camera, const int viewport[4], Seen seen[], int most)
{
    __try
    {
        void* engine = *engineSlot;
        const void* player = mainPlayer(engine);
        void* manager = objectManager();

        if (player == nullptr || manager == nullptr)
        {
            return -1;
        }

        WorldPoint centre{};
        pathPosition(player, &centre);
        found.last = found.first;
        targetsInRadius(engine, player, &centre, ScanRadius, &found, objectId(player), false, false);
        float origin[3]{};
        InWorld(centre, origin);
        WorldPoint zero{cameraRegion(camera), 0, 0, 0};
        WorldPoint offset{};
        viewOffset(camera, &offset, &zero);
        float back[3] = {offset.x, offset.y, offset.z};
        int count = 0;

        for (const unsigned int* id = found.first; id != found.last && count < most; id++)
        {
            const void* target = findObject(manager, *id);

            if (target == nullptr || !IsA(target, characterClass) || IsA(target, npcClass))
            {
                continue;
            }

            double life = currentLife(target);
            float limit = lifeLimit(characterBio(target));

            if (!(life > 0) || !(limit > 0))
            {
                continue;
            }

            WorldPoint where{};
            pathPosition(target, &where);
            float at[3]{};
            InWorld(where, at);
            float relative[3] = {at[0] - origin[0], at[1] - origin[1], at[2] - origin[2]};

            // In front of the camera when the way from the camera to it runs along the view, not back past the lens.
            float ahead = 0;

            for (int axis = 0; axis < 3; axis++)
            {
                ahead -= (relative[axis] - back[axis]) * back[axis];
            }

            // Aimed at the chest: three fifths of the creature's own height, so a
            // rat and a giant are each hit where they stand tallest.
            float tall = actorHeight(target);
            float lift = aimHeight;

            if (tall > 0.3f && tall < 30)
            {
                lift = tall * 0.6f;
            }

            (&where.x)[Up] += lift;

            float screen[2]{};
            projectPoint(camera, screen, &where, viewport);

            if (!described)
            {
                described = true;
                Say("Lock-on: camera offset %.2f %.2f %.2f; a target %.2f %.2f %.2f from the player, drawn at %.1f, %.1f of %d by %d.", back[0], back[1], back[2], relative[0], relative[1], relative[2], screen[0], screen[1], viewport[2], viewport[3]);
            }

            Seen& entry = seen[count];
            entry.id = *id;
            entry.x = screen[0] / static_cast<float>(viewport[2]);
            entry.y = screen[1] / static_cast<float>(viewport[3]);
            entry.distance = std::sqrt(relative[0] * relative[0] + relative[1] * relative[1] + relative[2] * relative[2]);
            entry.health = static_cast<int>(std::lround(std::fmin(1.0, life / limit) * 100));
            entry.onScreen = ahead > 0 && entry.x >= 0 && entry.x <= 1 && entry.y >= 0 && entry.y <= 1 && InSight(centre, relative, player);
            count++;
        }

        return count;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return -1;
    }
}

Seen seen[MostTargets]{};
float sinceTargetCheck = 0;
std::string lastTargets;
int lastTargetCount = -1;

// Set by the pipe thread on connecting, so a newcomer hears the targets even if none has moved.
std::atomic<bool> targetsWanted{false};

std::string Fixed(float value, int decimals)
{
    char text[32]{};
    std::snprintf(text, sizeof(text), "%.*f", decimals, value);
    return text;
}

// Where the ground round the character falls on the screen: its feet, and
// eight points a few steps out, every eighth of a turn from the way the camera
// looks. GDPilot aims a dodge through these, so a dodge goes where the stick
// points in either camera, perspective and all.
constexpr int RingPoints = 8;
constexpr float RingReach = 6;

struct Ring
{
    float feet[2];
    bool feetShown;
    float points[RingPoints][2];
    bool shown[RingPoints];
};

Ring ring{};

float Heading(const float vector[3]);

bool ReadRing(void* camera, const int viewport[4], Ring& out)
{
    __try
    {
        const void* player = mainPlayer(*engineSlot);

        if (player == nullptr)
        {
            return false;
        }

        WorldPoint centre{};
        pathPosition(player, &centre);
        WorldPoint zero{cameraRegion(camera), 0, 0, 0};
        WorldPoint offset{};
        viewOffset(camera, &offset, &zero);
        float back[3] = {offset.x, offset.y, offset.z};
        float facing = Heading(back) + 3.14159265f;

        for (int index = -1; index < RingPoints; index++)
        {
            float relative[3]{};

            if (index >= 0)
            {
                float angle = facing + index * 2 * 3.14159265f / RingPoints;
                relative[0] = std::cos(angle) * RingReach;
                relative[2] = std::sin(angle) * RingReach;
            }

            float ahead = 0;

            for (int axis = 0; axis < 3; axis++)
            {
                ahead -= (relative[axis] - back[axis]) * back[axis];
            }

            WorldPoint point{centre.region, centre.x + relative[0], centre.y, centre.z + relative[2]};
            float screen[2]{};
            projectPoint(camera, screen, &point, viewport);
            float x = screen[0] / static_cast<float>(viewport[2]);
            float y = screen[1] / static_cast<float>(viewport[3]);
            bool shown = ahead > 0 && std::isfinite(x) && std::isfinite(y);

            if (index < 0)
            {
                out.feet[0] = x;
                out.feet[1] = y;
                out.feetShown = shown;
            }
            else
            {
                out.points[index][0] = x;
                out.points[index][1] = y;
                out.shown[index] = shown;
            }
        }

        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

std::string Point(float x, float y, bool shown)
{
    if (!shown)
    {
        return "null";
    }

    return "[" + Fixed(x, 4) + "," + Fixed(y, 4) + "]";
}

std::string RingText(const Ring& around)
{
    std::string text = ",\"feet\":" + Point(around.feet[0], around.feet[1], around.feetShown) + ",\"ring\":[";

    for (int index = 0; index < RingPoints; index++)
    {
        if (index > 0)
        {
            text += ",";
        }

        text += Point(around.points[index][0], around.points[index][1], around.shown[index]);
    }

    return text + "]";
}

// Thirty times a second, enough for the cursor to stay on a running enemy, and
// sent only when something moved.
void CheckTargets(void* camera, float elapsed)
{
    sinceTargetCheck += elapsed;

    if (targetsInRadius == nullptr || !linked || sinceTargetCheck < 1.0f / 30)
    {
        return;
    }

    sinceTargetCheck = 0;
    int viewport[4]{};

    if (!Viewport(viewport))
    {
        return;
    }

    int count = ScanTargets(camera, viewport, seen, MostTargets);

    if (count < 0)
    {
        return;
    }

    if ((count == 0) != (lastTargetCount == 0))
    {
        Say("Targets: %d in range.", count);
    }

    lastTargetCount = count;
    std::string line = "{\"protocol\":1,\"type\":\"targets\",\"list\":[";

    for (int index = 0; index < count; index++)
    {
        const Seen& entry = seen[index];

        if (index > 0)
        {
            line += ",";
        }

        line += "{\"id\":" + std::to_string(entry.id) + ",\"x\":" + Fixed(entry.x, 4) + ",\"y\":" + Fixed(entry.y, 4) + ",\"distance\":" + Fixed(entry.distance, 1) + ",\"health\":" + std::to_string(entry.health);

        if (entry.onScreen)
        {
            line += ",\"seen\":true}";
        }
        else
        {
            line += ",\"seen\":false}";
        }
    }

    line += "]";

    if (ReadRing(camera, viewport, ring))
    {
        line += RingText(ring);
    }

    line += "}\n";

    if (targetsWanted.exchange(false))
    {
        lastTargets.clear();
    }

    if (line != lastTargets)
    {
        lastTargets = line;
        Post(line);
    }
}

// While locked in third person, the camera turns every frame until it looks
// past the character at the target, the way an action game frames a duel.
std::atomic<unsigned int> lockedId{0};
unsigned int steeringId = 0;
float yawSign = 0;
constexpr float Pi = 3.14159265f;

// A heading across the ground, ignoring height.
float Heading(const float vector[3])
{
    return std::atan2(vector[2], vector[0]);
}

float Wrap(float angle)
{
    while (angle > Pi)
    {
        angle -= 2 * Pi;
    }

    while (angle < -Pi)
    {
        angle += 2 * Pi;
    }

    return angle;
}

bool CameraBack(void* camera, float back[3])
{
    __try
    {
        WorldPoint zero{cameraRegion(camera), 0, 0, 0};
        WorldPoint offset{};
        viewOffset(camera, &offset, &zero);
        back[0] = offset.x;
        back[1] = offset.y;
        back[2] = offset.z;
        return std::isfinite(offset.x) && std::isfinite(offset.z);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

// Where the target lies from the player, or false once it is gone.
bool Bearing(unsigned int id, float toward[3])
{
    __try
    {
        const void* player = mainPlayer(*engineSlot);
        void* manager = objectManager();

        if (player == nullptr || manager == nullptr)
        {
            return false;
        }

        const void* target = findObject(manager, id);

        if (target == nullptr || !IsA(target, characterClass))
        {
            return false;
        }

        WorldPoint from{};
        WorldPoint to{};
        pathPosition(player, &from);
        pathPosition(target, &to);
        float origin[3]{};
        InWorld(from, origin);
        InWorld(to, toward);

        for (int axis = 0; axis < 3; axis++)
        {
            toward[axis] -= origin[axis];
        }

        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

// The game eases the view toward its yaw rather than jumping, so both which
// way the yaw turns the view and how long the view takes to follow are
// measured, not assumed: once the view has held still, a small turn is made,
// and the view's heading is sampled until it has settled.
float easing = 0.2f;
float turnOnItsWay = 0;
float stillFor = 0;
float lastHeading = 0;
bool probing = false;
float probeHeading = 0;
float probeClock = 0;
constexpr int ProbeSamples = 128;
float probeTimes[ProbeSamples]{};
float probeMoves[ProbeSamples]{};
int probeCount = 0;

// True once both are known; until then it may spend this frame probing.
bool MeasureYaw(void* camera, const float back[3], float elapsed)
{
    float heading = Heading(back);

    if (!probing)
    {
        if (std::fabs(Wrap(heading - lastHeading)) > 0.001f)
        {
            stillFor = 0;
        }
        else
        {
            stillFor += elapsed;
        }

        lastHeading = heading;

        if (stillFor < 0.15f)
        {
            return false;
        }

        probeHeading = heading;
        reinterpret_cast<AdjustYaw>(required[2].address)(camera, 0.05f);
        probing = true;
        probeClock = 0;
        probeCount = 0;
        return false;
    }

    probeClock += elapsed;

    if (probeCount < ProbeSamples)
    {
        probeTimes[probeCount] = probeClock;
        probeMoves[probeCount] = std::fabs(Wrap(heading - probeHeading));
        probeCount++;
    }

    if (probeClock < 0.8f)
    {
        return false;
    }

    probing = false;
    float change = Wrap(heading - probeHeading);

    if (std::fabs(change) < 0.01f)
    {
        yawSign = 2;
        Say("Lock-on: the camera's turn could not be measured, so it will not follow the target.");
        return false;
    }

    if (change > 0)
    {
        yawSign = 1;
    }
    else
    {
        yawSign = -1;
    }

    // How long the view takes to cover all but a third of a turn.
    easing = probeClock;

    for (int index = 0; index < probeCount; index++)
    {
        if (probeMoves[index] >= 0.632f * std::fabs(change))
        {
            easing = probeTimes[index];
            break;
        }
    }

    Say("Lock-on: a turn of 0.05 moves the view's heading by %.3f, eased over %.2f s.", change, easing);
    return true;
}

// Recentring: the camera swings round behind the character, as action games
// answer a lock-on press with nothing to lock. The character's own turn is the
// rotation in its world coordinates, three axes of three floats from 0xf0 in
// the character; its forward axis is the third, read live on 2026-10-30.
constexpr std::size_t CharacterAxes = 0xf0;
constexpr int ForwardAxis = 2;
std::atomic<bool> recenterRequested{false};
bool recentering = false;
float recenterHeading = 0;
float recenterClock = 0;

// Where the character faces across the ground, or false when it cannot be read.
bool Facing(float& heading)
{
    __try
    {
        const unsigned char* player = static_cast<const unsigned char*>(mainPlayer(*engineSlot));

        if (player == nullptr)
        {
            return false;
        }

        const float* axes = reinterpret_cast<const float*>(player + CharacterAxes);
        const float* forward = axes + ForwardAxis * 3;
        heading = Heading(forward);
        return std::isfinite(heading) && std::fabs(forward[1]) < 0.9f;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

// Where the camera should look across the ground: past the character at the
// locked enemy, a little to one side so the character never hides it, or the
// way the character faces while recentring. False when it should hold still.
bool Wanted(unsigned int id, float& heading, float elapsed)
{
    if (id != 0)
    {
        recentering = false;
        float toward[3]{};

        // Right on top of the player the heading swings wildly, so the camera holds still.
        if (!Bearing(id, toward) || std::hypot(toward[0], toward[2]) < 1)
        {
            return false;
        }

        heading = Heading(toward) - lockOffset;
        return true;
    }

    if (recenterRequested.exchange(false) && Facing(recenterHeading))
    {
        recentering = true;
        recenterClock = 0;
    }

    recenterClock += elapsed;

    // The stick takes over at once, and a swing never lasts more than a second and a half.
    if (yawPerSecond.load() != 0 || recenterClock > 1.5f)
    {
        recentering = false;
    }

    heading = recenterHeading;
    return recentering;
}

void Steer(void* camera, float elapsed)
{
    unsigned int id = lockedId.load();

    if (id != steeringId)
    {
        steeringId = id;
        Say("Lock-on: locked onto %u.", id);
    }

    if (elapsed > 0)
    {
        turnOnItsWay *= std::exp(-elapsed / easing);
    }

    if (!thirdPerson || elapsed <= 0 || targetsInRadius == nullptr)
    {
        return;
    }

    float back[3]{};

    if (!CameraBack(camera, back))
    {
        return;
    }

    // Measured as soon as third person is on, so the first lock turns at once.
    if (yawSign == 0)
    {
        MeasureYaw(camera, back, elapsed);
        return;
    }

    if (yawSign == 2)
    {
        return;
    }

    float wanted = 0;

    if (!Wanted(id, wanted, elapsed))
    {
        return;
    }

    // The camera looks the opposite way to its offset from the character. What
    // has been asked of the game but not yet shown is counted as done, so the
    // view glides onto its mark instead of swinging past it.
    float error = Wrap(wanted - Heading(back) - Pi) - turnOnItsWay;
    float turn = error * std::fmin(1.0f, lockTurn * elapsed);
    float most = 6 * elapsed;
    turn = std::fmax(-most, std::fmin(most, turn));

    if (std::fabs(error) > 0.005f)
    {
        reinterpret_cast<AdjustYaw>(required[2].address)(camera, turn * yawSign);
        turnOnItsWay += turn;
    }
    else
    {
        recentering = false;
    }
}

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

// What the in-game part's own work costs the game each frame, logged every
// half minute as an average and a worst frame, so a dip in the game's frame
// rate can be told apart from anything done here.
double costTotal = 0;
double costWorst = 0;
int costFrames = 0;
float sinceCostReport = 0;

void Account(LONGLONG ticks, float elapsed)
{
    double microseconds = static_cast<double>(ticks) * 1000000.0 / static_cast<double>(ticksPerSecond.QuadPart);
    costTotal += microseconds;
    costWorst = std::fmax(costWorst, microseconds);
    costFrames++;
    sinceCostReport += elapsed;

    if (sinceCostReport < 30)
    {
        return;
    }

    Say("Cost: %.0f us a frame on average, %.0f us at worst, over %d frames.", costTotal / costFrames, costWorst, costFrames);
    costTotal = 0;
    costWorst = 0;
    costFrames = 0;
    sinceCostReport = 0;
}

void Work(void* camera, float elapsed);

// Runs on the game's own thread, once per camera update, after the game's code.
void UpdatedCamera(void* camera)
{
    originalUpdate(camera);
    float elapsed = SecondsSinceLastFrame();
    LARGE_INTEGER started{};
    QueryPerformanceCounter(&started);
    Work(camera, elapsed);
    LARGE_INTEGER ended{};
    QueryPerformanceCounter(&ended);
    Account(ended.QuadPart - started.QuadPart, elapsed);
}

void Work(void* camera, float elapsed)
{
    CheckPanels(elapsed);
    CheckLayouts(elapsed);
    CheckHealth(elapsed);
    CheckTargets(camera, elapsed);

    if (thirdPerson)
    {
        Collide(camera, elapsed);
        Steer(camera, elapsed);
        float yaw = yawPerSecond.load();

        // The stick waits while the camera's turn is measured, which happens once, as third person first turns on.
        if (yaw != 0 && elapsed > 0 && !probing)
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

    if (line.find("\"type\":\"recenter\"") != std::string::npos)
    {
        recenterRequested = true;
    }

    if (line.find("\"type\":\"lock\"") != std::string::npos)
    {
        std::size_t at = line.find("\"id\":");

        if (at == std::string::npos)
        {
            lockedId = 0;
        }
        else
        {
            lockedId = static_cast<unsigned int>(std::strtoul(line.c_str() + at + 5, nullptr, 10));
        }
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
    layoutsWanted = true;
    targetsWanted = true;
    sentHealth = -1;

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
        lockedId = 0;
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
        else
        {
            FindInventoryRoute();
            FindCollision();
            FindHealth();
            FindTargets();
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
