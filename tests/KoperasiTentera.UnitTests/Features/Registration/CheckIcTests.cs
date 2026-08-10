using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class CheckIcTests
{
    [Fact]
    public async Task UnknownIc_ReturnsIcNotExists_AndCreatesNoSession()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.CheckIcAsync(new CheckIcRequest("880214566831"));

        Assert.True(result.IsSuccess);
        Assert.Equal("IC_NOT_EXISTS", result.Code);
        Assert.Equal(RegistrationNextSteps.EnterDetails, result.Value!.NextStep);
        Assert.Null(result.Value.RegistrationId);
        Assert.Empty(ctx.Db.Registrations);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Fact]
    public async Task ExistingIc_CreatesFreshLoginSessionAndMobileOtp()
    {
        using var ctx = new RegistrationTestContext();
        var existing = await ctx.AddRegistrationAsync(status: RegistrationStatuses.Completed);

        var result = await ctx.Service.CheckIcAsync(new CheckIcRequest(existing.ICNumber));

        Assert.True(result.IsSuccess);
        Assert.Equal("IC_EXISTS", result.Code);
        Assert.NotNull(result.Value);
        Assert.NotEqual(existing.Id, result.Value!.RegistrationId);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, result.Value.Status);
        Assert.Equal(RegistrationNextSteps.VerifyMobileOtp, result.Value.NextStep);
        Assert.Equal("******6675", result.Value.MaskedMobile);

        var sessions = await ctx.GetRegistrationsByIcAsync(existing.ICNumber);
        Assert.Equal(2, sessions.Count);
        Assert.Equal(RegistrationStatuses.Completed, sessions[0].Status);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, sessions[1].Status);

        var otps = await ctx.GetOtpsAsync(result.Value.RegistrationId!.Value, OtpChannels.Mobile);
        Assert.Empty(otps); // current CheckIcAsync uses legacy MobileOtp fields, not OtpVerification.

        var loginSession = await ctx.GetRegistrationAsync(result.Value.RegistrationId.Value);
        Assert.NotNull(loginSession);
        Assert.NotNull(loginSession!.MobileOtp);
        Assert.Matches("^\\d{4}$", loginSession.MobileOtp);
        Assert.NotNull(loginSession.OtpExpiry);
        Assert.True(loginSession.OtpExpiry > DateTime.UtcNow);
        Assert.Equal(0, loginSession.OtpAttempts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefghijk1")]
    [InlineData("88021456683A")]
    public async Task InvalidIc_ReturnsValidationError(string ic)
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.CheckIcAsync(new CheckIcRequest(ic));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(ctx.Db.Registrations);
    }

    [Fact]
    public async Task RepeatedExistingIcCheck_CreatesAnotherFreshSession()
    {
        using var ctx = new RegistrationTestContext();
        var existing = await ctx.AddRegistrationAsync(status: RegistrationStatuses.Completed);

        var first = await ctx.Service.CheckIcAsync(new CheckIcRequest(existing.ICNumber));
        var second = await ctx.Service.CheckIcAsync(new CheckIcRequest(existing.ICNumber));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value!.RegistrationId, second.Value!.RegistrationId);

        var sessions = await ctx.GetRegistrationsByIcAsync(existing.ICNumber);
        Assert.Equal(3, sessions.Count);
        Assert.All(sessions.Skip(1), x => Assert.Equal(RegistrationStatuses.PendingOtpMobile, x.Status));
    }
}
