using KoperasiTentera.Service.Services.Registration;

namespace KoperasiTentera.UnitTests.Security;

public class PinSecurityTests
{
    [Fact]
    public void Hash_IsNotEqualToPlainPin()
    {
        var hash = PinHasher.Hash("135790");
        Assert.NotEqual("135790", hash);
    }

    [Fact]
    public void Hash_ContainsSaltAndIterationMetadata()
    {
        var hash = PinHasher.Hash("135790");
        var parts = hash.Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal("100000", parts[0]);
        Assert.NotEmpty(parts[1]);
        Assert.NotEmpty(parts[2]);
    }

    [Fact]
    public void Verify_ReturnsTrueForOriginalPin()
    {
        var hash = PinHasher.Hash("135790");
        Assert.True(PinHasher.Verify("135790", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForDifferentPin()
    {
        var hash = PinHasher.Hash("135790");
        Assert.False(PinHasher.Verify("135791", hash));
    }

    [Fact]
    public void HashingSamePinProducesDifferentHashesBecauseOfRandomSalt()
    {
        var first = PinHasher.Hash("135790");
        var second = PinHasher.Hash("135790");

        Assert.NotEqual(first, second);
        Assert.True(PinHasher.Verify("135790", first));
        Assert.True(PinHasher.Verify("135790", second));
    }
}
