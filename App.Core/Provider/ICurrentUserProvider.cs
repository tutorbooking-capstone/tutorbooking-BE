namespace App.Core.Provider
{
    public interface ICurrentUserProvider
    {
        string? GetCurrentUserId();
        bool IsInRole(string roleName);
    }
}
