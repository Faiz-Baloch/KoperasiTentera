using KoperasiTentera.Service.DTOs.Registration; 
using FluentValidation;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class AcceptPrivacyPolicyRequestValidator : AbstractValidator<AcceptPrivacyPolicyRequest>
    {
        public AcceptPrivacyPolicyRequestValidator()
        {
            RuleFor(x => x.RegistrationId)
                .NotEmpty().WithErrorCode("REGISTRATION_ID_REQUIRED");
        }
    }
}
