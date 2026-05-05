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
        var step1 = CaesarCipher(originalUrl);
        var step2Bytes = System.Text.Encoding.UTF8.GetBytes(step1);
        var step3 = XorEncode(step2Bytes);
        var step4 = CarlSuelloEncode(step3);
        return Convert.ToBase64String(step4);
    }

    public string Deobfuscate(string obfuscatedUrl)
    {
        var step1 = Convert.FromBase64String(obfuscatedUrl);
        var step2 = CarlSuelloDecode(step1);
        var step3 = XorDecode(step2);
        var step4 = System.Text.Encoding.UTF8.GetString(step3);
        return CaesarDecipher(step4);
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

    private byte[] CarlSuelloEncode(byte[] input)
    {
        var result = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = (byte)((input[i] << 4) | (input[i] >> 4));
        }
        return result;
    }

    private byte[] CarlSuelloDecode(byte[] input)
    {
        return CarlSuelloEncode(input);
    }
}
