using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Services.Registration;
using KoperasiTentera.UnitTests.TestSupport;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class PinTests
{
    private static SetPinRequest Request(Guid id, string pin = "135790", string? confirm = null) =>
        new(id, pin, confirm ?? pin);

    [Fact]
    public async Task ValidPin_IsHashedAndCompletesRegistration()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPin);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("PIN_SET", result.Code);
        Assert.Equal(RegistrationStatuses.Completed, result.Value!.Status);
        Assert.Equal(RegistrationNextSteps.Completed, result.Value.NextStep);

        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.NotNull(db!.PinHash);
        Assert.NotEqual("135790", db.PinHash);
        Assert.True(PinHasher.Verify("135790", db.PinHash));
        Assert.False(PinHasher.Verify("135791", db.PinHash));
    }

    [Fact]
    public async Task PinMismatch_ReturnsBusinessErrorAndDoesNotPersistPin()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPin);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id, "135790", "135791"));

        Assert.False(result.IsSuccess);
        Assert.Equal("PIN_MISMATCH", result.Code);
        var db = await ctx.GetRegistrationAsync(registration.Id);
        Assert.Null(db!.PinHash);
        Assert.Equal(RegistrationStatuses.PendingPin, db.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345A")]
    public async Task InvalidPin_ReturnsValidationError(string pin)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPin);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id, pin, pin));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Null((await ctx.GetRegistrationAsync(registration.Id))!.PinHash);
    }

    [Fact]
    public async Task EmptyConfirmation_ReturnsValidationError()
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: RegistrationStatuses.PendingPin);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id, "135790", ""));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Contains(result.Errors, x => x.Code == "CONFIRM_PIN_REQUIRED");
    }

    [Fact]
    public async Task UnknownRegistration_ReturnsNotFound()
    {
        using var ctx = new RegistrationTestContext();

        var result = await ctx.Service.SetPinAsync(Request(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Code);
    }

    [Theory]
    [InlineData(RegistrationStatuses.PendingOtpMobile)]
    [InlineData(RegistrationStatuses.PendingOtpEmail)]
    [InlineData(RegistrationStatuses.PendingPrivacyPolicy)]
    [InlineData(RegistrationStatuses.Completed)]
    public async Task ExistingRegistrationInAnyStatus_CanBeCompletedByCurrentService(string status)
    {
        using var ctx = new RegistrationTestContext();
        var registration = await ctx.AddRegistrationAsync(status: status);

        var result = await ctx.Service.SetPinAsync(Request(registration.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatuses.Completed, result.Value!.Status);
    }
}
