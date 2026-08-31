using System.Numerics;
using System.Text;
using Base58namespace;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

public class BenchProgram
{
    // dotnet run -c Release --project BenchTest -- --filter *
    // add "--job short" for a quick (less precise) run.
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(BenchProgram).Assembly).Run(args);
}

// Base58Token against SimpleBase (https://github.com/ssg/SimpleBase), whose Base58.Bitcoin
// uses the same alphabet and the same leading-zero rule, so the two are directly comparable.
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Base58Benchmarks
{
    [Params(8, 32, 128)]
    public int Size;

    private byte[] _bytes = Array.Empty<byte>();
    private string _text = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        Random random = new Random(42);
        _bytes = new byte[Size];
        random.NextBytes(_bytes);
        _text = Base58Token.Encode(_bytes);

        // A round trip has to give the input back, otherwise the timings measure broken work.
        if (!Base58Token.Decode(_text).SequenceEqual(_bytes))
        {
            throw new InvalidOperationException($"round trip mismatch for {_text}");
        }

        // The two implementations must agree before their timings mean anything. Leading zero
        // bytes get their own check because that is where base58 implementations disagree.
        for (int zeros = 0; zeros <= Math.Min(4, Size); zeros++)
        {
            byte[] probe = (byte[])_bytes.Clone();
            for (int i = 0; i < zeros; i++)
            {
                probe[i] = 0;
            }

            string mine = Base58Token.Encode(probe);
            string theirs = SimpleBase.Base58.Bitcoin.Encode(probe);
            if (mine != theirs)
            {
                throw new InvalidOperationException($"encode mismatch: {mine} / {theirs}");
            }

            if (!Base58Token.Decode(mine).SequenceEqual(SimpleBase.Base58.Bitcoin.Decode(mine)))
            {
                throw new InvalidOperationException($"decode mismatch for {mine}");
            }
        }
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Encode")]
    public string Encode() => Base58Token.Encode(_bytes);

    [Benchmark, BenchmarkCategory("Encode")]
    public string EncodeSimpleBase() => SimpleBase.Base58.Bitcoin.Encode(_bytes);

    [Benchmark(Baseline = true), BenchmarkCategory("Decode")]
    public byte[] Decode() => Base58Token.Decode(_text);

    [Benchmark, BenchmarkCategory("Decode")]
    public byte[] DecodeSimpleBase() => SimpleBase.Base58.Bitcoin.Decode(_text);
}

// Base58Token.Encode (in-place division over uint limbs on the stack) against the three
// shapes it replaced, oldest last.
[MemoryDiagnoser]
public class EncodeVariants
{
    [Params(8, 32, 128, 512)]
    public int Size;

    private byte[] _bytes = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        Random random = new Random(42);
        _bytes = new byte[Size];
        random.NextBytes(_bytes);

        // Every variant must produce the same string, including the leading-zero cases
        // that only show up when the input starts with zero bytes.
        for (int zeros = 0; zeros <= Math.Min(4, Size); zeros++)
        {
            byte[] probe = (byte[])_bytes.Clone();
            for (int i = 0; i < zeros; i++)
            {
                probe[i] = 0;
            }
            string current = Base58Token.Encode(probe);
            string list = EncodeListReverse(probe);
            string linked = EncodeLinkedList(probe);
            string big = EncodeBigIntegerChunks(probe);
            if (current != list || current != linked || current != big)
            {
                throw new InvalidOperationException(
                    $"variant mismatch: {current} / {list} / {linked} / {big}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public string Current() => Base58Token.Encode(_bytes);

    [Benchmark]
    public string ListReverse() => EncodeListReverse(_bytes);

    [Benchmark]
    public string LinkedList() => EncodeLinkedList(_bytes);

    [Benchmark]
    public string BigIntegerChunks() => EncodeBigIntegerChunks(_bytes);

    // The version before Current: same 58^k chunking and the same stack char buffer, but
    // the arithmetic ran on BigInteger, so each DivRem allocated a fresh quotient.
    public static string EncodeBigIntegerChunks(byte[] bytes)
    {
        BigInteger x = new(bytes, isUnsigned: true, isBigEndian: true);
        int size = bytes.Length * 138 / 100 + 1;
        Span<char> answer = size <= 256 ? stackalloc char[size] : new char[size];
        int pos = size;

        while (x.Sign > 0)
        {
            x = BigInteger.DivRem(x, Base58Token.BigRadix10, out BigInteger mod);
            if (x.Sign == 0)
            {
                nint m = (nint)mod;
                while (m > 0)
                {
                    answer[--pos] = Base58Token.Alphabet[(int)(m % 58)];
                    m /= 58;
                }
            }
            else
            {
                nint m = (nint)mod;
                for (int i = 0; i < 10; i++)
                {
                    answer[--pos] = Base58Token.Alphabet[(int)(m % 58)];
                    m /= 58;
                }
            }
        }
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0) break;
            answer[--pos] = Base58Token.AlphabetIdx0;
        }
        return new string(answer[pos..]);
    }

    // What Encode looked like before: append to a List<byte>, reverse, decode as UTF8.
    public static string EncodeListReverse(byte[] bytes)
    {
        BigInteger x = new(bytes, isUnsigned: true, isBigEndian: true);
        List<byte> answer = [];
        BigInteger mod = new();
        while (x.Sign > 0)
        {
            x = BigInteger.DivRem(x, Base58Token.BigRadix10, out mod);
            if (x.Sign == 0)
            {
                nint m = (nint)mod;
                while (m > 0)
                {
                    answer.Add((byte)Base58Token.Alphabet[(int)(m % 58)]);
                    m /= 58;
                }
            }
            else
            {
                nint m = (nint)mod;
                for (int i = 0; i < 10; i++)
                {
                    answer.Add((byte)Base58Token.Alphabet[(int)(m % 58)]);
                    m /= 58;
                }
            }
        }
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0)
            {
                break;
            }
            answer.Add((byte)Base58Token.AlphabetIdx0);
        }
        answer.Reverse();
        return Encoding.UTF8.GetString(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(answer));
    }

    // AddFirst puts digits straight into the right order, so no Reverse() is needed.
    // Still one heap node per digit, and GetString needs a contiguous copy anyway.
    public static string EncodeLinkedList(byte[] bytes)
    {
        BigInteger x = new(bytes, isUnsigned: true, isBigEndian: true);
        LinkedList<byte> answer = new();
        BigInteger mod = new();
        while (x.Sign > 0)
        {
            x = BigInteger.DivRem(x, Base58Token.BigRadix10, out mod);
            if (x.Sign == 0)
            {
                nint m = (nint)mod;
                while (m > 0)
                {
                    answer.AddFirst((byte)Base58Token.Alphabet[(int)(m % 58)]);
                    m /= 58;
                }
            }
            else
            {
                nint m = (nint)mod;
                for (int i = 0; i < 10; i++)
                {
                    answer.AddFirst((byte)Base58Token.Alphabet[(int)(m % 58)]);
                    m /= 58;
                }
            }
        }
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0)
            {
                break;
            }
            answer.AddFirst((byte)Base58Token.AlphabetIdx0);
        }
        byte[] flat = new byte[answer.Count];
        answer.CopyTo(flat, 0);
        return Encoding.UTF8.GetString(flat);
    }
}

// Base58Token.Decode (in-place multiply-and-add over uint limbs) against the BigInteger
// version it replaced, which allocated a new accumulator per 10-character chunk.
[MemoryDiagnoser]
public class DecodeVariants
{
    [Params(8, 32, 128, 512)]
    public int Size;

    private string _text = string.Empty;

    private static readonly BigInteger[] bigRadix =
    [
        0,
        58,
        3364,
        195112,
        11316496,
        656356768,
        38068692544,
        2207984167552,
        128063081718016,
        7427658739644928,
        430804206899405824
    ];

    // Same 256-entry char -> digit map the old Decode used, built from the alphabet so the
    // benchmark does not have to carry a second copy of the table.
    private static readonly byte[] B58 = BuildLookup();

    private static byte[] BuildLookup()
    {
        byte[] table = new byte[256];
        table.AsSpan().Fill(255);
        for (int i = 0; i < 58; i++)
        {
            table[Base58Token.Alphabet[i]] = (byte)i;
        }
        return table;
    }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new Random(42);
        byte[] bytes = new byte[Size];
        random.NextBytes(bytes);
        _text = Base58Token.Encode(bytes);

        // Both variants have to return the input, and to agree with each other, including
        // on the leading-zero inputs that turn into leading '1' characters.
        for (int zeros = 0; zeros <= Math.Min(4, Size); zeros++)
        {
            byte[] probe = (byte[])bytes.Clone();
            for (int i = 0; i < zeros; i++)
            {
                probe[i] = 0;
            }
            string text = Base58Token.Encode(probe);
            if (!Base58Token.Decode(text).SequenceEqual(probe) ||
                !DecodeBigIntegerChunks(text).SequenceEqual(probe))
            {
                throw new InvalidOperationException($"variant mismatch for {text}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public byte[] Current() => Base58Token.Decode(_text);

    [Benchmark]
    public byte[] BigIntegerChunks() => DecodeBigIntegerChunks(_text);

    // What Decode looked like before: accumulate into a BigInteger, ten characters at a
    // time. Every "answer *= radix" and every Add builds a new limb array.
    public static byte[] DecodeBigIntegerChunks(string b)
    {
        var answer = BigInteger.Zero;
        ReadOnlySpan<char> t = b.AsSpan();
        while (t.Length > 0)
        {
            int n = t.Length;
            if (n > 10) n = 10;

            nuint total = 0;
            for (var k = 0; k < n; k++)
            {
                var tmp = B58[t[k]];
                if (tmp == 255) return [];
                total = total * 58 + (nuint)tmp;
            }
            answer *= bigRadix[n];
            answer = BigInteger.Add(answer, new BigInteger(total));
            t = t[n..];
        }

        int byteCount = answer.IsZero ? 0 : answer.GetByteCount(isUnsigned: true);
        Span<byte> tmpval = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
        if (byteCount > 0) answer.TryWriteBytes(tmpval, out _, isUnsigned: true, isBigEndian: true);

        int numZeros = 0;
        while (numZeros < b.Length)
        {
            if (b[numZeros] != Base58Token.AlphabetIdx0) break;
            numZeros += 1;
        }

        byte[] val = new byte[numZeros + tmpval.Length];
        tmpval.CopyTo(val.AsSpan(numZeros));
        return val;
    }
}
