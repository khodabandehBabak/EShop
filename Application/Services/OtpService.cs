using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace EShop.Application.Services;

public class OtpService : IOtpService
{
    private readonly IApplicationDbContext _db;
    private readonly ISmsService _sms;

    public OtpService(IApplicationDbContext db, ISmsService sms)
    {
        _db = db; _sms = sms;
    }

    public async Task<string> GenerateAndSendAsync(string phoneNumber)
    {
        // باطل کردن کدهای قبلی
        var old = await _db.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
            .ToListAsync();
        old.ForEach(o => o.IsUsed = true);

        var code = Random.Shared.Next(100000, 999999).ToString();
        var otp = new OtpCode
        {
            PhoneNumber = phoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false
        };
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        await _sms.SendAsync(phoneNumber,
            $"کد ورود شما به فروشگاه ستارخان: {code}");

        return code;
    }

    public async Task<bool> VerifyAsync(string phoneNumber, string code)
    {
        var otp = await _db.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber
                     && o.Code == code
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        if (otp is null) return false;
        otp.IsUsed = true;
        await _db.SaveChangesAsync();
        return true;
    }
}