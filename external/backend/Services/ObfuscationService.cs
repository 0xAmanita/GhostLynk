namespace backend_deob.Services;

public class ObfuscationService : IObfuscationService
{
    private readonly int _caesarShift;
    private readonly byte[] _xorKey;

    public ObfuscationService(IConfiguration configuration)
    {
        _caesarShift = configuration.GetValue<int>("Obfuscation:CaesarShift");
        var xorKeyString = configuration.GetValue<string>("Obfuscation:XorKey") ?? throw new InvalidOperationException("XorKey not configured");
        _xorKey = System.Text.Encoding.UTF8.GetBytes(xorKeyString);
    }

    public string Obfuscate(string originalUrl)
    {
        throw new NotImplementedException();
    }

    public string Deobfuscate(string obfuscatedUrl)
    {
        throw new NotImplementedException();
    }

    private string CaesarCipher(string input)
    {
        var result = new char[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = (char)(input[i] + _caesarShift);
        }
        return new string(result);
    }

    private string CaesarDecipher(string input)
    {
        var result = new char[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = (char)(input[i] - _caesarShift);
        }
        return new string(result);
    }

    private byte[] XorEncode(byte[] input)
    {
        var result = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = (byte)(input[i] ^ _xorKey[i % _xorKey.Length]);
        }
        return result;
    }

    private byte[] XorDecode(byte[] input)
    {
        return XorEncode(input);
    }
}
