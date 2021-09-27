using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PDS_Server
{
    internal static class AuthOptions
    {
        internal static readonly string ISSUER = ConfigurationManager.AppSetting.GetValue<string>("issuer");
        internal static readonly string AUDIENCE = ConfigurationManager.AppSetting.GetValue<string>("audience");
        internal static readonly string KEY = ConfigurationManager.AppSetting.GetValue<string>("secret");
        internal static readonly string SALT = ConfigurationManager.AppSetting.GetValue<string>("salt");
        internal static readonly int LIFETIME = ConfigurationManager.AppSetting.GetValue<int>("lifetime");
        internal static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
        internal static string ConvertToHash(this string text)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: text,
            salt: Convert.FromBase64String(SALT),
            prf: KeyDerivationPrf.HMACSHA1,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        }
        internal static bool ValidateEmail(this string email, out string normalized)
        {
            string value = email;
            try
            {
                if (string.IsNullOrWhiteSpace(value)) throw new Exception();
                string DomainMapper(Match match)
                {
                    var idn = new IdnMapping();
                    string domainName = idn.GetAscii(match.Groups[2].Value);
                    return match.Groups[1].Value + domainName;
                }
                value = Regex.Replace(value, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                if (Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
                {
                    normalized = value.ToLower();
                    return true;
                }
            }
            catch (Exception) { }
            normalized = null;
            return false;
        }
        internal static bool EmailIsValid(this string email)
        {
            string value = email;
            try
            {
                if (string.IsNullOrWhiteSpace(value)) throw new Exception();
                string DomainMapper(Match match)
                {
                    var idn = new IdnMapping();
                    string domainName = idn.GetAscii(match.Groups[2].Value);
                    return match.Groups[1].Value + domainName;
                }
                value = Regex.Replace(value, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                if (Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250))) return true;
            }
            catch (Exception) { }
            return false;
        }
    }

}
