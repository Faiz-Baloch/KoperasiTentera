using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class PrivacyPolicyTests
{
    [Fact]
    public async Task AcceptedPolicy_SetsFlagAndMovesToPin()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPrivacyPolicy);

        var result = await ctx.Service.AcceptPrivacyPolicyAsync(new AcceptPrivacyPolicyRequest(registration.Id, true));

        Assert.True(result.IsSuccess);
        Assert.Equal("PRIVACY_ACCEPTED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingPin, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.SetPin, result.Value.NextStep);
        Assert.True((await ctx.GetRegistrationAsync(registration.Id))!.IsPrivacyAccepted);
    }

    [Fact]
    public async Task RejectedPolicy_ReturnsBusinessErrorAndDoesNotChangeState()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPrivacyPolicy);

        var result = await ctx.Service.AcceptPrivacyPolicyAsync(new AcceptPrivacyPolicyRequest(registration.Id, false));

        Assert.False(result.IsSuccess);
        Assert.Equal("PRIVACY_NOT_ACCEPTED", result.Code);
        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.False(db!.IsPrivacyAccepted);
        Assert.Equal(RegistrationStatuses.PendingPrivacyPolicy, db.Status);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.AcceptPrivacyPolicyAsync(new AcceptPrivacyPolicyRequest(Guid.NewGuid(), true));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Fact]
    public async Task AcceptedPolicy_DoesNotChangeOtpVerificationFlags()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingPrivacyPolicy,
            mobileVerified: true,
            emailVerified: true);

        var result = await ctx.Service.AcceptPrivacyPolicyAsync(new AcceptPrivacyPolicyRequest(registration.Id, true));

        Assert.True(result.IsSuccess);
        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.True(db!.IsMobileVerified);
        Assert.True(db.IsEmailVerified);
    }

    [Fact]
    public async Task EmptyRegistrationId_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.AcceptPrivacyPolicyAsync(new AcceptPrivacyPolicyRequest(Guid.Empty, true));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "REGISTRATION_ID_REQUIRED");
    }
}
