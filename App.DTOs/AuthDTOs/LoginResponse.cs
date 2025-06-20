namespace App.DTOs.AuthDTOs
{
    public class LoginResponse
    {
        public TokenResponse Token { get; set; } = new();
        // public string Role { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
