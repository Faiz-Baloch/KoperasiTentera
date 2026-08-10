using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Domain.Common
{
    public static class RegistrationStatuses
    {
        public const string PendingOtpMobile = "PendingOtpMobile";
        public const string PendingOtpEmail = "PendingOtpEmail";
        public const string PendingPrivacyPolicy = "PendingPrivacyPolicy";
        public const string PendingFaceVerification = "PendingFaceVerification";
        public const string PendingPin = "PendingPin";
        public const string Completed = "Completed";
    }
}
