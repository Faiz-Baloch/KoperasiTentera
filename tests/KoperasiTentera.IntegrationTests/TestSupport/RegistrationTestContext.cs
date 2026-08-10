using AutoMapper;
using KoperasiTentera.Domain.Common;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Infrastructure.DATA;
using KoperasiTentera.Infrastructure.Persistence;
using KoperasiTentera.Infrastructure.Persistence.Repositories;
using KoperasiTentera.Service.Validators.Registration;
using KoperasiTentera.Service.Mapping;
using KoperasiTentera.Service.Services.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;

namespace KoperasiTentera.UnitTests.TestSupport;

public sealed class RegistrationTestContext : IDisposable
{
    public ApplicationDbContext Db { get; }
    public RegistrationService Service { get; }
    public CapturingLogger<RegistrationService> Logger { get; }

    public RegistrationTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"KoperasiTenteraTests-{Guid.NewGuid():N}")
            .Options;

        Db = new ApplicationDbContext(options);

        var registrationRepository = new Repository<Registrations>(
            Db,
            NullLogger<Repository<Registrations>>.Instance);

        var otpRepository = new Repository<OtpVerification>(
            Db,
            NullLogger<Repository<OtpVerification>>.Instance);

        var unitOfWork = new UnitOfWork(
            Db,
            NullLogger<UnitOfWork>.Instance);

        var mapperConfiguration = new MapperConfiguration(cfg =>
            cfg.AddProfile<RegistrationMappingProfile>());

        var mapper = mapperConfiguration.CreateMapper();
        Logger = new CapturingLogger<RegistrationService>();

        Service = new RegistrationService(
            registrationRepository,
            otpRepository,
            unitOfWork,
            mapper,
            Logger,
            new CheckIcRequestValidator(),
            new StartRegistrationRequestValidator(),
            new SendOtpRequestValidator(),
            new VerifyOtpRequestValidator(),
            new ChangeEmailRequestValidator(),
            new AcceptPrivacyPolicyRequestValidator(),
            new SetPinRequestValidator());
    }

    //public async Task<Registrations> AddRegistrationAsync(
    //    string status = RegistrationStatuses.PendingOtpMobile,
    //    string ic = "880214566831",
    //    string mobile = "0163386675",
    //    string email = "mariam@email.com",
    //    bool mobileVerified = false,
    //    bool emailVerified = false,
    //    bool privacyAccepted = false,
    //    string? pinHash = null,
    //    string? faceImagePath = null,
    //    bool faceVerified = false)
    //{
    //    var registration = new Registrations
    //    {
    //        CustomerName = "Mariam Abdul Rashid",
    //        ICNumber = ic,
    //        MobileNumber = mobile,
    //        Email = email,
    //        Status = status,
    //        IsMobileVerified = mobileVerified,
    //        IsEmailVerified = emailVerified,
    //        IsPrivacyAccepted = privacyAccepted,
    //        PinHash = pinHash,
    //        FaceImagePath = faceImagePath,
    //        IsFaceVerified = faceVerified,
    //        CreatedAtUtc = DateTime.UtcNow
    //    };

    //    Db.Registrations.Add(registration);
    //    await Db.SaveChangesAsync();
    //    return registration;
    //}



    public async Task<Registrations> AddRegistrationAsync(
    string status = RegistrationStatuses.PendingOtpMobile,
    string ic = "880214566831",
    string mobile = "0163386675",
    string email = "mariam@email.com",
    bool mobileVerified = false,
    bool emailVerified = false,
    bool privacyAccepted = false,
    string? pinHash = null,
    string? faceImagePath = null,
    bool faceVerified = false)
    {
        var registration = new Registrations
        {
            CustomerName = "Mariam Abdul Rashid",
            ICNumber = ic,
            MobileNumber = mobile,
            Email = email,
            Status = status,
            IsMobileVerified = mobileVerified,
            IsEmailVerified = emailVerified,
            IsPrivacyAccepted = privacyAccepted,
            PinHash = pinHash,
            FaceImagePath = faceImagePath,
            IsFaceVerified = faceVerified,
            CreatedAtUtc = DateTime.UtcNow
        };

        Db.Registrations.Add(registration);
        await Db.SaveChangesAsync();

        // The seeded entity must not remain tracked.
        // The service represents a separate request and will load
        // the registration itself.
        Db.ChangeTracker.Clear();

        return registration;
    }

    public async Task<OtpVerification> AddOtpAsync(
        Guid registrationId,
        string channel,
        string code = "7981",
        DateTime? expiresAtUtc = null,
        int attempts = 0,
        bool isUsed = false,
        DateTime? createdAtUtc = null)
    {
        var otp = new OtpVerification
        {
            RegistrationId = registrationId,
            Channel = channel,
            OtpHash = HashOtp(code),
            ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddMinutes(5),
            Attempts = attempts,
            IsUsed = isUsed,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };

        Db.OtpVerifications.Add(otp);
        await Db.SaveChangesAsync();
        return otp;
    }


    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }
    public string? LastGeneratedOtp(string channel)
    {
        foreach (var message in Logger.Messages.Reverse())
        {
            if (!message.Contains(channel, StringComparison.OrdinalIgnoreCase))
                continue;

            var match = Regex.Match(message, @"(?:Otp=|is\s+)(\d{4})\b");
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }

    public async Task<Registrations?> GetRegistrationAsync(Guid id) =>
        await Db.Registrations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

    public async Task<List<OtpVerification>> GetOtpsAsync(Guid registrationId, string channel) =>
        await Db.OtpVerifications
            .AsNoTracking()
            .Where(x => x.RegistrationId == registrationId && x.Channel == channel)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

    public void Dispose() => Db.Dispose();
}

public sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<string> _messages = new();
    public IReadOnlyList<string> Messages => _messages;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _messages.Add(formatter(state, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
