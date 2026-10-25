using System.Buffers.Binary;
using System.Text;
using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>A minimal 64-bit DLL image built by hand: one section holding an export table with two names.</summary>
public sealed class ExportTableTests
{
    private const string Camera = "?SetCameraYaw@WorldCamera@GAME@@UEAAXM@Z";

    [Fact]
    public void ExportedNamesAndTheirAddressesAreRead()
    {
        ExportTable table = ExportTable.Read(Image());

        Assert.Equal(2, table.Names.Count);
        Assert.Equal(0x1100u, table.Names["Alpha"]);
        Assert.Equal(0x1104u, table.Names[Camera]);
    }

    [Fact]
    public void AFunctionsFirstBytesCanBeChecked()
    {
        ExportTable table = ExportTable.Read(Image());

        Assert.Equal(new byte[] { 0xF3, 0x0F, 0x11, 0x49, 0x0C, 0xC3 }, table.BytesAt(table.Names[Camera], 6));
    }

    [Fact]
    public void SomethingThatIsNotADllIsRefused()
    {
        Assert.Throws<FormatException>(() => ExportTable.Read(new byte[0x200]));
    }

    private static byte[] Image()
    {
        byte[] image = new byte[0x400];
        Put(image, 0x3C, 0x40);
        Put(image, 0x40, 0x00004550);
        PutShort(image, 0x44, 0x8664);
        PutShort(image, 0x46, 1);
        PutShort(image, 0x54, 240);
        PutShort(image, 0x58, 0x20B);

        // The export directory entry, then the one section mapping address 0x1000 to file offset 0x200.
        Put(image, 0x58 + 112, 0x1000);
        Put(image, 0x58 + 116, 0x100);
        Put(image, 0x148 + 8, 0x200);
        Put(image, 0x148 + 12, 0x1000);
        Put(image, 0x148 + 16, 0x200);
        Put(image, 0x148 + 20, 0x200);

        Put(image, 0x200 + 20, 2);
        Put(image, 0x200 + 24, 2);
        Put(image, 0x200 + 28, 0x1040);
        Put(image, 0x200 + 32, 0x1050);
        Put(image, 0x200 + 36, 0x1060);
        Put(image, 0x240, 0x1100);
        Put(image, 0x244, 0x1104);
        Put(image, 0x250, 0x1080);
        Put(image, 0x254, 0x10A0);
        PutShort(image, 0x260, 0);
        PutShort(image, 0x262, 1);
        Encoding.ASCII.GetBytes("Alpha").CopyTo(image, 0x280);
        Encoding.ASCII.GetBytes(Camera).CopyTo(image, 0x2A0);
        new byte[] { 0xF3, 0x0F, 0x11, 0x49, 0x0C, 0xC3 }.CopyTo(image, 0x304);
        return image;
    }

    private static void Put(byte[] image, int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(offset, 4), value);
    }

    private static void PutShort(byte[] image, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset, 2), (ushort)value);
    }
}
