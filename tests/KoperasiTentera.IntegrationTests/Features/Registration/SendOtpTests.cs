using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class SendOtpTests
{
    [Theory]
    [InlineData(OtpChannels.Mobile)]
    [InlineData(OtpChannels.Email)]
    public async Task ValidRequest_CreatesOtpAndReturnsCorrectNextStep(string channel)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail);

        var result = await ctx.Service.SendOtpAsync(
            new SendOtpRequest(registration.Id, channel));

        Assert.True(result.IsSuccess);
        Assert.Equal("OTP_SENT", result.Code);
        Assert.Equal(registration.Id, result.Value!.RegistrationId);
        Assert.Equal(
            channel == OtpChannels.Mobile
                ? RegistrationNextSteps.VerifyMobileOtp
                : RegistrationNextSteps.VerifyEmailOtp,
            result.Value.NextStep);

        var otps = await ctx.GetOtpsAsync(registration.Id, channel);
        Assert.Single(otps);
        Assert.False(otps[0].IsUsed);
        Assert.Equal(0, otps[0].Attempts);
        Assert.True(otps[0].ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(4, ctx.LastGeneratedOtp(channel)!.Length);
    }

    [Fact]
    public async Task ResendInvalidatesPreviousOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "1111");

        var result = await ctx.Service.SendOtpAsync(
            new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        Assert.True(result.IsSuccess);

        var otps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        Assert.Equal(2, otps.Count);
        Assert.True(otps[0].IsUsed);
        Assert.False(otps[1].IsUsed);
        Assert.Equal(0, otps[1].Attempts);
    }

    [Fact]
    public async Task ResendOnlyInvalidatesSameChannel()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var mobile = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "1111");
        var email = await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "2222");

        await ctx.Service.SendOtpAsync(
            new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        var mobileAfter = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        var emailAfter = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Email);

        Assert.True(mobileAfter.Single(x => x.Id == mobile.Id).IsUsed);
        Assert.False(emailAfter.Single(x => x.Id == email.Id).IsUsed);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.SendOtpAsync(
            new SendOtpRequest(Guid.NewGuid(), OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("SMS")]
    [InlineData("WhatsApp")]
    public async Task InvalidChannel_ReturnsValidationError(string channel)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.SendOtpAsync(
            new SendOtpRequest(registration.Id, channel));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Fact]
    public async Task EmptyRegistrationId_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.SendOtpAsync(
            new SendOtpRequest(Guid.Empty, OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "REGISTRATION_ID_REQUIRED");
    }
}
