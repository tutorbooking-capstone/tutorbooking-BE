using App.Core.Provider;
using App.Services.Interfaces.User;

namespace App.Services.Infras
{
    public class CurrentUserProvider : ICurrentUserProvider
    {
        private readonly IUserService _userService;

        public CurrentUserProvider(IUserService userService)
        {
            _userService = userService;
        }

        public string? GetCurrentUserId()
        {
            try
            {
                return _userService.GetCurrentUserId();
            }
            catch
            {
                return null;
            }
        }

        public bool IsInRole(string roleName)
        {
            return _userService.IsInRole(roleName);
        }
    }
}
