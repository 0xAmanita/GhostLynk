namespace backend_deob.Services;

public interface IObfuscationService
{
    string Obfuscate(string originalUrl);
    string Deobfuscate(string obfuscatedUrl);
}
