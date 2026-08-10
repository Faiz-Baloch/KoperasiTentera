using KoperasiTentera.Service.DTOs.Registration; 
using KoperasiTentera.Domain.Common;
using FluentValidation;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
    {
        public SendOtpRequestValidator()
        {
            RuleFor(x => x.RegistrationId)
                .NotEmpty().WithErrorCode("REGISTRATION_ID_REQUIRED");

            RuleFor(x => x.Channel)
                .NotEmpty().WithErrorCode("CHANNEL_REQUIRED")
                .Must(c => c.Equals(OtpChannels.Mobile, System.StringComparison.OrdinalIgnoreCase)
                        || c.Equals(OtpChannels.Email, System.StringComparison.OrdinalIgnoreCase))
                .WithErrorCode("CHANNEL_INVALID")
                .WithMessage("Channel must be either 'Mobile' or 'Email'.");
        }
    }
}
