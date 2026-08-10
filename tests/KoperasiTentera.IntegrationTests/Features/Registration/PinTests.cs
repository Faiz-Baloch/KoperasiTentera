using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Services.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class PinTests
{
    private static SetPinRequest Request(Guid id, string pin = "135790", string confirm = "135790") =>
        new(id, pin, confirm);

    [Fact]
    public async Task ValidPin_IsHashedAndMovesToFaceVerification()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("PIN_SET", result.Code);
        Assert.Equal(RegistrationStatuses.PendingFaceVerification, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.VerifyFace, result.Value.NextStep);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.NotNull(dbRegistration!.PinHash);
        Assert.NotEqual("135790", dbRegistration.PinHash);
        Assert.True(PinHasher.Verify("135790", dbRegistration.PinHash));
        Assert.False(PinHasher.Verify("135791", dbRegistration.PinHash));
    }

    [Fact]
    public async Task PinMismatch_ReturnsBusinessErrorAndDoesNotPersistPin()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id, "135790", "135791"));

        Assert.False(result.IsSuccess);
        Assert.Equal("PIN_MISMATCH", result.Code);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.Null(dbRegistration!.PinHash);
        Assert.Equal(RegistrationStatuses.PendingPin, dbRegistration.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345A")]
    public async Task InvalidPin_ReturnsValidationError(string pin)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id, pin, pin));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Null((await ctx.GetRegistrationAsync(registration.Id))!.PinHash);
    }

    [Fact]
    public async Task EmptyConfirmation_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id, "135790", ""));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "CONFIRM_PIN_REQUIRED");
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.SetPinAsync(Request(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData(RegistrationStatuses.PendingOtpMobile)]
    [InlineData(RegistrationStatuses.PendingOtpEmail)]
    [InlineData(RegistrationStatuses.PendingPrivacyPolicy)]
    [InlineData(RegistrationStatuses.PendingFaceVerification)]
    [InlineData(RegistrationStatuses.Completed)]
    public async Task WrongState_ReturnsInvalidRegistrationState(string status)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: status,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task PendingPin_ButMobileNotVerified_ReturnsMobileNotVerified()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: false,
            emailVerified: true,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("MOBILE_NOT_VERIFIED", result.Code);
    }

    [Fact]
    public async Task PendingPin_ButEmailNotVerified_ReturnsEmailNotVerified()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: true,
            emailVerified: false,
            privacyAccepted: true);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_NOT_VERIFIED", result.Code);
    }

    [Fact]
    public async Task PendingPin_ButPrivacyNotAccepted_ReturnsPrivacyNotAccepted()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPin,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: false);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("PRIVACY_NOT_ACCEPTED", result.Code);
    }
}
