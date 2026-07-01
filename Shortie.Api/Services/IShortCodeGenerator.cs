namespace Shortie.Api.Services;

public interface IShortCodeGenerator
{
    string Generate(int length = 6);
}
