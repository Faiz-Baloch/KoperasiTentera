using KoperasiTentera.Service.Responses.Registration;
using KoperasiTentera.Service.Services.Registration;
using KoperasiTentera.Application.Common.Results;
using KoperasiTentera.Service.DTOs.Registration;
using Microsoft.AspNetCore.Mvc;
using System;

namespace KoperasiTentera.Api.Controllers
{
    [ApiController]
    [Route("api/registration")]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _registrationService; 
        public RegistrationController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        /// <summary>
        /// Step 1 (New &amp; Existing User): check whether an IC number is already registered.
        /// </summary>
        [HttpPost("check-ic")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckIc(
            [FromBody] CheckIcRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.CheckIcAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// New User: submit CustomerName/Mobile/Email to start registration and trigger the mobile OTP.
        /// </summary>
        [HttpPost("start")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Start(
            [FromBody] StartRegistrationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.StartRegistrationAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Resend an OTP on the given channel ("Mobile" or "Email").
        /// </summary>
        [HttpPost("send-otp")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendOtp(
            [FromBody] SendOtpRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.SendOtpAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Verify a Mobile or Email OTP. Mobile OTP success advances straight into Email OTP.
        /// </summary>
        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.VerifyOtpAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Existing User: change the email to verify and resend the Email OTP.
        /// </summary>
        [HttpPost("change-email")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeEmail(
            [FromBody] ChangeEmailRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.ChangeEmailAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Accept the privacy policy after both OTPs are verified.
        /// </summary>
        [HttpPost("accept-privacy-policy")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptPrivacyPolicy(
            [FromBody] AcceptPrivacyPolicyRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.AcceptPrivacyPolicyAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Create + confirm the 6-digit PIN. Completes the registration/login flow.
        /// </summary>
        [HttpPost("set-pin")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetPin(
            [FromBody] SetPinRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.SetPinAsync(request, cancellationToken);
            return ToActionResult(result);
        }
        [HttpPost("verify-face")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyFace(
    [FromForm] VerifyFaceUploadRequest request,
    CancellationToken cancellationToken)
        {
            if (request.FaceImage == null || request.FaceImage.Length == 0)
            {
                return BadRequest(ApiResponse<RegistrationResponse>.Fail(
                    StatusCodes.Status400BadRequest,
                    "FACE_IMAGE_REQUIRED",
                    "Face image is required."));
            }

            var allowedExtensions = new[]
            {
        ".jpg",
        ".jpeg",
        ".png"
    };

            var extension = Path.GetExtension(request.FaceImage.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<RegistrationResponse>.Fail(
                    StatusCodes.Status400BadRequest,
                    "INVALID_IMAGE_FORMAT",
                    "Only JPG, JPEG and PNG images are allowed."));
            }

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "faces",
                request.RegistrationId.ToString());

            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";

            var physicalPath = Path.Combine(
                uploadsFolder,
                fileName);

            await using (var stream = new FileStream(
                physicalPath,
                FileMode.Create))
            {
                await request.FaceImage.CopyToAsync(
                    stream,
                    cancellationToken);
            }

            var relativePath =
                $"/faces/{request.RegistrationId}/{fileName}";

            var verifyFaceRequest = new VerifyFaceRequest
            {
                RegistrationId = request.RegistrationId,
                FaceImagePath = relativePath
               // IsVerified = true
            };

            var result = await _registrationService.VerifyFaceAsync(
                verifyFaceRequest,
                cancellationToken);

            return ToActionResult(result);
        }

        private IActionResult ToActionResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                var response = ApiResponse<T>.Ok(result.Value!, result.Code, result.Message);
                return Ok(response);
            }

            var status = result.Code switch
            {
                "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
                "INVALID_OTP" => StatusCodes.Status400BadRequest,
                "OTP_EXPIRED" => StatusCodes.Status400BadRequest,
                "PIN_MISMATCH" => StatusCodes.Status400BadRequest,
                "PRIVACY_NOT_ACCEPTED" => StatusCodes.Status400BadRequest,
                "ACCOUNT_ALREADY_EXISTS" => StatusCodes.Status409Conflict,
                "NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };

            var fail = ApiResponse<T>.Fail(status, result.Code, result.Message, result.Errors);
            return StatusCode(status, fail);
        }
    }
}
