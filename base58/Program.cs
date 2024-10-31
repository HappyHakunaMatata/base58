
using System.Collections;
using System.Numerics;
using base58namespace;

public class Programm
{

    public static void Main(string[] args)
    {
        byte[] encodeValue = new byte[] { 49, 50, 51, 52, 53, 57, 56, 55, 54, 48 };
        var s = base58Token.Encode(encodeValue);
        Console.WriteLine($"{s}");
        var decodeResult = base58Token.Decode("3mJr7AoUXx2Wqd");
        Console.WriteLine($"[{string.Join(", ", decodeResult.Select(b => b.ToString()))}]");
    }
}