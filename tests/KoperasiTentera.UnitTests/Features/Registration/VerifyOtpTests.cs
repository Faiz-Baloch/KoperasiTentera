using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class VerifyOtpTests
{
    [Fact]
    public async Task CorrectMobileOtp_MarksMobileVerifiedAndMovesToEmail()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpMobile);
        var otp = await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.True(result.IsSuccess);
        Assert.Equal("OTP_VERIFIED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.VerifyEmailOtp, result.Value.NextStep);
        Assert.True(result.Value.MaskedMobile == "******6675");

        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.True(db!.IsMobileVerified);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, db.Status);

        var dbOtp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single(x => x.Id == otp.Id);
        Assert.True(dbOtp.IsUsed);
    }

    [Fact]
    public async Task CorrectEmailOtp_MarksEmailVerifiedAndMovesToPrivacy()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail, mobileVerified: true);
        var otp = await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "7981");

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Email));

        Assert.True(result.IsSuccess);
        Assert.Equal("OTP_VERIFIED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingPrivacyPolicy, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.AcceptPrivacyPolicy, result.Value.NextStep);

        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.True(db!.IsEmailVerified);
        Assert.True((await ctx.GetOtpsAsync(registration.Id, OtpChannels.Email)).Single(x => x.Id == otp.Id).IsUsed);
    }

    [Fact]
    public async Task WrongOtp_IncrementsAttemptsWithoutVerifying()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_OTP", result.Code);
        Assert.Equal("Incorrect OTP. Please enter your OTP again.", result.Message);

        var otp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single();
        Assert.Equal(1, otp.Attempts);
        Assert.False(otp.IsUsed);
    }

    [Fact]
    public async Task ThirdWrongOtp_ReachesMaximumAttemptsButLeavesRecordUnused()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981", attempts: 2);

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_OTP", result.Code);
        var otp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single();
        Assert.Equal(3, otp.Attempts);
        Assert.False(otp.IsUsed);
    }

    [Fact]
    public async Task FourthAttemptAfterMaximum_ReturnsMaximumAttempts()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981", attempts: 3);

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_OTP", result.Code);
        Assert.Contains("Maximum attempts exceeded", result.Message);
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

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_EXPIRED", result.Code);
        Assert.True((await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single(x => x.Id == otp.Id).IsUsed);
    }

    [Fact]
    public async Task NoActiveOtp_ReturnsOtpExpired()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_EXPIRED", result.Code);
    }

    [Fact]
    public async Task SuccessfulOtpCannotBeReused()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var first = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));
        var second = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("OTP_EXPIRED", second.Code);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(Guid.NewGuid(), "7981", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12A4")]
    public async Task InvalidOtpFormat_ReturnsValidationError(string otp)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, otp, OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
    }

    [Fact]
    public async Task InvalidChannel_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", "SMS"));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
    }
}
