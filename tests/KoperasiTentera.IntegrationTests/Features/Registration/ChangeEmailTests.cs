using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class ChangeEmailTests
{
    [Fact]
    public async Task ValidEmailChange_UpdatesEmailResetsVerificationAndCreatesOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingOtpEmail,
            email: "old@email.com",
            emailVerified: true);
        var oldOtp = await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "1111");

        var result = await ctx.Service.ChangeEmailAsync(
            new ChangeEmailRequest(registration.Id, "new@email.com"));

        Assert.True(result.IsSuccess);
        Assert.Equal("EMAIL_CHANGED", result.Code);
        Assert.Equal("ne•••@••••.com", result.Value!.MaskedEmail);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, result.Value.Status);
        Assert.Equal(RegistrationNextSteps.VerifyEmailOtp, result.Value.NextStep);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.Equal("new@email.com", dbRegistration!.Email);
        Assert.False(dbRegistration.IsEmailVerified);

        var otps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Email);
        Assert.Equal(2, otps.Count);
        Assert.True(otps.Single(x => x.Id == oldOtp.Id).IsUsed);
        Assert.False(otps.Single(x => x.Id != oldOtp.Id).IsUsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-email")]
    public async Task InvalidEmail_ReturnsValidationError(string email)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail);

        var result = await ctx.Service.ChangeEmailAsync(
            new ChangeEmailRequest(registration.Id, email));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.Equal("mariam@email.com", dbRegistration!.Email);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.ChangeEmailAsync(
            new ChangeEmailRequest(Guid.NewGuid(), "new@email.com"));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData(RegistrationStatuses.PendingOtpMobile)]
    [InlineData(RegistrationStatuses.PendingPrivacyPolicy)]
    [InlineData(RegistrationStatuses.PendingPin)]
    [InlineData(RegistrationStatuses.PendingFaceVerification)]
    [InlineData(RegistrationStatuses.Completed)]
    public async Task WrongState_ReturnsInvalidRegistrationState(string status)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: status);

        var result = await ctx.Service.ChangeEmailAsync(
            new ChangeEmailRequest(registration.Id, "new@email.com"));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task EmailChange_TrimsEmailBeforePersisting()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail);

        await ctx.Service.ChangeEmailAsync(
            new ChangeEmailRequest(registration.Id, "  new@email.com  "));

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.Equal("new@email.com", dbRegistration!.Email);
    }
}
