using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Services.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class FaceVerificationTests
{
    [Fact]
    public async Task ValidFaceVerification_CompletesRegistration()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingFaceVerification,
            mobileVerified: true,
            emailVerified: true,
            privacyAccepted: true,
            pinHash: PinHasher.Hash("135790"));

        var result = await ctx.Service.VerifyFaceAsync(new VerifyFaceRequest
        {
            RegistrationId = registration.Id,
            FaceImagePath = "uploads/faces/session.jpg"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("REGISTRATION_COMPLETED", result.Code);
        Assert.Equal(RegistrationStatuses.Completed, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.Completed, result.Value.NextStep);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.True(dbRegistration!.IsFaceVerified);
        Assert.Equal("uploads/faces/session.jpg", dbRegistration.FaceImagePath);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.VerifyFaceAsync(new VerifyFaceRequest
        {
            RegistrationId = Guid.NewGuid(),
            FaceImagePath = "face.jpg"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData(RegistrationStatuses.PendingOtpMobile)]
    [InlineData(RegistrationStatuses.PendingOtpEmail)]
    [InlineData(RegistrationStatuses.PendingPrivacyPolicy)]
    [InlineData(RegistrationStatuses.PendingPin)]
    [InlineData(RegistrationStatuses.Completed)]
    public async Task WrongState_ReturnsInvalidRegistrationState(string status)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: status,
            pinHash: PinHasher.Hash("135790"));

        var result = await ctx.Service.VerifyFaceAsync(new VerifyFaceRequest
        {
            RegistrationId = registration.Id,
            FaceImagePath = "face.jpg"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REGISTRATION_STATE", result.Code);
    }

    [Fact]
    public async Task PendingFaceVerificationWithoutPin_ReturnsPinNotSet()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingFaceVerification);

        var result = await ctx.Service.VerifyFaceAsync(new VerifyFaceRequest
        {
            RegistrationId = registration.Id,
            FaceImagePath = "face.jpg"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("PIN_NOT_SET", result.Code);

        var dbRegistration = await ctx.GetRegistrationAsync(registration.Id);
        Assert.False(dbRegistration!.IsFaceVerified);
        Assert.Equal(RegistrationStatuses.PendingFaceVerification, dbRegistration.Status);
    }
}
