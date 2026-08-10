using KoperasiTentera.Service.DTOs.Registration; 
using FluentValidation;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class SetPinRequestValidator : AbstractValidator<SetPinRequest>
    {
        public SetPinRequestValidator()
        {
            RuleFor(x => x.RegistrationId)
                .NotEmpty().WithErrorCode("REGISTRATION_ID_REQUIRED");

            RuleFor(x => x.Pin)
                .NotEmpty().WithErrorCode("PIN_REQUIRED")
                .Matches(@"^\d{6}$").WithErrorCode("PIN_FORMAT");

            RuleFor(x => x.ConfirmPin)
                .NotEmpty().WithErrorCode("CONFIRM_PIN_REQUIRED")
                .Matches(@"^\d{6}$").WithErrorCode("CONFIRM_PIN_FORMAT");

            // Note: Pin vs ConfirmPin equality is deliberately checked in
            // RegistrationService (not here) so a mismatch surfaces as the
            // dedicated PIN_MISMATCH business code rather than VALIDATION_FAILED.
        }
    }
}
