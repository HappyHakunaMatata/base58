using System.Numerics;
using System.Text;
using Base58namespace;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class BenchProgram
{
    // dotnet run -c Release --project BenchTest -- --filter *
    // add "--job short" for a quick (less precise) run.
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(BenchProgram).Assembly).Run(args);
}

[MemoryDiagnoser]
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
    }

    [Benchmark]
    public string Encode() => Base58Token.Encode(_bytes);

    [Benchmark]
    public byte[] Decode() => Base58Token.Decode(_text);
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
                    answer.Add((byte)Base58Token.alphabet[(int)(m % 58)]);
                    m /= 58;
                }
            }
            else
            {
                nint m = (nint)mod;
                for (int i = 0; i < 10; i++)
                {
                    answer.Add((byte)Base58Token.alphabet[(int)(m % 58)]);
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
                    answer.AddFirst((byte)Base58Token.alphabet[(int)(m % 58)]);
                    m /= 58;
                }
            }
            else
            {
                nint m = (nint)mod;
                for (int i = 0; i < 10; i++)
                {
                    answer.AddFirst((byte)Base58Token.alphabet[(int)(m % 58)]);
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
