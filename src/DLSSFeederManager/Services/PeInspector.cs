using System.Buffers.Binary;

namespace DLSSFeederManager.Services;

public static class PeInspector
{
    public static bool Is64BitExecutable(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[64];

        if (stream.Read(header) != header.Length || header[0] != 'M' || header[1] != 'Z')
            return false;

        var peOffset = BinaryPrimitives.ReadInt32LittleEndian(header[0x3c..0x40]);
        if (peOffset < 64 || peOffset > stream.Length - 6)
            return false;

        stream.Position = peOffset;
        Span<byte> peHeader = stackalloc byte[6];
        if (stream.Read(peHeader) != peHeader.Length)
            return false;

        return peHeader[0] == 'P'
            && peHeader[1] == 'E'
            && peHeader[2] == 0
            && peHeader[3] == 0
            && BinaryPrimitives.ReadUInt16LittleEndian(peHeader[4..6]) == 0x8664;
    }
}
