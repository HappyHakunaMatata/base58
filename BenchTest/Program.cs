using base58namespace;
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
        _text = base58Token.Encode(_bytes);

        // A round trip has to give the input back, otherwise the timings measure broken work.
        if (!base58Token.Decode(_text).SequenceEqual(_bytes))
        {
            throw new InvalidOperationException($"round trip mismatch for {_text}");
        }
    }

    [Benchmark]
    public string Encode() => base58Token.Encode(_bytes);

    [Benchmark]
    public byte[] Decode() => base58Token.Decode(_text);
}
