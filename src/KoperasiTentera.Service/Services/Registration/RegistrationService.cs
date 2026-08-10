using AutoMapper;
using FluentValidation;
using KoperasiTentera.Application.Abstractions.Persistence.Repositories;
using KoperasiTentera.Application.Common.Results;
using KoperasiTentera.Application.Persistence;
using KoperasiTentera.Domain.Common;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Responses.Registration;
using Microsoft.Extensions.Logging; 

namespace KoperasiTentera.Service.Services.Registration
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IRepository<Registrations> _repository;
        private readonly IRepository<OtpVerification> _otpRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RegistrationService> _logger;

        private readonly IValidator<CheckIcRequest> _checkIcValidator;
        private readonly IValidator<StartRegistrationRequest> _startRegistrationValidator;
        private readonly IValidator<SendOtpRequest> _sendOtpValidator;
        private readonly IValidator<VerifyOtpRequest> _verifyOtpValidator;
        private readonly IValidator<ChangeEmailRequest> _changeEmailValidator;
        private readonly IValidator<AcceptPrivacyPolicyRequest> _acceptPrivacyPolicyValidator;
        private readonly IValidator<SetPinRequest> _setPinValidator;

        public RegistrationService(
            IRepository<Registrations> repository,
            IRepository<OtpVerification> otpVerifications,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<RegistrationService> logger,
            IValidator<CheckIcRequest> checkIcValidator,
            IValidator<StartRegistrationRequest> startRegistrationValidator,
            IValidator<SendOtpRequest> sendOtpValidator,
            IValidator<VerifyOtpRequest> verifyOtpValidator,
            IValidator<ChangeEmailRequest> changeEmailValidator,
            IValidator<AcceptPrivacyPolicyRequest> acceptPrivacyPolicyValidator,
            IValidator<SetPinRequest> setPinValidator)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;

            _checkIcValidator = checkIcValidator;
            _startRegistrationValidator = startRegistrationValidator;
            _sendOtpValidator = sendOtpValidator;
            _verifyOtpValidator = verifyOtpValidator;
            _changeEmailValidator = changeEmailValidator;
            _acceptPrivacyPolicyValidator = acceptPrivacyPolicyValidator;
            _setPinValidator = setPinValidator;
            _otpRepository = otpVerifications;
        }
 
        public async Task<Result<RegistrationResponse>> CheckIcAsync(
    CheckIcRequest request,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: CheckIcAsync started for IC number {ICNumber}.",
                request.ICNumber);

            // ============================================================
            // 1. Validate Request
            // ============================================================

            var validationResult = await ValidateAsync(
                _checkIcValidator,
                request,
                cancellationToken);

            if (validationResult is not null)
            {
                _logger.LogWarning(
                    "Registration: CheckIcAsync validation failed for IC number {ICNumber}.",
                    request.ICNumber);

                return validationResult;
            }


            // ============================================================
            // 2. Check for Existing Completed Account
            // ============================================================

            var existing = await _repository.FirstOrDefaultAsync(
                r => r.ICNumber == request.ICNumber,
                cancellationToken);


            // ============================================================
            // 3. New User Flow
            // ============================================================

            if (existing is null)
            {
                _logger.LogInformation(
                    "Registration: IC number {ICNumber} is not registered. Starting new user registration flow.",
                    request.ICNumber);

                var response = new RegistrationResponse
                {
                    RegistrationId = null,
                    Status = string.Empty,
                    NextStep = RegistrationNextSteps.EnterDetails,
                    Message = "IC number is not registered. Please enter your details to continue."
                };

                return Result<RegistrationResponse>.Success(
                    response,
                    "IC_NOT_EXISTS",
                    response.Message);
            }


            // ============================================================
            // 4. Existing User Flow
            // Generate Mobile OTP Automatically
            // ============================================================

            _logger.LogInformation(
                "Registration: Existing completed account found. RegistrationId={RegistrationId}. Generating mobile OTP.",
                existing.Id);

            var plainOtp = await GenerateAndStoreOtpAsync(
                existing.Id,
                OtpChannels.Mobile,
                cancellationToken);

            existing.Status = RegistrationStatuses.PendingOtpMobile;
            _repository.Update(existing);
            // ============================================================
            // 5. Save OTP
            // ============================================================

            await _unitOfWork.SaveChangesAsync(cancellationToken);


            // ============================================================
            // 6. Send OTP
            // Development only
            // Replace with SMS provider in production
            // ============================================================

            LogOtpForDevelopment(
                existing.Id,
                OtpChannels.Mobile,
                plainOtp);


            _logger.LogInformation(
                "Registration: Existing user mobile OTP generated successfully. RegistrationId={RegistrationId}, Mobile={MaskedMobile}.",
                existing.Id,
                RegistrationHelpers.MaskMobile(existing.MobileNumber));


            // ============================================================
            // 7. Prepare Response
            // ============================================================

            var existingUserResponse = new RegistrationResponse
            {
                RegistrationId = existing.Id,
                Status = existing.Status,
                NextStep = RegistrationNextSteps.VerifyMobileOtp,
                MaskedMobile = RegistrationHelpers.MaskMobile(
                    existing.MobileNumber),

                // Email is not needed yet.
                // It will be shown after mobile OTP verification.
                MaskedEmail = null,

                Message = "Your account was found. A verification code has been sent to your registered mobile number."
            };


            return Result<RegistrationResponse>.Success(
                existingUserResponse,
                "IC_EXISTS",
                existingUserResponse.Message);
        }
    
        public async Task<Result<RegistrationResponse>> StartRegistrationAsync(
       StartRegistrationRequest request,
       CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: StartRegistrationAsync started for IC number {ICNumber}.",
                request.ICNumber);


            // ============================================================
            // 1. Validate Request
            // ============================================================

            var validationResult = await ValidateAsync(
                _startRegistrationValidator,
                request,
                cancellationToken);

            if (validationResult is not null)
            {
                _logger.LogWarning(
                    "Registration: StartRegistrationAsync validation failed for IC number {ICNumber}.",
                    request.ICNumber);

                return validationResult;
            }


            // ============================================================
            // 2. Check Existing Completed Account
            // ============================================================

            var existingAccount = await _repository.FirstOrDefaultAsync(
                r => r.ICNumber == request.ICNumber
                     && r.Status == RegistrationStatuses.Completed,
                cancellationToken);

            if (existingAccount is not null)
            {
                _logger.LogWarning(
                    "Registration: StartRegistrationAsync blocked. Completed account already exists. RegistrationId={RegistrationId}, ICNumber={ICNumber}.",
                    existingAccount.Id,
                    request.ICNumber);

                return Result<RegistrationResponse>.Failure(
                    "ACCOUNT_ALREADY_EXISTS",
                    "An account already exists with this IC number. Please continue by verifying your registered mobile number.");
            }


            // ============================================================
            // 3. Check Existing Pending Registration
            // Prevent duplicate registration records
            // ============================================================

            var pendingRegistration = await _repository.FirstOrDefaultAsync(
                r => r.ICNumber == request.ICNumber
                     && r.Status != RegistrationStatuses.Completed,
                cancellationToken);

            if (pendingRegistration is not null)
            {
                _logger.LogInformation(
                    "Registration: Existing pending registration found. RegistrationId={RegistrationId}, Status={Status}.",
                    pendingRegistration.Id,
                    pendingRegistration.Status);

                return Result<RegistrationResponse>.Failure(
                    "REGISTRATION_ALREADY_IN_PROGRESS",
                    "A registration is already in progress for this IC number. Please continue your existing registration.");
            }


            // ============================================================
            // 4. Create New Registration
            // ============================================================

            var registration = _mapper.Map<Registrations>(request);

            registration.Status = RegistrationStatuses.PendingOtpMobile;

            registration.IsMobileVerified = false;
            registration.IsEmailVerified = false;
            registration.IsPrivacyAccepted = false;


            await _repository.AddAsync(
                registration,
                cancellationToken);


            _logger.LogInformation(
                "Registration: New registration created. RegistrationId={RegistrationId}, ICNumber={ICNumber}.",
                registration.Id,
                registration.ICNumber);


            // ============================================================
            // 5. Generate Mobile OTP Automatically
            // ============================================================

            var plainOtp = await GenerateAndStoreOtpAsync(
                registration.Id,
                OtpChannels.Mobile,
                cancellationToken);


            // ============================================================
            // 6. Save Registration + OTP
            // ============================================================

            await _unitOfWork.SaveChangesAsync(cancellationToken);


            // ============================================================
            // 7. Send Mobile OTP
            // Development only
            // ============================================================

            LogOtpForDevelopment(
                registration.Id,
                OtpChannels.Mobile,
                plainOtp);


            _logger.LogInformation(
                "Registration: Registration started successfully and mobile OTP generated. RegistrationId={RegistrationId}, Mobile={MaskedMobile}.",
                registration.Id,
                RegistrationHelpers.MaskMobile(registration.MobileNumber));


            // ============================================================
            // 8. Prepare Response
            // ============================================================

            var response = new RegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status,
                NextStep = RegistrationNextSteps.VerifyMobileOtp,

                MaskedMobile = RegistrationHelpers.MaskMobile(
                    registration.MobileNumber),

                // Email verification happens after Mobile OTP verification
                MaskedEmail = null,

                Message = "Registration started successfully. A verification code has been sent to your mobile number."
            };


            return Result<RegistrationResponse>.Success(
                response,
                "REGISTRATION_STARTED",
                response.Message);
        }
        public async Task<Result<RegistrationResponse>> SendOtpAsync(
    SendOtpRequest request,
    CancellationToken cancellationToken = default)
        {
            var validation = await _sendOtpValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new ApiError
                {
                    Field = e.PropertyName,
                    Code = e.ErrorCode,
                    Message = e.ErrorMessage
                });
                return Result<RegistrationResponse>.ValidationFailed(errors);
            }

            var registration = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
            if (registration is null)
                return Result<RegistrationResponse>.NotFound("Registration not found.");

            var channel = request.Channel.Trim();

            // Purane active OTPs ko expire/mark used kar do (same channel)
            var oldOtps = await _otpRepository.FindAsync(
                x => x.RegistrationId == registration.Id
                     && x.Channel == channel
                     && !x.IsUsed,
                cancellationToken);

            foreach (var old in oldOtps)
            {
                old.IsUsed = true;
                _otpRepository.Update(old);
            }

            // Naya OTP generate + save
            var plainOtp = RegistrationHelpers.GenerateOtp();
            var otpEntity = new OtpVerification
            {
                RegistrationId = registration.Id,
                Channel = channel,
                OtpHash = RegistrationHelpers.HashOtp(plainOtp),
                ExpiresAtUtc = DateTime.UtcNow.Add(RegistrationHelpers.OtpValidity),
                Attempts = 0,
                IsUsed = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _otpRepository.AddAsync(otpEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Development logging only
            _logger.LogInformation(
                "OTP generated. RegistrationId={RegistrationId}, Channel={Channel}, Otp={Otp}",
                registration.Id, channel, plainOtp);

            var response = new RegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status,
                NextStep = channel == OtpChannels.Mobile
                    ? RegistrationNextSteps.VerifyMobileOtp
                    : RegistrationNextSteps.VerifyEmailOtp,
                MaskedMobile = RegistrationHelpers.MaskMobile(registration.MobileNumber),
                MaskedEmail = RegistrationHelpers.MaskEmail(registration.Email),
                Message = "OTP has been sent successfully."
            };

            return Result<RegistrationResponse>.Success(response, "OTP_SENT", "OTP has been sent successfully.");
        }
 

        public async Task<Result<RegistrationResponse>> VerifyOtpAsync(
            VerifyOtpRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: VerifyOtpAsync started. RegistrationId={RegistrationId}, Channel={Channel}.",
                request.RegistrationId,
                request.Channel);

            // ============================================================
            // 1. Validate Request
            // ============================================================

            var validation = await _verifyOtpValidator.ValidateAsync(
                request,
                cancellationToken);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new ApiError
                {
                    Field = e.PropertyName,
                    Code = e.ErrorCode,
                    Message = e.ErrorMessage
                });

                _logger.LogWarning(
                    "Registration: OTP verification validation failed. RegistrationId={RegistrationId}, Channel={Channel}.",
                    request.RegistrationId,
                    request.Channel);

                return Result<RegistrationResponse>.ValidationFailed(errors);
            }


            // ============================================================
            // 2. Get Registration
            // ============================================================

            var registration = await _repository.GetByIdAsync(
                request.RegistrationId,
                cancellationToken);

            if (registration is null)
            {
                _logger.LogWarning(
                    "Registration: OTP verification failed. Registration not found. RegistrationId={RegistrationId}.",
                    request.RegistrationId);

                return Result<RegistrationResponse>.NotFound(
                    "Registration not found.");
            }


            // ============================================================
            // 3. Normalize and Validate Channel
            // ============================================================

            var channel = request.Channel.Trim();

            if (channel.Equals(OtpChannels.Mobile, StringComparison.OrdinalIgnoreCase))
            {
                channel = OtpChannels.Mobile;
            }
            else if (channel.Equals(OtpChannels.Email, StringComparison.OrdinalIgnoreCase))
            {
                channel = OtpChannels.Email;
            }
            else
            {
                _logger.LogWarning(
                    "Registration: OTP verification failed. Invalid channel. RegistrationId={RegistrationId}, Channel={Channel}.",
                    registration.Id,
                    request.Channel);

                return Result<RegistrationResponse>.Failure(
                    "INVALID_CHANNEL",
                    "Invalid OTP verification channel.");
            }


            // ============================================================
            // 4. Validate Registration State
            // Prevent wrong OTP step verification
            // ============================================================

            if (channel == OtpChannels.Mobile &&
                registration.Status != RegistrationStatuses.PendingOtpMobile)
            {
                _logger.LogWarning(
                    "Registration: Mobile OTP verification attempted in invalid state. RegistrationId={RegistrationId}, Status={Status}.",
                    registration.Id,
                    registration.Status);

                return Result<RegistrationResponse>.Failure(
                    "INVALID_REGISTRATION_STATE",
                    "Mobile verification is not available at the current registration step.");
            }

            if (channel == OtpChannels.Email &&
                registration.Status != RegistrationStatuses.PendingOtpEmail)
            {
                _logger.LogWarning(
                    "Registration: Email OTP verification attempted in invalid state. RegistrationId={RegistrationId}, Status={Status}.",
                    registration.Id,
                    registration.Status);

                return Result<RegistrationResponse>.Failure(
                    "INVALID_REGISTRATION_STATE",
                    "Email verification is not available at the current registration step.");
            }


            // ============================================================
            // 5. Find Latest Active OTP
            // ============================================================

            var otpList = await _otpRepository.FindAsync(
                x => x.RegistrationId == registration.Id
                     && x.Channel == channel
                     && !x.IsUsed,
                cancellationToken);

            var otpRecord = otpList
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();


            if (otpRecord is null)
            {
                _logger.LogWarning(
                    "Registration: No active OTP found. RegistrationId={RegistrationId}, Channel={Channel}.",
                    registration.Id,
                    channel);

                return Result<RegistrationResponse>.Failure(
                    "OTP_EXPIRED",
                    "No active verification code was found. Please request a new code.");
            }


            // ============================================================
            // 6. Check OTP Expiry
            // ============================================================

            if (otpRecord.ExpiresAtUtc <= DateTime.UtcNow)
            {
                otpRecord.IsUsed = true;
                _otpRepository.Update(otpRecord);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Registration: OTP expired. RegistrationId={RegistrationId}, Channel={Channel}, OtpId={OtpId}.",
                    registration.Id,
                    channel,
                    otpRecord.Id);

                return Result<RegistrationResponse>.Failure(
                    "OTP_EXPIRED",
                    "Your verification code has expired. Please request a new code.");
            }


            // ============================================================
            // 7. Check Maximum Attempts
            // ============================================================

            if (otpRecord.Attempts >= RegistrationHelpers.MaxOtpAttempts)
            {
                otpRecord.IsUsed = true;
                _otpRepository.Update(otpRecord);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Registration: Maximum OTP attempts exceeded. RegistrationId={RegistrationId}, Channel={Channel}, Attempts={Attempts}.",
                    registration.Id,
                    channel,
                    otpRecord.Attempts);

                return Result<RegistrationResponse>.Failure(
                    "OTP_MAX_ATTEMPTS_EXCEEDED",
                    "Maximum verification attempts exceeded. Please request a new verification code.");
            }


            // ============================================================
            // 8. Verify OTP Hash
            // ============================================================

            var isValidOtp = RegistrationHelpers.VerifyOtpHash(
                request.Otp,
                otpRecord.OtpHash);

            if (!isValidOtp)
            {
                otpRecord.Attempts++;

                var remainingAttempts =
                    Math.Max(
                        0,
                        RegistrationHelpers.MaxOtpAttempts - otpRecord.Attempts);

                if (otpRecord.Attempts >= RegistrationHelpers.MaxOtpAttempts)
                {
                    otpRecord.IsUsed = true;
                }

                _otpRepository.Update(otpRecord);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Registration: Invalid OTP entered. RegistrationId={RegistrationId}, Channel={Channel}, Attempts={Attempts}, RemainingAttempts={RemainingAttempts}.",
                    registration.Id,
                    channel,
                    otpRecord.Attempts,
                    remainingAttempts);

                if (remainingAttempts == 0)
                {
                    return Result<RegistrationResponse>.Failure(
                        "OTP_MAX_ATTEMPTS_EXCEEDED",
                        "Maximum verification attempts exceeded. Please request a new verification code.");
                }

                return Result<RegistrationResponse>.Failure(
                    "INVALID_OTP",
                    $"Incorrect verification code. You have {remainingAttempts} attempt(s) remaining.");
            }


            // ============================================================
            // 9. OTP VERIFIED SUCCESSFULLY
            // ============================================================

            otpRecord.IsUsed = true;
            _otpRepository.Update(otpRecord);


            // ============================================================
            // 10. MOBILE OTP VERIFIED
            // Automatically Generate + Save Email OTP
            // ============================================================

            string? plainEmailOtp = null;

            if (channel == OtpChannels.Mobile)
            {
                registration.IsMobileVerified = true;
                registration.Status = RegistrationStatuses.PendingOtpEmail;


                // --------------------------------------------------------
                // Invalidate any existing active Email OTP
                // --------------------------------------------------------

                var oldEmailOtps = await _otpRepository.FindAsync(
                    x => x.RegistrationId == registration.Id
                         && x.Channel == OtpChannels.Email
                         && !x.IsUsed,
                    cancellationToken);

                foreach (var oldEmailOtp in oldEmailOtps)
                {
                    oldEmailOtp.IsUsed = true;
                    _otpRepository.Update(oldEmailOtp);
                }


                // --------------------------------------------------------
                // Generate New Email OTP Automatically
                // --------------------------------------------------------

                plainEmailOtp = RegistrationHelpers.GenerateOtp();

                var emailOtpVerification = new OtpVerification
                {
                    RegistrationId = registration.Id,
                    Channel = OtpChannels.Email,
                    OtpHash = RegistrationHelpers.HashOtp(plainEmailOtp),
                    ExpiresAtUtc = DateTime.UtcNow.Add(
                        RegistrationHelpers.OtpValidity),
                    Attempts = 0,
                    IsUsed = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _otpRepository.AddAsync(
                    emailOtpVerification,
                    cancellationToken);


                _logger.LogInformation(
                    "Registration: Mobile OTP verified successfully. Email OTP generated automatically. RegistrationId={RegistrationId}.",
                    registration.Id);
            }


            // ============================================================
            // 11. EMAIL OTP VERIFIED
            // ============================================================

            else if (channel == OtpChannels.Email)
            {
                registration.IsEmailVerified = true;
                registration.Status = RegistrationStatuses.PendingPrivacyPolicy;

                _logger.LogInformation(
                    "Registration: Email OTP verified successfully. RegistrationId={RegistrationId}.",
                    registration.Id);
            }


            // ============================================================
            // 12. Update Registration
            // ============================================================

            _repository.Update(registration);


            // ============================================================
            // 13. Single Database Commit
            // Mobile OTP marked used
            // Registration updated
            // Email OTP created automatically if Mobile verified
            // ============================================================

            await _unitOfWork.SaveChangesAsync(cancellationToken);


            // ============================================================
            // 14. Send / Log Email OTP AFTER Successful Commit
            // ============================================================

            if (channel == OtpChannels.Mobile)
            {
                _logger.LogInformation(
                    "Registration: Email verification OTP is ready to be sent. RegistrationId={RegistrationId}, Email={MaskedEmail}.",
                    registration.Id,
                    RegistrationHelpers.MaskEmail(registration.Email));

                // ========================================================
                // DEVELOPMENT ONLY
                // Remove when actual email provider is implemented
                // ========================================================

                LogOtpForDevelopment(
                    registration.Id,
                    OtpChannels.Email,
                    plainEmailOtp!); 
            }


            // ============================================================
            // 15. Prepare Response
            // ============================================================

            var response = new RegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status,

                NextStep = channel == OtpChannels.Mobile
                    ? RegistrationNextSteps.VerifyEmailOtp
                    : RegistrationNextSteps.AcceptPrivacyPolicy,

                MaskedMobile = RegistrationHelpers.MaskMobile(
                    registration.MobileNumber),

                MaskedEmail = RegistrationHelpers.MaskEmail(
                    registration.Email),

                Message = channel == OtpChannels.Mobile
                    ? "Mobile number verified successfully. A verification code has been sent to your email address."
                    : "Email address verified successfully. Please review and accept the privacy policy to continue."
            };


            _logger.LogInformation(
                "Registration: OTP verification completed successfully. RegistrationId={RegistrationId}, Channel={Channel}, NextStep={NextStep}, Status={Status}.",
                registration.Id,
                channel,
                response.NextStep,
                registration.Status);


            return Result<RegistrationResponse>.Success(
                response,
                "OTP_VERIFIED",
                response.Message);
        }
        public async Task<Result<RegistrationResponse>> ChangeEmailAsync(
    ChangeEmailRequest request,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: ChangeEmailAsync started. RegistrationId={RegistrationId}.",
                request.RegistrationId);

            // ============================================================
            // 1. Validate Request
            // ============================================================

            var validationResult = await ValidateAsync(
                _changeEmailValidator,
                request,
                cancellationToken);

            if (validationResult is not null)
            {
                _logger.LogWarning(
                    "Registration: ChangeEmailAsync validation failed. RegistrationId={RegistrationId}.",
                    request.RegistrationId);

                return validationResult;
            }


            // ============================================================
            // 2. Get Registration
            // ============================================================

            var registration = await _repository.GetByIdAsync(
                request.RegistrationId,
                cancellationToken);

            if (registration is null)
            {
                _logger.LogWarning(
                    "Registration: ChangeEmailAsync failed. Registration not found. RegistrationId={RegistrationId}.",
                    request.RegistrationId);

                return Result<RegistrationResponse>.NotFound(
                    "Registration not found.");
            }


            // ============================================================
            // 3. Validate Current Registration State
            // Email can only be changed during Email OTP verification
            // ============================================================

            if (registration.Status != RegistrationStatuses.PendingOtpEmail)
            {
                _logger.LogWarning(
                    "Registration: Email change attempted in invalid state. RegistrationId={RegistrationId}, Status={Status}.",
                    registration.Id,
                    registration.Status);

                return Result<RegistrationResponse>.Failure(
                    "INVALID_REGISTRATION_STATE",
                    "Email address cannot be changed at the current registration step.");
            }


            // ============================================================
            // 4. Update Email
            // ============================================================

            registration.Email = request.Email.Trim();
            registration.IsEmailVerified = false;


            // ============================================================
            // 5. Generate New Email OTP
            // This automatically invalidates previous active Email OTPs
            // ============================================================

            var plainOtp = await GenerateAndStoreOtpAsync(
                registration.Id,
                OtpChannels.Email,
                cancellationToken);


            // ============================================================
            // 6. Update Registration
            // ============================================================

            registration.Status = RegistrationStatuses.PendingOtpEmail;

            _repository.Update(registration);


            // ============================================================
            // 7. Save Everything in One Commit
            // ============================================================

            await _unitOfWork.SaveChangesAsync(cancellationToken);


            // ============================================================
            // 8. Send / Log OTP
            // Replace with actual Email Service in Production
            // ============================================================

            LogOtpForDevelopment(
                registration.Id,
                OtpChannels.Email,
                plainOtp);


            _logger.LogInformation(
                "Registration: Email changed successfully and new Email OTP generated. RegistrationId={RegistrationId}, Email={MaskedEmail}.",
                registration.Id,
                RegistrationHelpers.MaskEmail(registration.Email));


            // ============================================================
            // 9. Prepare Response
            // ============================================================

            var response = new RegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status,
                NextStep = RegistrationNextSteps.VerifyEmailOtp,
                MaskedMobile = RegistrationHelpers.MaskMobile(
                    registration.MobileNumber),
                MaskedEmail = RegistrationHelpers.MaskEmail(
                    registration.Email),
                Message = "Email address updated successfully. A new verification code has been sent to your email address."
            };


            return Result<RegistrationResponse>.Success(
                response,
                "EMAIL_CHANGED",
                response.Message);
        }
    
        public async Task<Result<RegistrationResponse>> AcceptPrivacyPolicyAsync(
            AcceptPrivacyPolicyRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: AcceptPrivacyPolicyAsync started for {RegistrationId}.", request.RegistrationId);

            var validationResult = await ValidateAsync(_acceptPrivacyPolicyValidator, request, cancellationToken);
            if (validationResult is not null)
                return validationResult;

            var registration = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
            if (registration is null)
                return Result<RegistrationResponse>.NotFound("Registration session not found.");

            if (!request.Accepted)
            {
                _logger.LogInformation("Registration: {RegistrationId} declined the privacy policy.", registration.Id);
                return Result<RegistrationResponse>.Failure(
                    "PRIVACY_NOT_ACCEPTED",
                    "You must accept the privacy policy to continue.");
            }

            registration.IsPrivacyAccepted = true;
            registration.Status = RegistrationStatuses.PendingPin;

            _repository.Update(registration);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registration: {RegistrationId} accepted privacy policy.", registration.Id);

            var response = _mapper.Map<RegistrationResponse>(registration);
            response.NextStep = RegistrationNextSteps.SetPin;
            response.Message = "Privacy policy accepted. Please set your 6-digit PIN.";

            return Result<RegistrationResponse>.Success(response, "PRIVACY_ACCEPTED", response.Message);
        }

     


        public async Task<Result<RegistrationResponse>> SetPinAsync(
    SetPinRequest request,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: SetPinAsync started. RegistrationId={RegistrationId}.",
                request.RegistrationId);

            // ============================================================
            // 1. Validate Request
            // ============================================================

            var validationResult = await ValidateAsync(
                _setPinValidator,
                request,
                cancellationToken);

            if (validationResult is not null)
            {
                _logger.LogWarning(
                    "Registration: SetPinAsync validation failed. RegistrationId={RegistrationId}.",
                    request.RegistrationId);

                return validationResult;
            }


            // ============================================================
            // 2. Get Registration
            // ============================================================

            var registration = await _repository.GetByIdAsync(
                request.RegistrationId,
                cancellationToken);

            if (registration is null)
            {
                _logger.LogWarning(
                    "Registration: PIN setup failed. Registration not found. RegistrationId={RegistrationId}.",
                    request.RegistrationId);

                return Result<RegistrationResponse>.NotFound(
                    "Registration not found.");
            }


            // ============================================================
            // 3. Validate Registration State
            // ============================================================

            if (registration.Status != RegistrationStatuses.PendingPin)
            {
                _logger.LogWarning(
                    "Registration: PIN setup attempted in invalid state. RegistrationId={RegistrationId}, Status={Status}.",
                    registration.Id,
                    registration.Status);

                return Result<RegistrationResponse>.Failure(
                    "INVALID_REGISTRATION_STATE",
                    "PIN setup is not available at the current registration step.");
            }


            // ============================================================
            // 4. Validate Previous Registration Steps
            // ============================================================

            if (!registration.IsMobileVerified)
            {
                return Result<RegistrationResponse>.Failure(
                    "MOBILE_NOT_VERIFIED",
                    "Please verify your mobile number before setting your PIN.");
            }

            if (!registration.IsEmailVerified)
            {
                return Result<RegistrationResponse>.Failure(
                    "EMAIL_NOT_VERIFIED",
                    "Please verify your email address before setting your PIN.");
            }

            if (!registration.IsPrivacyAccepted)
            {
                return Result<RegistrationResponse>.Failure(
                    "PRIVACY_NOT_ACCEPTED",
                    "Please accept the privacy policy before setting your PIN.");
            }


            // ============================================================
            // 5. Validate PIN Confirmation
            // ============================================================

            if (!string.Equals(
                    request.Pin,
                    request.ConfirmPin,
                    StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Registration: PIN mismatch. RegistrationId={RegistrationId}.",
                    registration.Id);

                return Result<RegistrationResponse>.Failure(
                    "PIN_MISMATCH",
                    "PIN and confirmation PIN do not match.");
            }


            // ============================================================
            // 6. Store PIN Securely
            // ============================================================

            registration.PinHash = PinHasher.Hash(request.Pin);

            // PIN is complete.
            // Next step = Face Verification.
            registration.Status =
                RegistrationStatuses.PendingFaceVerification;


            _repository.Update(registration);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "Registration: PIN set successfully. RegistrationId={RegistrationId}. Next step: FaceVerification.",
                registration.Id);


            // ============================================================
            // 7. Prepare Response
            // ============================================================

            var response = new RegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status,
                NextStep = RegistrationNextSteps.VerifyFace,
                Message =
                    "PIN set successfully. Please complete face verification to finish your registration."
            };


            return Result<RegistrationResponse>.Success(
                response,
                "PIN_SET",
                response.Message);
        }


        public async Task<Result<RegistrationResponse>> VerifyFaceAsync(
    VerifyFaceRequest request,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Registration: VerifyFaceAsync started. RegistrationId={RegistrationId}.",
                request.RegistrationId);


            // ============================================================
            // 1. Get Registration
            // ============================================================

            var registration = await _repository.GetByIdAsync(
                request.RegistrationId,
                cancellationToken);

            if (registration is null)
            {
                _logger.LogWarning(
                    "Registration: Face verification failed. Registration not found. RegistrationId={RegistrationId}.",
                    request.RegistrationId);

                return Result<RegistrationResponse>.NotFound(
                    "Registration not found.");
            }


            // ============================================================
            // 2. Validate Registration State
            // ============================================================

            if (registration.Status !=
                RegistrationStatuses.PendingFaceVerification)
            {
                _logger.LogWarning(
                    "Registration: Face verification attempted in invalid state. RegistrationId={RegistrationId}, Status={Status}.",
                    registration.Id,
                    registration.Status);

                return Result<RegistrationResponse>.Failure(
                    "INVALID_REGISTRATION_STATE",
                    "Face verification is not available at the current registration step.");
            }


            // ============================================================
            // 3. Ensure PIN Exists
            // ============================================================

            if (string.IsNullOrWhiteSpace(registration.PinHash))
            {
                _logger.LogWarning(
                    "Registration: Face verification blocked because PIN is not set. RegistrationId={RegistrationId}.",
                    registration.Id);

                return Result<RegistrationResponse>.Failure(
                    "PIN_NOT_SET",
                    "Please set your PIN before completing face verification.");
            }

            // ============================================================
            // 5. Mark Face Verified
            // ============================================================

            registration.IsFaceVerified = true;
            registration.FaceImagePath = request.FaceImagePath;

            // ============================================================
            // 6. Registration Completed
            // ============================================================

            registration.Status =
                RegistrationStatuses.Completed;

            _repository.Update(registration);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "Registration: Registration completed successfully after face verification. RegistrationId={RegistrationId}.",
                registration.Id);

            // ============================================================
            // 7. Prepare Response
            // ============================================================

            var response = new RegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status,
                NextStep = RegistrationNextSteps.Completed,

                Message =
                    "Face verification completed successfully. Your registration is now complete."
            };

            return Result<RegistrationResponse>.Success(
                response,
                "REGISTRATION_COMPLETED",
                response.Message);
        }


        private async Task<Result<RegistrationResponse>?> ValidateAsync<TRequest>(
            IValidator<TRequest> validator,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (validation.IsValid)
                return null;

            var errors = validation.Errors.Select(e => new ApiError
            {
                Field = e.PropertyName,
                Code = e.ErrorCode,
                Message = e.ErrorMessage
            });

            return Result<RegistrationResponse>.ValidationFailed(errors);
        }

        /// <summary>
        /// No SMS/email gateway is wired up in this codebase yet, so the OTP
        /// is only surfaced via a Debug-level log line for local testing.
        /// TODO: replace with a real notification provider before go-live,
        /// and remove this log line once that provider is in place.
        /// </summary>
        private void LogOtpForDevelopment(Guid registrationId, string channel, string otp) =>
            _logger.LogDebug(
                "Registration: [DEV ONLY] {Channel} OTP for {RegistrationId} is {Otp}",
                channel, registrationId, otp);



        private async Task<string> GenerateAndStoreOtpAsync(
            Guid registrationId,
            string channel,
            CancellationToken cancellationToken)
        {
            // Purane active OTPs invalidate karo
            var oldOtps = await _otpRepository.FindAsync(
                x => x.RegistrationId == registrationId
                     && x.Channel == channel
                     && !x.IsUsed,
                cancellationToken);

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.IsUsed = true;
                _otpRepository.Update(oldOtp);
            }

            // New OTP
            var plainOtp = RegistrationHelpers.GenerateOtp();

            var otpVerification = new OtpVerification
            {
                RegistrationId = registrationId,
                Channel = channel,
                OtpHash = RegistrationHelpers.HashOtp(plainOtp),
                ExpiresAtUtc = DateTime.UtcNow.Add(RegistrationHelpers.OtpValidity),
                Attempts = 0,
                IsUsed = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _otpRepository.AddAsync(
                otpVerification,
                cancellationToken);

            return plainOtp;
        }


    }
}
