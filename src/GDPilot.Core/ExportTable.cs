using System.Buffers.Binary;
using System.Text;

namespace GDPilot.Core;

/// <summary>
/// The functions a Windows DLL exposes by name, read straight from its bytes.
/// Grim Dawn's own DLLs name their camera, inventory and UI functions this way,
/// which is what lets the in-game part find them again after a patch moves them.
/// </summary>
public sealed class ExportTable
{
    private readonly byte[] image;
    private readonly List<(uint Address, uint Size, uint FileOffset)> sections;

    private ExportTable(byte[] image, List<(uint, uint, uint)> sections, Dictionary<string, uint> names)
    {
        this.image = image;
        this.sections = sections;
        Names = names;
    }

    /// <summary>Every exported name and the address it lives at, relative to where the DLL is loaded.</summary>
    public IReadOnlyDictionary<string, uint> Names { get; }

    public static ExportTable Read(byte[] image)
    {
        int header = Int(image, 0x3C);

        if (image.Length < header + 24 || Int(image, header) != 0x00004550)
        {
            throw new FormatException("Not a Windows DLL or program.");
        }

        int sectionCount = Short(image, header + 6);
        int optionalSize = Short(image, header + 20);
        int optional = header + 24;

        // The export directory is the first data directory, 112 bytes into a 64-bit header, 96 into a 32-bit one.
        int directories = optional + 96;

        if (Short(image, optional) == 0x20B)
        {
            directories = optional + 112;
        }

        List<(uint, uint, uint)> sections = new();

        for (int index = 0; index < sectionCount; index++)
        {
            int entry = optional + optionalSize + index * 40;
            sections.Add(((uint)Int(image, entry + 12), (uint)Math.Max(Int(image, entry + 8), Int(image, entry + 16)), (uint)Int(image, entry + 20)));
        }

        ExportTable table = new(image, sections, new Dictionary<string, uint>());
        uint exports = (uint)Int(image, directories);

        if (exports == 0)
        {
            return table;
        }

        int directory = table.Offset(exports);
        int nameCount = Int(image, directory + 24);
        int functions = table.Offset((uint)Int(image, directory + 28));
        int names = table.Offset((uint)Int(image, directory + 32));
        int ordinals = table.Offset((uint)Int(image, directory + 36));
        Dictionary<string, uint> found = new(nameCount);

        for (int index = 0; index < nameCount; index++)
        {
            string name = table.Text((uint)Int(image, names + index * 4));
            int ordinal = Short(image, ordinals + index * 2);
            found[name] = (uint)Int(image, functions + ordinal * 4);
        }

        return new ExportTable(image, sections, found);
    }

    /// <summary>The first bytes of the code at an address, to check a function still starts the way it is expected to.</summary>
    public byte[] BytesAt(uint address, int count)
    {
        return image.AsSpan(Offset(address), count).ToArray();
    }

    private int Offset(uint address)
    {
        foreach ((uint start, uint size, uint fileOffset) in sections)
        {
            if (address >= start && address < start + size)
            {
                return (int)(address - start + fileOffset);
            }
        }

        throw new FormatException($"Address 0x{address:X} is in no section of the file.");
    }

    private string Text(uint address)
    {
        int start = Offset(address);
        int end = Array.IndexOf(image, (byte)0, start);
        return Encoding.ASCII.GetString(image, start, end - start);
    }

    private static int Int(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, 4));
    }

    private static int Short(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
    }
}
