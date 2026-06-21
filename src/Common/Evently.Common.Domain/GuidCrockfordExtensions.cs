namespace Evently.Common.Domain;

public static class GuidCrockfordExtensions
{
    private const string CrockfordChars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int CodeCharCount = 26;

    /// <summary>
    /// Encodes a Guid (canonical big-endian byte order) to a 26-char Crockford Base32 string.
    /// For UUIDv7 values this preserves lexicographic time ordering.
    /// </summary>
    public static string ToCrockfordBase32(this Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes, bigEndian: true, out int written);

        if (written != 16)
        {
            throw new InvalidOperationException("Guid byte length must be 16.");
        }

        return string.Create(CodeCharCount, bytes, static (chars, src) =>
        {
            int buffer = 0;
            int bitCount = 0;
            int outIndex = 0;

            for (int i = 0; i < src.Length; i++)
            {
                buffer = (buffer << 8) | src[i];
                bitCount += 8;

                while (bitCount >= 5)
                {
                    bitCount -= 5;
                    chars[outIndex++] = CrockfordChars[(buffer >> bitCount) & 31];
                }
            }

            if (bitCount > 0)
            {
                chars[outIndex++] = CrockfordChars[(buffer << (5 - bitCount)) & 31];
            }

            if (outIndex != CodeCharCount)
            {
                throw new InvalidOperationException("Crockford output length must be 26.");
            }
        });
    }

    /// <summary>
    /// Decodes a 26-char Crockford Base32 code into Guid.
    /// </summary>
    public static Guid FromCrockfordBase32(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return FromCrockfordBase32(code.AsSpan());
    }

    public static Guid FromCrockfordBase32(ReadOnlySpan<char> code)
    {
        if (code.Length != CodeCharCount)
        {
            throw new FormatException("Kod 26 karakter olmalı.");
        }

        // Canonical ULID/Crockford representation for 128-bit payload requires first symbol <= 7
        int firstValue = Decode(code[0]);
        if (firstValue > 7)
        {
            throw new FormatException("İlk karakter 128-bit Crockford formatı için 0..7 aralığında olmalı.");
        }

        Span<byte> bytes = stackalloc byte[16];
        int buffer = firstValue;
        int bitCount = 5;
        int byteIndex = 0;

        for (int i = 1; i < code.Length; i++)
        {
            int value = Decode(code[i]);
            buffer = (buffer << 5) | value;
            bitCount += 5;

            while (bitCount >= 8)
            {
                bitCount -= 8;
                if (byteIndex >= bytes.Length)
                {
                    throw new FormatException("Geçersiz Crockford Base32 uzunluğu.");
                }

                bytes[byteIndex++] = (byte)(buffer >> bitCount);
            }
        }

        if (byteIndex != 16)
        {
            throw new FormatException("Geçersiz Crockford Base32 içeriği.");
        }

        // 130-bit stream carries 2 padding bits for 128-bit Guid; they must be zero.
        if (bitCount != 2 || (buffer & ((1 << bitCount) - 1)) != 0)
        {
            throw new FormatException("Geçersiz Crockford Base32 dolgu bitleri.");
        }

        return new Guid(bytes, bigEndian: true);
    }

    private static int Decode(char c)
    {
        if ((uint)(c - '0') <= 9)
        {
            return c - '0';
        }

        char upper = char.ToUpperInvariant(c);

        // Crockford aliases
        if (upper == 'O')
        {
            return 0;
        }

        if (upper == 'I' || upper == 'L')
        {
            return 1;
        }

        return upper switch
        {
            'A' => 10,
            'B' => 11,
            'C' => 12,
            'D' => 13,
            'E' => 14,
            'F' => 15,
            'G' => 16,
            'H' => 17,
            'J' => 18,
            'K' => 19,
            'M' => 20,
            'N' => 21,
            'P' => 22,
            'Q' => 23,
            'R' => 24,
            'S' => 25,
            'T' => 26,
            'V' => 27,
            'W' => 28,
            'X' => 29,
            'Y' => 30,
            'Z' => 31,
            _ => throw new FormatException($"Geçersiz karakter: '{c}'")
        };
    }
}
