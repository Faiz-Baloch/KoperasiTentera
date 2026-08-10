using AutoMapper;
using KoperasiTentera.Domain.Common;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Infrastructure.DATA;
using KoperasiTentera.Infrastructure.Persistence;
using KoperasiTentera.Infrastructure.Persistence.Repositories;
using KoperasiTentera.Service.Mapping;
using KoperasiTentera.Service.Services.Registration;
using KoperasiTentera.Service.Validators.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KoperasiTentera.UnitTests.TestSupport;

public sealed class RegistrationTestContext : IDisposable
{
    public ApplicationDbContext Db { get; }
    public RegistrationService Service { get; }
    public CapturingLogger<RegistrationService> Logger { get; }

    public RegistrationTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"KoperasiTentera.UnitTests-{Guid.NewGuid():N}")
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

    public async Task<Registrations> AddRegistrationAsync(
        string status = RegistrationStatuses.PendingOtpMobile,
        string ic = "880214566831",
        string mobile = "0163386675",
        string email = "mariam@email.com",
        bool mobileVerified = false,
        bool emailVerified = false,
        bool privacyAccepted = false,
        string? pinHash = null)
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
            CreatedAtUtc = DateTime.UtcNow
        };

        Db.Registrations.Add(registration);
        await Db.SaveChangesAsync();

        // Seeded data represents a previous request. Detach it so service
        // calls can load/update their own instance without tracking conflicts.
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
        Db.ChangeTracker.Clear();
        return otp;
    }

    public async Task<Registrations?> GetRegistrationAsync(Guid id) =>
        await Db.Registrations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

    public async Task<List<Registrations>> GetRegistrationsByIcAsync(string ic) =>
        await Db.Registrations.AsNoTracking()
            .Where(x => x.ICNumber == ic)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

    public async Task<List<OtpVerification>> GetOtpsAsync(Guid registrationId, string channel) =>
        await Db.OtpVerifications.AsNoTracking()
            .Where(x => x.RegistrationId == registrationId && x.Channel == channel)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

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

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }

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
