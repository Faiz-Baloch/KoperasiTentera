using KoperasiTentera.Service.DTOs.Registration;  
using KoperasiTentera.Domain.Common;
using FluentValidation;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
    {
        public VerifyOtpRequestValidator()
        {
            RuleFor(x => x.RegistrationId)
                .NotEmpty().WithErrorCode("REGISTRATION_ID_REQUIRED");

            RuleFor(x => x.Otp)
                .NotEmpty().WithErrorCode("OTP_REQUIRED")
                .Matches(@"^\d{4}$").WithErrorCode("OTP_FORMAT");

            RuleFor(x => x.Channel)
                .NotEmpty().WithErrorCode("CHANNEL_REQUIRED")
                .Must(c => c.Equals(OtpChannels.Mobile, System.StringComparison.OrdinalIgnoreCase)
                        || c.Equals(OtpChannels.Email, System.StringComparison.OrdinalIgnoreCase))
                .WithErrorCode("CHANNEL_INVALID")
                .WithMessage("Channel must be either 'Mobile' or 'Email'.");
        }
    }
}
