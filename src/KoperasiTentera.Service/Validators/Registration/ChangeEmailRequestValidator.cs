using KoperasiTentera.Service.DTOs.Registration; 
using FluentValidation;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
    {
        public ChangeEmailRequestValidator()
        {
            RuleFor(x => x.RegistrationId)
                .NotEmpty()
                .WithErrorCode("REGISTRATION_ID_REQUIRED");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithErrorCode("EMAIL_REQUIRED")
                .EmailAddress()
                .WithErrorCode("EMAIL_INVALID")
                .MaximumLength(150)
                .WithErrorCode("EMAIL_TOO_LONG");
        }
    }
}
