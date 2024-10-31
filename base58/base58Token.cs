using System;
using System.Collections;
using System.Numerics;
using System.Text;

namespace base58namespace
{
    public static class base58Token
    {
       
        public static readonly string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        public static readonly BigInteger BigRadix10 = BigInteger.Pow(58, 10);
        public static readonly char AlphabetIdx0 = '1';

        public static BigInteger[] bigRadix = new BigInteger[]
        {
            new BigInteger(0),
            new BigInteger(58),
            BigInteger.Pow(58, 2),
            BigInteger.Pow(58, 3),
            BigInteger.Pow(58, 4),
            BigInteger.Pow(58, 5),
            BigInteger.Pow(58, 6),
            BigInteger.Pow(58, 7),
            BigInteger.Pow(58, 8),
            BigInteger.Pow(58, 9),
            BigInteger.Pow(58, 10)
        };

        public static byte[] b58 = new byte[256]
        {
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
        };

        public static string Encode(byte[] bytes)
        {
            BigInteger x = new BigInteger(bytes, isBigEndian: true);
            List<byte> answer = new List<byte>();
            BigInteger mod = new BigInteger();
            while (x.Sign > 0)
            {
                x = BigInteger.DivRem(x, BigRadix10, out mod);
                if (x.Sign == 0)
                {
                    nint m = (nint)mod;
                    while (m > 0)
                    {
                        answer.Add((byte)alphabet[(int)(m % 58)]);
                        m /= 58;
                    }
                }
                else
                {
                    nint m = (nint)mod;
                    for (int i = 0; i < 10; i++)
                    {
                        answer.Add((byte)alphabet[(int)(m % 58)]);
                        m /= 58;
                    }
                }
            }
            foreach (byte b in bytes)
            {
                if (b != 0)
                {
                    break;
                }
                answer.Append((byte)AlphabetIdx0);
            }
            answer.Reverse();
            return (Encoding.UTF8.GetString(answer.ToArray()));
        }

        public static byte[] Decode(string b)
        {
            var answer = BigInteger.Zero;
            BigInteger scratch;
            char[] t = b.ToCharArray();
            while (t.Length > 0)
            {
                int n = t.Length;
                if (n > 10)
                {
                    n = 10;
                }
                nuint total = 0;
                for (var k = 0; k < n; k++)
                {
                    var tmp = b58[t[k]];
                    if (tmp == 255)
                    {
                        
                        return new byte[0];
                    }
                    total = total * 58 + (nuint)tmp;
                }
                answer *= bigRadix[n];
                scratch = new BigInteger(total);
                answer = BigInteger.Add(answer, scratch);
                t = t.Skip(n).ToArray();
            }

            var tmpval = answer.ToByteArray(isBigEndian: true).SkipWhile((value, index) => value == 0 && index == 0).ToArray();
            

            int numZeros = 0;
            while (numZeros < b.Length)
            {
                if (b[numZeros] != AlphabetIdx0)
                {
                    break;
                }
                numZeros += 1;
                
            }
            
            var flen = numZeros + tmpval.Length;
            byte[] val = new byte[flen];
            
            Array.Copy(tmpval, 0, val, numZeros, tmpval.Length);
            return val;
        }

        
    }

}

