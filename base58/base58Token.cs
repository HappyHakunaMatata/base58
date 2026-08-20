using System.Numerics;

namespace base58namespace
{
    public static class base58Token
    {
        public static readonly string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        public static readonly BigInteger BigRadix10 = new(430804206899405824);
        public static readonly char AlphabetIdx0 = '1';

        private static readonly BigInteger[] bigRadix =
        [
            0,
            58,
            3364,              //BigInteger.Pow(58, 2)
            195112,            //BigInteger.Pow(58, 3)
            11316496,          //BigInteger.Pow(58, 4)
            656356768,         //BigInteger.Pow(58, 5)
            38068692544,       //BigInteger.Pow(58, 6)
            2207984167552,     //BigInteger.Pow(58, 7)
            128063081718016,   //BigInteger.Pow(58, 8)
            7427658739644928,  //BigInteger.Pow(58, 9)
            430804206899405824 //BigInteger.Pow(58, 10)
        ];

        private static readonly byte[] b58 =
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
            BigInteger x = new(bytes, isUnsigned: true, isBigEndian: true);
            int size = bytes.Length * 138 / 100 + 1;
            Span<char> answer = size <= 256 ? stackalloc char[size] : new char[size];
            int pos = size;

            while (x.Sign > 0)
            {
                x = BigInteger.DivRem(x, BigRadix10, out BigInteger mod);
                if (x.Sign == 0)
                {
                    nint m = (nint)mod;
                    while (m > 0)
                    {
                        answer[--pos] = alphabet[(int)(m % 58)];
                        m /= 58;
                    }
                }
                else
                {
                    nint m = (nint)mod;
                    for (int i = 0; i < 10; i++)
                    {
                        answer[--pos] = alphabet[(int)(m % 58)];
                        m /= 58;
                    }
                }
            }
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0) break;
                answer[--pos] = AlphabetIdx0;
            }
            return new string(answer[pos..]);
        }

        public static byte[] Decode(string b)
        {
            var answer = BigInteger.Zero;
            BigInteger scratch;
            ReadOnlySpan<char> t = b.AsSpan();
            while (t.Length > 0)
            {
                int n = t.Length;
                if (n > 10) n = 10;
               
                nuint total = 0;
                for (var k = 0; k < n; k++)
                {
                    var tmp = b58[t[k]];
                    if (tmp == 255) return [];
                    total = total * 58 + (nuint)tmp;
                }
                answer *= bigRadix[n];
                scratch = new BigInteger(total);
                answer = BigInteger.Add(answer, scratch);
                t = t[n..];
            }

            
            int byteCount = answer.IsZero ? 0 : answer.GetByteCount(isUnsigned: true);
            Span<byte> tmpval = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
            if (byteCount > 0) answer.TryWriteBytes(tmpval, out _, isUnsigned: true, isBigEndian: true);
           

            int numZeros = 0;
            while (numZeros < b.Length)
            {
                if (b[numZeros] != AlphabetIdx0) break;
                numZeros += 1;
            }

            byte[] val = new byte[numZeros + tmpval.Length];
            tmpval.CopyTo(val.AsSpan(numZeros));
            return val;
        }
    }

}

