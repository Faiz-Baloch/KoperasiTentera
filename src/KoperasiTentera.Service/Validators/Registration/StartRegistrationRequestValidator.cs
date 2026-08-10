using FluentValidation;
using KoperasiTentera.Service.DTOs.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class StartRegistrationRequestValidator : AbstractValidator<StartRegistrationRequest>
    {
        public StartRegistrationRequestValidator()
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithErrorCode("CUSTOMER_NAME_REQUIRED")
                .MaximumLength(100).WithErrorCode("CUSTOMER_NAME_TOO_LONG");

            RuleFor(x => x.ICNumber)
                .NotEmpty().WithErrorCode("IC_NUMBER_REQUIRED")
                .Length(12).WithErrorCode("IC_NUMBER_INVALID")
                .Matches(@"^\d{12}$").WithErrorCode("IC_NUMBER_FORMAT");

            RuleFor(x => x.MobileNumber)
                .NotEmpty().WithErrorCode("MOBILE_REQUIRED")
                .Matches(@"^01[0-9]{8,9}$").WithErrorCode("MOBILE_INVALID");

            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode("EMAIL_REQUIRED")
                .EmailAddress().WithErrorCode("EMAIL_INVALID")
                .MaximumLength(150).WithErrorCode("EMAIL_TOO_LONG");
        }
    }
}
