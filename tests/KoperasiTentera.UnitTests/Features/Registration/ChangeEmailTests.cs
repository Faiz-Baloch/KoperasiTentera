using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class ChangeEmailTests
{
    [Fact]
    public async Task ValidEmailChange_UpdatesEmailResetsVerificationAndCreatesLegacyOtp()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(
            status: RegistrationStatuses.PendingOtpEmail,
            email: "old@email.com",
            emailVerified: true);

        var result = await ctx.Service.ChangeEmailAsync(
            new ChangeEmailRequest(registration.Id, "new@email.com"));

        Assert.True(result.IsSuccess);
        Assert.Equal("EMAIL_CHANGED", result.Code);
        Assert.Equal(RegistrationStatuses.PendingOtpEmail, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.VerifyEmailOtp, result.Value.NextStep);
        Assert.Equal("ne•••@••••.com", result.Value.MaskedEmail);

        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.Equal("new@email.com", db!.Email);
        Assert.False(db.IsEmailVerified);
        Assert.Matches("^\\d{4}$", db.EmailOtp!);
        Assert.True(db.OtpExpiry > DateTime.UtcNow);
        Assert.Equal(0, db.OtpAttempts);
        Assert.Empty(ctx.Db.OtpVerifications);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-email")]
    public async Task InvalidEmail_ReturnsValidationError(string email)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(email: "old@email.com");

        var result = await ctx.Service.ChangeEmailAsync(new ChangeEmailRequest(registration.Id, email));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Equal("old@email.com", (await ctx.GetRegistrationAsync(registration.Id))!.Email);
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.ChangeEmailAsync(new ChangeEmailRequest(Guid.NewGuid(), "new@email.com"));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Fact]
    public async Task EmailChange_PreservesExactInputWhitespace()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync();

        var result = await ctx.Service.ChangeEmailAsync(new ChangeEmailRequest(registration.Id, "  new@email.com  "));

        // Current service assigns request.Email directly; it does not trim it.
        Assert.True(result.IsSuccess);
        Assert.Equal("  new@email.com  ", (await ctx.GetRegistrationAsync(registration.Id))!.Email);
    }
}
