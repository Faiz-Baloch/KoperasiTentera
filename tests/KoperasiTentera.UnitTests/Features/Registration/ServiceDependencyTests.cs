using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Validators.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class ServiceDependencyTests
{
    [Fact]
    public void RegistrationTestContext_CreatesRegistrationService()
    {
        using var ctx = new RegistrationTestContext();
        Assert.NotNull(ctx.Service);
    }

    [Fact]
    public async Task ChangeEmailValidator_ResolvesAndValidates()
    {
        var validator = new ChangeEmailRequestValidator();
        var result = await validator.ValidateAsync(new ChangeEmailRequest(Guid.NewGuid(), "new@email.com"));
        Assert.True(result.IsValid);
    }
}
