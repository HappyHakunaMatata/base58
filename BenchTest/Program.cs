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

// Base58Token.Encode (stack buffer of chars, filled back to front) against the two
// collection-based shapes it replaced.
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
            if (current != list || current != linked)
            {
                throw new InvalidOperationException(
                    $"variant mismatch: {current} / {list} / {linked}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public string Current() => Base58Token.Encode(_bytes);

    [Benchmark]
    public string ListReverse() => EncodeListReverse(_bytes);

    [Benchmark]
    public string LinkedList() => EncodeLinkedList(_bytes);

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
