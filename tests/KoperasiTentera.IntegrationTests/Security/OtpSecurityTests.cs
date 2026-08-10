using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Security;

public class OtpSecurityTests
{
    [Fact]
    public async Task StoredOtpIsHashed_NotPlaintext()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        await ctx.Service.SendOtpAsync(
            new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        var otp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single();
        var plainOtp = ctx.LastGeneratedOtp(OtpChannels.Mobile)!;

        Assert.NotEqual(plainOtp, otp.OtpHash);
        Assert.Equal(64, otp.OtpHash.Length);
    }

    [Fact]
    public async Task WrongOtpDoesNotChangeRegistrationVerificationFlags()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.False(dbRegistration!.IsMobileVerified);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, dbRegistration.Status);
    }

    [Fact]
    public async Task SuccessfulOtpCannotBeReused()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var first = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        var second = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Mobile));

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", second.Code);
    }

    [Fact]
    public async Task ResendResetsAttemptCounterOnNewOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981", attempts: 2);

        await ctx.Service.SendOtpAsync(
            new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        var active = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile))
            .Single(x => !x.IsUsed);

        Assert.Equal(0, active.Attempts);
    }
}
