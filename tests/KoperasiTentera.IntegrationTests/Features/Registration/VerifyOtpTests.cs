using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class VerifyOtpTests
{
    [Fact]
    public async Task CorrectMobileOtp_MarksMobileVerified_AndCreatesEmailOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.True(result.IsSuccess);
        Assert.Equal("OTP_VERIFIED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.VerifyEmailOtp, result.Value.NextStep);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.True(dbRegistration!.IsMobileVerified);
        Assert.False(dbRegistration.IsEmailVerified);

        var mobileOtps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        var emailOtps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Email);
        Assert.True(mobileOtps.Single().IsUsed);
        Assert.Single(emailOtps);
        Assert.False(emailOtps[0].IsUsed);
        Assert.Equal(0, emailOtps[0].Attempts);
    }

    [Fact]
    public async Task CorrectEmailOtp_MarksEmailVerified_AndMovesToPrivacy()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingOtpEmail,
            mobileVerified: true);
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "7981");

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Email));

        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingPrivacyPolicy, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.AcceptPrivacyPolicy, result.Value.NextStep);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.True(dbRegistration!.IsMobileVerified);
        Assert.True(dbRegistration.IsEmailVerified);

        var emailOtp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Email)).Single();
        Assert.True(emailOtp.IsUsed);
    }

    [Fact]
    public async Task WrongOtp_IncrementsAttempts()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var otp = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_OTP", result.Code);

        var dbOtp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single(x => x.Id == otp.Id);
        Assert.Equal(1, dbOtp.Attempts);
        Assert.False(dbOtp.IsUsed);
    }

    [Fact]
    public async Task ThirdWrongOtp_ExhaustsAttemptsAndUsesOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var otp = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981", attempts: 2);

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_MAX_ATTEMPTS_EXCEEDED", result.Code);

        var dbOtp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single(x => x.Id == otp.Id);
        Assert.Equal(3, dbOtp.Attempts);
        Assert.True(dbOtp.IsUsed);
    }

    [Fact]
    public async Task AlreadyMaxedOtp_ReturnsMaxAttemptsAndUsesOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var otp = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981", attempts: 3);

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_MAX_ATTEMPTS_EXCEEDED", result.Code);

        var dbOtp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single(x => x.Id == otp.Id);
        Assert.True(dbOtp.IsUsed);
    }

    [Fact]
    public async Task ExpiredOtp_IsMarkedUsedAndRejected()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        var otp = await ctx.AddOtpAsync(
            registration.Id,
            OtpChannels.Mobile,
            "7981",
            expiresAtUtc: DateTime.UtcNow.AddMinutes(-1));

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_EXPIRED", result.Code);

        var dbOtp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single(x => x.Id == otp.Id);
        Assert.True(dbOtp.IsUsed);
    }

    [Fact]
    public async Task NoActiveOtp_ReturnsOtpExpiredCode()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981", isUsed: true);

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_EXPIRED", result.Code);
    }

    [Fact]
    public async Task WrongMobileState_ReturnsInvalidRegistrationState()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail);
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task WrongEmailState_ReturnsInvalidRegistrationState()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPrivacyPolicy);
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "7981");

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Email));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(Guid.NewGuid(), "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("abcd")]
    public async Task InvalidOtpFormat_ReturnsValidationError(string otp)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, otp, OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "OTP_FORMAT" || x.Code == "OTP_REQUIRED");
    }

    [Fact]
    public async Task InvalidChannel_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", "SMS"));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "CHANNEL_INVALID");
    }
}
