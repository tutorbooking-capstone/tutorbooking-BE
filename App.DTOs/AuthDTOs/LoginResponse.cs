using App.DTOs.AuthDTOs;

namespace App.DTOs.AuthDTOs
{
    public class LoginResponse
    {
        public TokenResponse Token { get; set; } = new();
        public string Role { get; set; } = string.Empty;
    }
}
