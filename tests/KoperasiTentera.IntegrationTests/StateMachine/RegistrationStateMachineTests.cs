using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.StateMachine;

public class RegistrationStateMachineTests
{
    [Fact]
    public async Task CompleteNewRegistrationFlow_TransitionsThroughEveryState()
    {
        using var ctx = new RegistrationTestContext();

        var start = await ctx.Service.StartRegistrationAsync(
            new StartRegistrationRequest(
                "Mariam Abdul Rashid",
                "880214566831",
                "0163386675",
                "mariam@email.com"));

        Assert.True(start.IsSuccess);
        var sessionId = start.Value!.RegistrationId!.Value;
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, start.Value.Status);

        var mobileOtp = ctx.LastGeneratedOtp(OtpChannels.Mobile)!;
        var mobile = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(sessionId, mobileOtp, OtpChannels.Mobile));

        Assert.True(mobile.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, mobile.Value!.Status);

        var emailOtp = ctx.LastGeneratedOtp(OtpChannels.Email)!;
        var email = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(sessionId, emailOtp, OtpChannels.Email));

        Assert.True(email.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingPrivacyPolicy, email.Value!.Status);

        var privacy = await ctx.Service.AcceptPrivacyPolicyAsync(
            new AcceptPrivacyPolicyRequest(sessionId, true));

        Assert.True(privacy.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingPin, privacy.Value!.Status);

        var pin = await ctx.Service.SetPinAsync(
            new SetPinRequest(sessionId, "135790", "135790"));

        Assert.True(pin.IsSuccess);
        Assert.Equal(RegistrationStatuses.PendingFaceVerification, pin.Value!.Status);

        var face = await ctx.Service.VerifyFaceAsync(new VerifyFaceRequest
        {
            RegistrationId = sessionId,
            FaceImagePath = "faces/mariam.jpg"
        });

        Assert.True(face.IsSuccess);
        Assert.Equal(RegistrationStatuses.Completed, face.Value!.Status);
        Assert.Equal(RegistrationNextSteps.Completed, face.Value.NextStep);

        var final = await ctx.GetRegistrationAsync(sessionId);
        Assert.True(final!.IsMobileVerified);
        Assert.True(final.IsEmailVerified);
        Assert.True(final.IsPrivacyAccepted);
        Assert.NotNull(final.PinHash);
        Assert.True(final.IsFaceVerified);
        Assert.Equal("faces/mariam.jpg", final.FaceImagePath);
    }

    [Fact]
    public async Task MobileOtpCannotBeVerifiedAfterMovingToEmailStep()
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
    public async Task EmailOtpCannotBeVerifiedBeforeMobileStep()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpMobile);
        await ctx.AddOtpAsync(registration.Id, OtpChannels.Email, "7981");

        var result = await ctx.Service.VerifyOtpAsync(
            new VerifyOtpRequest(registration.Id, "7981", OtpChannels.Email));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task PinCannotBeSetBeforePrivacyPolicy()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPrivacyPolicy,
            mobileVerified: true,
            emailVerified: true);

        var result = await ctx.Service.SetPinAsync(
            new SetPinRequest(registration.Id, "135790", "135790"));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task FaceCannotBeVerifiedBeforePin()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingFaceVerification);

        var result = await ctx.Service.VerifyFaceAsync(new VerifyFaceRequest
        {
            RegistrationId = registration.Id,
            FaceImagePath = "face.jpg"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("PIN_NOT_SET", result.Code);
    }
}
