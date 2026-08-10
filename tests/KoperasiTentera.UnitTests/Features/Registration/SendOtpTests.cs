using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class SendOtpTests
{
    [Theory]
    [InlineData(OtpChannels.Mobile, RegistrationNextSteps.VerifyMobileOtp)]
    [InlineData(OtpChannels.Email, RegistrationNextSteps.VerifyEmailOtp)]
    public async Task ValidRequest_CreatesHashedOtp(string channel, string nextStep)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.SendOtpAsync(new SendOtpRequest(registration.Id, channel));

        Assert.True(result.IsSuccess);
        Assert.Equal("OTP_SENT", result.Code);
        Assert.Equal(registration.Id, result.Value!.RegistrationId);
        Assert.Equal(nextStep, result.Value.NextStep);

        var otps = await ctx.GetOtpsAsync(registration.Id, channel);
        Assert.Single(otps);
        Assert.False(otps[0].IsUsed);
        Assert.Equal(0, otps[0].Attempts);
        Assert.Equal(64, otps[0].OtpHash.Length);
        Assert.NotEqual(ctx.LastGeneratedOtp(channel), otps[0].OtpHash);
        Assert.True(otps[0].ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Resend_InvalidatesPreviousOtpOfSameChannel()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var old = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "1111");

        var result = await ctx.Service.SendOtpAsync(new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        Assert.True(result.IsSuccess);
        var otps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        Assert.Equal(2, otps.Count);
        Assert.True(otps.Single(x => x.Id == old.Id).IsUsed);
        Assert.False(otps.Single(x => x.Id != old.Id).IsUsed);
    }

    [Fact]
    public async Task Resend_DoesNotInvalidateOtherChannel()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var mobile = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "1111");
        var email = await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "2222");

        await ctx.Service.SendOtpAsync(new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        var mobileOtps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        var emailOtps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Email);

        Assert.True(mobileOtps.Single(x => x.Id == mobile.Id).IsUsed);
        Assert.False(emailOtps.Single(x => x.Id == email.Id).IsUsed);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.SendOtpAsync(new SendOtpRequest(Guid.NewGuid(), OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("SMS")]
    [InlineData("WhatsApp")]
    public async Task InvalidChannel_ReturnsValidationError(string channel)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.SendOtpAsync(new SendOtpRequest(registration.Id, channel));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task EmptyRegistrationId_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.SendOtpAsync(new SendOtpRequest(Guid.Empty, OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "REGISTRATION_ID_REQUIRED");
    }
}
