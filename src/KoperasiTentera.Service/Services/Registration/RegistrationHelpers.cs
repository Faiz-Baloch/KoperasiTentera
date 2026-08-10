using System.Security.Cryptography;
using System.Text;

namespace KoperasiTentera.Service.Services.Registration
{
    internal static class RegistrationHelpers
    {
        /// <summary>How long a generated OTP stays valid.</summary>
        public static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(5);

        /// <summary>Max wrong attempts allowed against a single OTP before a resend is required.</summary>
        public const int MaxOtpAttempts = 3;

        /// <summary>Generates a secure random 4-digit OTP, e.g. "0483".</summary>
        public static string GenerateOtp() => RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4");

        public static string HashOtp(string otp)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
            return Convert.ToHexString(bytes);
        }

        public static bool VerifyOtpHash(string plainOtp, string hash) =>
            HashOtp(plainOtp) == hash;


        /// <summary>"0123456789" → "******6789".</summary>
        public static string MaskMobile(string mobile)
        {
            if (string.IsNullOrEmpty(mobile))
                return string.Empty;

            if (mobile.Length <= 4)
                return new string('*', mobile.Length);

            var visible = mobile[^4..];
            return new string('*', mobile.Length - 4) + visible;
        }

        /// <summary>"maria@example.com" → "ma•••@••••.com".</summary>
        public static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
                return "•••";

            var localPart = email[..atIndex];
            var domainPart = email[(atIndex + 1)..];

            var visibleLocal = localPart.Length >= 2 ? localPart[..2] : localPart;

            var dotIndex = domainPart.LastIndexOf('.');
            var domainSuffix = dotIndex >= 0 ? domainPart[dotIndex..] : string.Empty;

            return $"{visibleLocal}•••@••••{domainSuffix}";
        }
    }
}
