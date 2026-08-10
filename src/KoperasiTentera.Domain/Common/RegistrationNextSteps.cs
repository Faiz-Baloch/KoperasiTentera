using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Domain.Common
{
    public static class RegistrationNextSteps
    {
        public const string EnterDetails = "EnterDetails";
        public const string VerifyMobileOtp = "VerifyMobileOtp";
        public const string VerifyEmailOtp = "VerifyEmailOtp";
        public const string AcceptPrivacyPolicy = "AcceptPrivacyPolicy";
        public const string SetPin = "SetPin";
        public const string VerifyFace = "VerifyFace";
        public const string Completed = "Completed";
    }
}
