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
}
