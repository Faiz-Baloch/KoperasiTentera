using FluentValidation;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Validators.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class ServiceDependencyTests
{
    [Fact]
    public void RegistrationServiceTestContext_ResolvesEveryValidatorRequiredByService()
    {
        using var ctx = new RegistrationTestContext();

        Assert.IsType<CheckIcRequestValidator>(new CheckIcRequestValidator());
        Assert.IsType<StartRegistrationRequestValidator>(new StartRegistrationRequestValidator());
        Assert.IsType<SendOtpRequestValidator>(new SendOtpRequestValidator());
        Assert.IsType<VerifyOtpRequestValidator>(new VerifyOtpRequestValidator());
        Assert.IsType<ChangeEmailRequestValidator>(new ChangeEmailRequestValidator());
        Assert.IsType<AcceptPrivacyPolicyRequestValidator>(new AcceptPrivacyPolicyRequestValidator());
        Assert.IsType<SetPinRequestValidator>(new SetPinRequestValidator());
        Assert.NotNull(ctx.Service);
    }

    [Fact]
    public async Task ChangeEmailValidator_IsResolvableAndValidatesThroughFluentValidation()
    {
        IValidator<ChangeEmailRequest> validator = new ChangeEmailRequestValidator();

        var valid = await validator.ValidateAsync(
            new ChangeEmailRequest(Guid.NewGuid(), "new@email.com"));

        var invalid = await validator.ValidateAsync(
            new ChangeEmailRequest(Guid.Empty, "bad"));

        Assert.True(valid.IsValid);
        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, x => x.ErrorCode == "REGISTRATION_ID_REQUIRED");
        Assert.Contains(invalid.Errors, x => x.ErrorCode == "EMAIL_INVALID");
    }
}
