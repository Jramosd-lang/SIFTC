using SIFTC.Domain.Common;
using System;
using System.Net.Mail;
using System.Text;

namespace SIFTC.Domain.ValueObjects;

public sealed record Correo
{
    public string Value { get; } = default!;
    private Correo(string value)
    {
        Value = value;
    }



    public static Result<Correo> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Correo>(new Error("email.empty", "Email cannot be null or empty.", ErrorType.Validation));
        }

        if (value.Length > 254)
        {
            return Result.Failure<Correo>(new Error("email.tooLong", "Email cannot be longer than 254 characters.", ErrorType.Validation));
        }
        // Basic email format validation
        if (!MailAddress.TryCreate(value, out _))
        {
            return Result.Failure<Correo>(new Error("email.invalid", "Invalid email format.", ErrorType.Validation));
        }
        return Result.Success(new Correo(value.Trim().ToLowerInvariant()));
    }


}
