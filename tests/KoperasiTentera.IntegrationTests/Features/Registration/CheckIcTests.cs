using KoperasiTentera.Domain.Common;
using KoperasiTentera.UnitTests.TestSupport;
using KoperasiTentera.Service.DTOs.Registration;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class CheckIcTests
{
    [Fact]
    public async Task UnknownIc_ReturnsIcNotExists_AndDoesNotCreateOtp()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.CheckIcAsync(
            new CheckIcRequest("880214566831"));

        Assert.True(result.IsSuccess);
        Assert.Equal("IC_NOT_EXISTS", result.Code);

        Assert.Equal(
            RegistrationNextSteps.EnterDetails,
            result.Value!.NextStep);

        Assert.Null(result.Value.RegistrationId);

        Assert.Empty(ctx.Db.OtpVerifications);
        Assert.Empty(ctx.Db.Registrations);
    }

    [Fact]
    public async Task ExistingIc_CreatesMobileOtp_AndReturnsVerifyMobileStep()
    {
        using var ctx = new RegistrationTestContext();

        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.Completed);

        // Simulate a new HTTP request.
        ctx.Db.ChangeTracker.Clear();

        var result = await ctx.Service.CheckIcAsync(
            new CheckIcRequest(registration.ICNumber));

        Assert.True(result.IsSuccess);
        Assert.Equal("IC_EXISTS", result.Code);

        Assert.NotNull(result.Value);

        Assert.Equal(
            RegistrationNextSteps.VerifyMobileOtp,
            result.Value!.NextStep);

        Assert.Equal(
            RegistrationStatuses.PendingOtpMobile,
            result.Value.Status);

        Assert.Equal(
            "******6675",
            result.Value.MaskedMobile);

        // Existing IC uses the existing registration/session.
        Assert.Equal(
            registration.Id,
            result.Value.RegistrationId);

        var session = await ctx.GetRegistrationAsync(
            result.Value.RegistrationId!.Value);

        Assert.NotNull(session);

        Assert.Equal(
            registration.Id,
            session!.Id);

        Assert.Equal(
            RegistrationStatuses.PendingOtpMobile,
            session.Status);

        Assert.Equal(
            registration.ICNumber,
            session.ICNumber);

        Assert.Equal(
            registration.MobileNumber,
            session.MobileNumber);

        Assert.Equal(
            registration.Email,
            session.Email);

        var otps = await ctx.GetOtpsAsync(
            session.Id,
            OtpChannels.Mobile);

        Assert.Single(otps);

        Assert.False(otps[0].IsUsed);
        Assert.NotEmpty(otps[0].OtpHash);

        // OTP hash must not be the plain OTP.
        Assert.NotEqual(
            "7981",
            otps[0].OtpHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefghijk1")]
    [InlineData("88021456683A")]
    public async Task InvalidIc_ReturnsValidationError(
        string ic)
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.CheckIcAsync(
            new CheckIcRequest(ic));

        Assert.False(result.IsSuccess);

        Assert.Equal(
            "VALIDATION_FAILED",
            result.Code);

        Assert.NotEmpty(result.Errors);

        Assert.Empty(ctx.Db.OtpVerifications);
        Assert.Empty(ctx.Db.Registrations);
    }

    [Fact]
    public async Task ExistingIc_RepeatedCheck_InvalidatesPreviousMobileOtp()
    {
        using var ctx = new RegistrationTestContext();

        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.Completed);

        // -----------------------------
        // First request
        // -----------------------------

        ctx.Db.ChangeTracker.Clear();

        var firstResult = await ctx.Service.CheckIcAsync(
            new CheckIcRequest(registration.ICNumber));

        Assert.True(firstResult.IsSuccess);
        Assert.NotNull(firstResult.Value);

        var registrationId =
            firstResult.Value!.RegistrationId!.Value;

        Assert.Equal(
            registration.Id,
            registrationId);

        // -----------------------------
        // Second request
        // -----------------------------

        ctx.Db.ChangeTracker.Clear();

        var secondResult = await ctx.Service.CheckIcAsync(
            new CheckIcRequest(registration.ICNumber));

        Assert.True(secondResult.IsSuccess);
        Assert.NotNull(secondResult.Value);

        // Same registration/session is used.
        Assert.Equal(
            registrationId,
            secondResult.Value!.RegistrationId);

        // -----------------------------
        // Verify OTP lifecycle
        // -----------------------------

        var otps = await ctx.GetOtpsAsync(
            registrationId,
            OtpChannels.Mobile);

        Assert.Equal(2, otps.Count);

        var latestOtp = otps
            .OrderByDescending(x => x.CreatedAtUtc)
            .First();

        var previousOtp = otps
            .Where(x => x.Id != latestOtp.Id)
            .Single();

        // Previous OTP must be invalidated.
        Assert.True(previousOtp.IsUsed);

        // Latest OTP must remain active.
        Assert.False(latestOtp.IsUsed);

        Assert.Equal(
            0,
            latestOtp.Attempts);

        Assert.True(
            latestOtp.ExpiresAtUtc > DateTime.UtcNow);
    }
}