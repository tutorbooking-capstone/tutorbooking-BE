using App.Core.Base;

namespace App.Core.Jsetting
{
    public class JwtSettings
    {
        public string? SecretKey { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(SecretKey))
            {
                throw new InvalidArgumentException(
                    paramName: nameof(SecretKey),
                    message: "SecretKey không được null hoặc rỗng."
                );
            }

            if (string.IsNullOrEmpty(Issuer))
            {
                throw new InvalidArgumentException(
                    paramName: nameof(Issuer),
                    message: "Issuer không được null hoặc rỗng."
                );
            }

            if (string.IsNullOrEmpty(Audience))
            {
                throw new InvalidArgumentException(
                    paramName: nameof(Audience),
                    message: "Audience không được null hoặc rỗng."
                );
            }

            if (AccessTokenExpirationMinutes <= 0)
            {
                throw new InvalidArgumentException(
                    paramName: nameof(AccessTokenExpirationMinutes),
                    message: "AccessTokenExpirationMinutes phải lớn hơn 0."
                );
            }

            if (RefreshTokenExpirationDays <= 0)
            {
                throw new InvalidArgumentException(
                    paramName: nameof(RefreshTokenExpirationDays),
                    message: "RefreshTokenExpirationDays phải lớn hơn 0."
                );
            }

            return true;
        }
    }
}
