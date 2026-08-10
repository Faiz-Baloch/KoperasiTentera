using KoperasiTentera.Application.Common.Results;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Responses.Registration;

namespace KoperasiTentera.Service.Services.Registration
{
    public interface IRegistrationService
    {
        Task<Result<RegistrationResponse>> CheckIcAsync(
            CheckIcRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> StartRegistrationAsync(
            StartRegistrationRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> SendOtpAsync(
            SendOtpRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> VerifyOtpAsync(
            VerifyOtpRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> ChangeEmailAsync(
            ChangeEmailRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> AcceptPrivacyPolicyAsync(
            AcceptPrivacyPolicyRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> SetPinAsync(
            SetPinRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<RegistrationResponse>> VerifyFaceAsync(
     VerifyFaceRequest request,
     CancellationToken cancellationToken = default);
    }
}
