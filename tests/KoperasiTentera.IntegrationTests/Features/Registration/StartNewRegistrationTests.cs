using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class StartNewRegistrationTests
{
    private static StartRegistrationRequest ValidRequest(string ic = "880214566831") =>
        new("Mariam Abdul Rashid", ic, "0163386675", "mariam@email.com");

    [Fact]
    public async Task ValidRequest_CreatesRegistrationAndMobileOtp()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.StartRegistrationAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("REGISTRATION_STARTED", result.Code);
        Assert.NotNull(result.Value!.RegistrationId);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, result.Value.Status);
        Assert.Equal(RegistrationNextSteps.VerifyMobileOtp, result.Value.NextStep);
        Assert.Equal("******6675", result.Value.MaskedMobile);
        Assert.Null(result.Value.MaskedEmail);

        var registration = await ctx.GetRegistrationAsync(result.Value.RegistrationId!.Value);
        Assert.NotNull(registration);
        Assert.Equal("Mariam Abdul Rashid", registration!.CustomerName);
        Assert.Equal("880214566831", registration.ICNumber);
        Assert.False(registration.IsMobileVerified);
        Assert.False(registration.IsEmailVerified);
        Assert.False(registration.IsPrivacyAccepted);
        Assert.Null(registration.PinHash);

        var otps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        Assert.Single(otps);
        Assert.False(otps[0].IsUsed);
        Assert.True(otps[0].ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(0, otps[0].Attempts);
        Assert.Equal(4, ctx.LastGeneratedOtp(OtpChannels.Mobile)!.Length);
    }

    [Fact]
    public async Task CompletedAccount_ReturnsAccountAlreadyExists()
    {
        using var ctx = new RegistrationTestContext();
        await ctx.AddRegistrationAsync(status: RegistrationStatuses.Completed, ic: "880214566831");

        var result = await ctx.Service.StartRegistrationAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_ALREADY_EXISTS", result.Code);
        Assert.Single(ctx.Db.Registrations);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Fact]
    public async Task PendingRegistration_ReturnsAlreadyInProgress()
    {
        using var ctx = new RegistrationTestContext();
        await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail, ic: "880214566831");

        var result = await ctx.Service.StartRegistrationAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("REGISTRATION_ALREADY_IN_PROGRESS", result.Code);
        Assert.Single(ctx.Db.Registrations);
    }

    [Theory]
    [InlineData("", "0163386675", "mariam@email.com")]
    [InlineData("Mariam", "", "mariam@email.com")]
    [InlineData("Mariam", "0163386675", "")]
    [InlineData("Mariam", "123", "mariam@email.com")]
    [InlineData("Mariam", "0163386675", "not-email")]
    [InlineData("Mariam", "019999999999", "mariam@email.com")]
    public async Task InvalidRequest_ReturnsValidationError(
        string name,
        string mobile,
        string email)
    {
        using var ctx = new RegistrationTestContext();
        var request = new StartRegistrationRequest(name, "880214566831", mobile, email);

        var result = await ctx.Service.StartRegistrationAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(ctx.Db.Registrations);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Fact]
    public async Task SecondStartForSameIc_IsBlockedWithoutCreatingAnotherSession()
    {
        using var ctx = new RegistrationTestContext();

        var first = await ctx.Service.StartRegistrationAsync(ValidRequest());
        var second = await ctx.Service.StartRegistrationAsync(ValidRequest());

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("REGISTRATION_ALREADY_IN_PROGRESS", second.Code);
        Assert.Single(ctx.Db.Registrations);
        Assert.Single(ctx.Db.OtpVerifications);
    }
}
