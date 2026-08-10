using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class StartRegistrationTests
{
    private static StartRegistrationRequest Request(
        string name = "Mariam Abdul Rashid",
        string ic = "880214566831",
        string mobile = "0163386675",
        string email = "mariam@email.com") =>
        new(name, ic, mobile, email);

    [Fact]
    public async Task ValidRequest_CreatesPendingSessionAndLegacyMobileOtp()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.StartRegistrationAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal("REGISTRATION_STARTED", result.Code);
        Assert.NotNull(result.Value!.RegistrationId);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, result.Value.Status);
        Assert.Equal(RegistrationNextSteps.VerifyMobileOtp, result.Value.NextStep);
        Assert.Equal("******6675", result.Value.MaskedMobile);

        var entity = await ctx.GetRegistrationAsync(result.Value.RegistrationId.Value);
        Assert.NotNull(entity);
        Assert.Equal("Mariam Abdul Rashid", entity!.CustomerName);
        Assert.Equal("880214566831", entity.ICNumber);
        Assert.Equal("0163386675", entity.MobileNumber);
        Assert.Equal("mariam@email.com", entity.Email);
        Assert.Matches("^\\d{4}$", entity.MobileOtp!);
        Assert.Equal(0, entity.OtpAttempts);
        Assert.True(entity.OtpExpiry > DateTime.UtcNow);
    }

    [Theory]
    [InlineData("880214566831", "0163386675", "mariam@email.com")]
    [InlineData("880214566832", "0163386675", "other@email.com")]
    [InlineData("880214566832", "0163386676", "mariam@email.com")]
    public async Task CompletedAccount_ReturnsAccountAlreadyExists(string ic, string mobile, string email)
    {
        using var ctx = new RegistrationTestContext();
        await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.Completed,
            ic: ic,
            mobile: mobile,
            email: email);

        var result = await ctx.Service.StartRegistrationAsync(Request(ic: ic, mobile: mobile, email: email));

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_ALREADY_EXISTS", result.Code);
        Assert.Empty(ctx.Db.OtpVerifications);
        Assert.Single(await ctx.GetRegistrationsByIcAsync(ic));
    }

    [Fact]
    public async Task PendingRegistration_IsNotBlockedByCurrentImplementation()
    {
        using var ctx = new RegistrationTestContext();
        await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingOtpMobile);

        var result = await ctx.Service.StartRegistrationAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal("REGISTRATION_STARTED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingOtpMobile, result.Value!.Status);

        var sessions = await ctx.GetRegistrationsByIcAsync("880214566831");
        Assert.Equal(2, sessions.Count);
    }

    [Theory]
    [InlineData("", "880214566831", "0163386675", "mariam@email.com")]
    [InlineData("Mariam", "123", "0163386675", "mariam@email.com")]
    [InlineData("Mariam", "880214566831", "", "mariam@email.com")]
    [InlineData("Mariam", "880214566831", "0163386675", "invalid")]
    public async Task InvalidRequest_ReturnsValidationError(
        string name, string ic, string mobile, string email)
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.StartRegistrationAsync(new StartRegistrationRequest(name, ic, mobile, email));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(ctx.Db.Registrations);
    }
}
