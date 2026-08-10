using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Security;

public class OtpSecurityTests
{
    [Fact]
    public async Task StoredOtp_IsSha256Hash_NotPlainText()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        await ctx.Service.SendOtpAsync(new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        var otp = (await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile)).Single();
        Assert.Equal(64, otp.OtpHash.Length);
        Assert.DoesNotContain(ctx.LastGeneratedOtp(OtpChannels.Mobile)!, otp.OtpHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongOtp_DoesNotVerifyRegistration()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.False(db!.IsMobileVerified);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, db.Status);
    }

    [Fact]
    public async Task Resend_InvalidatesOldActiveOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        await ctx.Service.SendOtpAsync(new SendOtpRequest(registration.Id, OtpChannels.Mobile));

        var otps = await ctx.GetOtpsAsync(registration.Id, OtpChannels.Mobile);
        Assert.Equal(2, otps.Count);
        Assert.True(otps[0].IsUsed);
        Assert.False(otps[1].IsUsed);
    }
}
