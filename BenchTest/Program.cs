using base58namespace;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class Programm
{

    public static void Main(string[] args)
    {
        List<byte[]> datab = new();
        FullFill(datab);
        List<string> datas = new() { };
        FullFill(datas);
        var result = Timeit(Base58Encode, datab);
        Console.WriteLine($"Base 58 encoding: {result}");
        result = Timeit(Base64Encode, datab);
        Console.WriteLine($"Base 64 encoding {result}");
        result = Timeit(Base58Decode, datas);
        Console.WriteLine($"Base 58 decoding {result}");
        result = Timeit(Base64Decode, datas);
        Console.WriteLine($"Base 64 decoding {result}");
    }

    public static void FullFill(List<byte[]> datab)
    {
        Random random = new Random();
        for (int i = 0; i < 130; i++)
        {
            byte[] byteArray = new byte[random.Next(5, 10)];
            random.NextBytes(byteArray);
            datab.Add(byteArray);
        }
    }

    public static void FullFill(List<string> data)
    {
        Random random = new Random();
        for (int i = 0; i < 130; i++)
        {
            byte[] byteArray = new byte[random.Next(5, 10)];
            random.NextBytes(byteArray);
            string base64String = Convert.ToBase64String(byteArray);
            data.Add(base64String);
        }
    }

    public static double Timeit<T>(Action<T> func, T args)
    {
        var watch = Stopwatch.StartNew();
        func(args);
        watch.Stop();
        return watch.Elapsed.TotalNanoseconds;
    }

    public static void Base58Encode(List<byte[]> values)
    {
        foreach (var i in values)
        {
            base58Token.Encode(i);
        }
    }

    public static void Base64Encode(List<byte[]> values)
    {
        foreach (var i in values)
        {
            Convert.ToBase64String(i);
        }
    }

    public static void Base58Decode(List<string> values)
    {
        foreach (var i in values)
        {
            base58Token.Decode(i);
        }
    }

    public static void Base64Decode(List<string> values)
    {
        foreach (var i in values)
        {
            Convert.FromBase64String(i);
        }
    }
}