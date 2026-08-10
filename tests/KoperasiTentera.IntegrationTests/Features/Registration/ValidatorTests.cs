using KoperasiTentera.Domain.Common;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Validators.Registration;

namespace KoperasiTentera.UnitTests.Features.Registration;

public class ValidatorTests
{
    [Fact]
    public async Task CheckIcValidator_AcceptsValidIc()
    {
        var result = await new CheckIcRequestValidator().ValidateAsync(
            new CheckIcRequest("880214566831"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("88021456683A")]
    public async Task CheckIcValidator_RejectsInvalidIc(string ic)
    {
        var result = await new CheckIcRequestValidator().ValidateAsync(new CheckIcRequest(ic));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task StartRegistrationValidator_AcceptsValidRequest()
    {
        var result = await new StartRegistrationRequestValidator().ValidateAsync(
            new StartRegistrationRequest("Mariam", "880214566831", "0163386675", "mariam@email.com"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "880214566831", "0163386675", "mariam@email.com")]
    [InlineData("Mariam", "123", "0163386675", "mariam@email.com")]
    [InlineData("Mariam", "880214566831", "", "mariam@email.com")]
    [InlineData("Mariam", "880214566831", "0163386675", "invalid")]
    public async Task StartRegistrationValidator_RejectsInvalidRequest(
        string name, string ic, string mobile, string email)
    {
        var result = await new StartRegistrationRequestValidator().ValidateAsync(
            new StartRegistrationRequest(name, ic, mobile, email));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task SendOtpValidator_OnlyAcceptsMobileOrEmail()
    {
        var validator = new SendOtpRequestValidator();

        Assert.True((await validator.ValidateAsync(
            new SendOtpRequest(Guid.NewGuid(), OtpChannels.Mobile))).IsValid);
        Assert.True((await validator.ValidateAsync(
            new SendOtpRequest(Guid.NewGuid(), OtpChannels.Email))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new SendOtpRequest(Guid.NewGuid(), "SMS"))).IsValid);
    }

    [Fact]
    public async Task VerifyOtpValidator_RequiresFourDigits()
    {
        var validator = new VerifyOtpRequestValidator();

        Assert.True((await validator.ValidateAsync(
            new VerifyOtpRequest(Guid.NewGuid(), "7981", OtpChannels.Mobile))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new VerifyOtpRequest(Guid.NewGuid(), "798", OtpChannels.Mobile))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new VerifyOtpRequest(Guid.NewGuid(), "79812", OtpChannels.Mobile))).IsValid);
    }

    [Fact]
    public async Task ChangeEmailValidator_RequiresValidEmail()
    {
        var validator = new ChangeEmailRequestValidator();

        Assert.True((await validator.ValidateAsync(
            new ChangeEmailRequest(Guid.NewGuid(), "new@email.com"))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new ChangeEmailRequest(Guid.NewGuid(), "bad"))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new ChangeEmailRequest(Guid.NewGuid(), ""))).IsValid);
    }

    [Fact]
    public async Task SetPinValidator_RequiresSixNumericDigits()
    {
        var validator = new SetPinRequestValidator();

        Assert.True((await validator.ValidateAsync(
            new SetPinRequest(Guid.NewGuid(), "135790", "135790"))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new SetPinRequest(Guid.NewGuid(), "12345", "12345"))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new SetPinRequest(Guid.NewGuid(), "12345A", "12345A"))).IsValid);
    }

    [Fact]
    public async Task PrivacyValidator_RequiresRegistrationId()
    {
        var result = await new AcceptPrivacyPolicyRequestValidator().ValidateAsync(
            new AcceptPrivacyPolicyRequest(Guid.Empty, true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorCode == "REGISTRATION_ID_REQUIRED");
    }
}
