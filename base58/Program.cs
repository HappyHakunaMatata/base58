
using System.Collections;
using System.Numerics;
using base58namespace;

public class Programm
{

    public static void Main(string[] args)
    {
        base58Token token = new();
        byte[] encodeValue = new byte[] { 49, 50, 51, 52, 53, 57, 56, 55, 54, 48 };
        var s = token.Encode(encodeValue);
        Console.WriteLine($"{s}");
        var decodeResult = token.Decode("3mJr7AoUXx2Wqd");
        Console.WriteLine($"[{string.Join(", ", decodeResult.Select(b => b.ToString()))}]");
    }

    
   
}