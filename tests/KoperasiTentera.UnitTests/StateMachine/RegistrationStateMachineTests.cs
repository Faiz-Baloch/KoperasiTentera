using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.StateMachine;

public class RegistrationStateMachineTests
{
    [Fact]
    public async Task NewRegistrationFlow_CompletesUsingCurrentServiceEndpoints()
    {
        using var ctx = new RegistrationTestContext();

        var start = await ctx.Service.StartRegistrationAsync(
            new StartRegistrationRequest("Mariam Abdul Rashid", "880214566831", "0163386675", "mariam@email.com"));

        Assert.True(start.IsSuccess);
        var id = start.Value!.RegistrationId!.Value;
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, start.Value.Status);

        await ctx.Service.SendOtpAsync(new SendOtpRequest(id, OtpChannels.Mobile));
        var mobileOtp = ctx.LastGeneratedOtp(OtpChannels.Mobile)!;
        Assert.Matches("^\\d{4}$", mobileOtp);

        var mobile = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(id, mobileOtp, OtpChannels.Mobile));
        Assert.True(mobile.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, mobile.Value!.Status);

        await ctx.Service.SendOtpAsync(new SendOtpRequest(id, OtpChannels.Email));
        var emailOtp = ctx.LastGeneratedOtp(OtpChannels.Email)!;
        Assert.Matches("^\\d{4}$", emailOtp);

        var email = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(id, emailOtp, OtpChannels.Email));
        Assert.True(email.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingPrivacyPolicy, email.Value!.Status);

        var privacy = await ctx.Service.AcceptPrivacyPolicyAsync(new AcceptPrivacyPolicyRequest(id, true));
        Assert.True(privacy.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingPin, privacy.Value!.Status);

        var pin = await ctx.Service.SetPinAsync(new SetPinRequest(id, "135790", "135790"));
        Assert.True(pin.IsSuccess);
        Assert.Equal(RegistrationStatuses.Completed, pin.Value!.Status);
        Assert.Equal(RegistrationNextSteps.Completed, pin.Value.NextStep);

        var final = await ctx.GetRegistrationAsync(id);
        Assert.Equal(RegistrationStatuses.Completed, final!.Status);
        Assert.True(final.IsMobileVerified);
        Assert.True(final.IsEmailVerified);
        Assert.True(final.IsPrivacyAccepted);
        Assert.NotNull(final.PinHash);
    }

    [Fact]
    public async Task WrongOtp_DoesNotAdvanceState()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpMobile);
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Mobile, "7981");

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "1234", OtpChannels.Mobile));

        Assert.False(result.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, (await ctx.GetRegistrationAsync(registration.Id))!.Status);
    }

    [Fact]
    public async Task EmailVerification_RequiresAnActiveEmailOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpEmail);

        var result = await ctx.Service.VerifyOtpAsync(new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Email));

        Assert.False(result.IsSuccess);
        Assert.Equal("OTP_EXPIRED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, (await ctx.GetRegistrationAsync(registration.Id))!.Status);
    }
}
