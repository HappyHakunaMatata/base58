using System.Numerics;

namespace Base58namespace;

public static class Base58Token
{
    public static ReadOnlySpan<char> Alphabet => "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    public static readonly BigInteger BigRadix10 = new(430804206899405824);
    public static readonly char AlphabetIdx0 = '1';

   
    private const uint Radix5 = 656356768;

    private static ReadOnlySpan<uint> Radix => [1, 58, 3364, 195112, 11316496, 656356768];

    private const int MaxStackLimbs = 160;
    private const int MaxStackChars = 768;
   
    private static ReadOnlySpan<byte> B58 =>
    [
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 0, 1, 2, 3, 4, 5, 6,
        7, 8, 255, 255, 255, 255, 255, 255,
        255, 9, 10, 11, 12, 13, 14, 15,
        16, 255, 17, 18, 19, 20, 21, 255,
        22, 23, 24, 25, 26, 27, 28, 29,
        30, 31, 32, 255, 255, 255, 255, 255,
        255, 33, 34, 35, 36, 37, 38, 39,
        40, 41, 42, 43, 255, 44, 45, 46,
        47, 48, 49, 50, 51, 52, 53, 54,
        55, 56, 57, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255,
    ];

    public static string Encode(byte[] bytes)
    {
        int zeros = 0;
        while (zeros < bytes.Length && bytes[zeros] == 0) zeros++;
        ReadOnlySpan<byte> body = bytes.AsSpan(zeros);

        int limbCount = (body.Length + 3) / 4;
        Span<uint> limbs = limbCount <= MaxStackLimbs ? stackalloc uint[limbCount] : new uint[limbCount];
        for (int li = limbCount - 1, bi = body.Length; li >= 0; li--)
        {
            int take = Math.Min(4, bi);
            uint v = 0;
            for (int k = bi - take; k < bi; k++) v = (v << 8) | body[k];
            limbs[li] = v;
            bi -= take;
        }

        int size = bytes.Length * 138 / 100 + 1;
        Span<char> answer = size <= MaxStackChars ? stackalloc char[size] : new char[size];
        int pos = size;

       
        int first = 0;
        while (first < limbCount)
        {
            ulong rem = 0;
            for (int i = first; i < limbCount; i++)
            {
                ulong cur = (rem << 32) | limbs[i];
                limbs[i] = (uint)(cur / Radix5);
                rem = cur % Radix5;
            }
            while (first < limbCount && limbs[first] == 0) first++;

            uint m = (uint)rem;
            if (first == limbCount)
            {
                while (m > 0)
                {
                    answer[--pos] = Alphabet[(int)(m % 58)];
                    m /= 58;
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    answer[--pos] = Alphabet[(int)(m % 58)];
                    m /= 58;
                }
            }
        }

        for (int i = 0; i < zeros; i++) answer[--pos] = AlphabetIdx0;
        return new string(answer[pos..]);
    }

    public static byte[] Decode(string b)
    {
        int numZeros = 0;
        while (numZeros < b.Length && b[numZeros] == AlphabetIdx0) numZeros++;
        ReadOnlySpan<char> body = b.AsSpan(numZeros);

        int maxBytes = body.Length * 733 / 1000 + 1;
        int limbCap = (maxBytes + 3) / 4;
       
        Span<uint> limbs = limbCap <= MaxStackLimbs ? stackalloc uint[limbCap] : new uint[limbCap];
        int used = 0;

        while (body.Length > 0)
        {
            int n = Math.Min(5, body.Length);
            uint total = 0;
            for (int k = 0; k < n; k++)
            {
                char c = body[k];
                byte digit = c < 256 ? B58[c] : (byte)255;
                if (digit == 255) return [];
                total = total * 58 + digit;
            }

           
            ulong carry = total;
            uint radix = Radix[n];
            for (int i = 0; i < used; i++)
            {
                ulong cur = (ulong)limbs[i] * radix + carry;
                limbs[i] = (uint)cur;
                carry = cur >> 32;
            }
            if (carry > 0) limbs[used++] = (uint)carry;

            body = body[n..];
        }

        int byteCount = 0;
        if (used > 0)
        {
            uint top = limbs[used - 1];
            int topBytes = 4;
            while (topBytes > 1 && (top >> ((topBytes - 1) * 8)) == 0) topBytes--;
            byteCount = (used - 1) * 4 + topBytes;
        }

        byte[] val = new byte[numZeros + byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            val[val.Length - 1 - i] = (byte)(limbs[i >> 2] >> ((i & 3) * 8));
        }
        return val;
    }
}
