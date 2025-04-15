namespace App.DTOs.AuthDTOs
{
    public class ResponseAuthModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public DateTime ExpiresIn { get; set; }
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
