using Shortie.Api.Services;
using Shortie.Api.Validators;
using Shortie.Api.DTOs.Urls;

namespace Shortie.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void Generate_ShouldCreateExpectedLengthAndCharset()
    {
        var generator = new ShortCodeGenerator();

        var code = generator.Generate(8);

        Assert.Equal(8, code.Length);
        Assert.Matches("^[a-zA-Z0-9]+$", code);
    }

    [Fact]
    public void CreateShortUrlValidator_ShouldFail_ForInvalidUrl()
    {
        var validator = new CreateShortUrlRequestValidator();
        var request = new CreateShortUrlRequestDto("not-a-url", null, null);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateShortUrlRequestDto.OriginalUrl));
    }
}
