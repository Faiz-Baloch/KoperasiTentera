using FluentValidation;
using KoperasiTentera.Service.DTOs.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.Validators.Registration
{
    public class CheckIcRequestValidator : AbstractValidator<CheckIcRequest>
    {
        public CheckIcRequestValidator()
        {
            RuleFor(x => x.ICNumber)
                .NotEmpty().WithErrorCode("IC_NUMBER_REQUIRED")
                .Length(12).WithErrorCode("IC_NUMBER_INVALID")
                .Matches(@"^\d{12}$").WithErrorCode("IC_NUMBER_FORMAT");
        }
    }
}
