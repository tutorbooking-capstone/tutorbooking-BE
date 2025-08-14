using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.Core.Utils;
using App.DTOs.AppUserDTOs.AdminDTOs;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace App.Services.Services.User
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public AdminService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<BasePaginatedList<StaffManagerResponse>> GetAllManagersAsync(StaffManagerFilterRequest filter)
        {
            EnsureAdminAccess();

            var query = _unitOfWork.GetRepository<Manager>()
                    .GetQueryable()
                    .Include(m => m.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(m => m.User!.FullName.Contains(filter.Name));

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value)
                    query = query.Where(m => m.User!.DeletedTime == null);
                else
                    query = query.Where(m => m.User!.DeletedTime != null);
            }

            query = query.OrderByDescending(m => m.User!.CreatedTime);
            var totalItems = await query.CountAsync();
            var pageSize = Math.Max(1, filter.PageSize);
            var pageIndex = Math.Max(0, filter.PageIndex);

            var managers = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = managers.Select(m => m.ToManagerResponse(Role.Manager.ToString())).ToList();

            return new BasePaginatedList<StaffManagerResponse>(
                items, 
                totalItems, 
                pageIndex, 
                pageSize
            );
        }

        public async Task<BasePaginatedList<StaffManagerResponse>> GetAllStaffsAsync(StaffManagerFilterRequest filter)
        {
            EnsureAdminAccess();

            var query = _unitOfWork.GetRepository<Staff>()
                .GetQueryable()
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(s => s.User!.FullName.Contains(filter.Name));

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value)
                    query = query.Where(s => s.User!.DeletedTime == null);
                else
                    query = query.Where(s => s.User!.DeletedTime != null);
            }

            query = query.OrderByDescending(s => s.User!.CreatedTime);

            var totalItems = await query.CountAsync();

            var pageSize = Math.Max(1, filter.PageSize);
            var pageIndex = Math.Max(0, filter.PageIndex);

            var staffs = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = staffs.Select(s => s.ToStaffResponse(Role.Staff.ToString())).ToList();

            return new BasePaginatedList<StaffManagerResponse>(
                items, 
                totalItems, 
                pageIndex, 
                pageSize
            );
        }

        public async Task<StaffManagerResponse> CreateStaffManagerAsync(CreateStaffManagerRequest request)
        {
            EnsureAdminAccess();

            // Tạo username từ tên
            string username = GenerateUsername(request.FullName);
            
            // Tạo password ngẫu nhiên theo quy tắc
            string password = GenerateSecurePassword();
            
            // Mã hóa CCCD
            string encryptedCitizenId = EncryptCitizenId(request.CitizenId);
            
            // Tạo user
            var passwordHasher = new FixedSaltPasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions()));
            
            var newUser = new AppUser
            {
                Id = Guid.NewGuid().ToString("N"),
                FullName = request.FullName,
                UserName = username,
                NormalizedUserName = _userManager.KeyNormalizer.NormalizeName(username),
                Email = $"{username}@tutorbooking.com",
                NormalizedEmail = _userManager.KeyNormalizer.NormalizeEmail($"{username}@tutorbooking.com"),
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = passwordHasher.HashPassword(null, password),
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                PhoneNumber = request.PhoneNumber,  
                CreatedTime = DateTime.UtcNow
            };
            
            var result = await _userManager.CreateAsync(newUser);
            newUser.TrackCreate(GetAuthenticatedUserId());
            
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể tạo tài khoản");
            
            // Thêm role
            string roleName;
            if (request.AccountType == StaffManagerType.Manager)
            {
                roleName = Role.Manager.ToString();
                result = await _userManager.AddToRoleAsync(newUser, roleName);
                
                if (!result.Succeeded)
                {
                    await _userManager.DeleteAsync(newUser);
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        result.Errors.FirstOrDefault()?.Description ?? "Không thể gán vai trò");
                }
                
                // Tạo Manager entity
                var manager = new Manager
                {
                    UserId = newUser.Id,
                    EncryptedCitizenId = encryptedCitizenId,
                    User = newUser
                };
                
                _unitOfWork.GetRepository<Manager>().Insert(manager);
                await _unitOfWork.SaveAsync();
                
                return manager.ToManagerResponse(roleName, password);  
            }
            else // Staff
            {
                roleName = Role.Staff.ToString();
                result = await _userManager.AddToRoleAsync(newUser, roleName);
                
                if (!result.Succeeded)
                {
                    await _userManager.DeleteAsync(newUser);
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        result.Errors.FirstOrDefault()?.Description ?? "Không thể gán vai trò");
                }
                
                // Tạo Staff entity
                var staff = new Staff
                {
                    UserId = newUser.Id,
                    EncryptedCitizenId = encryptedCitizenId,
                    User = newUser
                };
                
                _unitOfWork.GetRepository<Staff>().Insert(staff);
                await _unitOfWork.SaveAsync();
                
                return staff.ToStaffResponse(roleName, password);  
            }
        }

        public async Task<BasePaginatedList<UserResponse>> GetAllUsersAsync(UserFilterRequest filter)
        {
            EnsureAdminAccess();

            var query = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(u => u.FullName.Contains(filter.Name));

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value)
                    query = query.Where(u => u.DeletedTime == null);
                else
                    query = query.Where(u => u.DeletedTime != null);
            }

            query = query.OrderByDescending(u => u.CreatedTime);
            var allUserIds = await query.Select(u => u.Id).ToListAsync();
            var userRolesDict = await _unitOfWork.GetUserRolesAsync(allUserIds);
            var filteredUserIds = new List<string>();
            
            foreach (var userId in allUserIds)
            {
                if (!userRolesDict.TryGetValue(userId, out var roles))
                    continue;
                    
                if (filter.Role.HasValue)
                {
                    if (roles.Contains(filter.Role.Value.ToString()))
                        filteredUserIds.Add(userId);
                }
                else
                {
                    if (roles.Contains(Role.Learner.ToString()) || roles.Contains(Role.Tutor.ToString()))
                        filteredUserIds.Add(userId);
                }
            }
            
            query = query.Where(u => filteredUserIds.Contains(u.Id));
            
            var totalItems = await query.CountAsync();
            var pageSize = Math.Max(1, filter.PageSize);
            var pageIndex = Math.Max(0, filter.PageIndex);
            
            var users = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Tạo response
            var items = users.Select(user => 
            {
                userRolesDict.TryGetValue(user.Id, out var roles);
                roles ??= new List<string>();
                
                return new UserResponse
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    IsActive = !user.DeletedTime.HasValue,
                    CreatedTime = user.CreatedTime,
                    Role = string.Join(", ", roles)
                };
            }).ToList();
            
            return new BasePaginatedList<UserResponse>(
                items,
                totalItems,
                pageIndex,
                pageSize
            );
        }

        public async Task<StaffManagerResponse> ToggleAccountStatusAsync(AccountStatusRequest request, bool allowAllRoles = false)
        {
            EnsureAdminAccess();
            
            var user = await _userManager.FindByIdAsync(request.UserId) 
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng");
            
            // Kiểm tra role
            var roles = await _userManager.GetRolesAsync(user);
            
            if (!allowAllRoles && !roles.Contains(Role.Manager.ToString()) && !roles.Contains(Role.Staff.ToString()))
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Người dùng không phải là Manager hoặc Staff");
            
            // Không cho phép vô hiệu hóa Admin
            if (roles.Contains(Role.Admin.ToString()))
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Không thể thay đổi trạng thái của tài khoản Admin");
            
            // Cập nhật trạng thái
            if (request.IsActive && user.DeletedTime.HasValue)
            {
                // Kích hoạt lại tài khoản
                user.DeletedTime = null;
                user.DeletedBy = null;
            }
            else if (!request.IsActive && !user.DeletedTime.HasValue)
            {
                // Vô hiệu hóa tài khoản
                user.DeletedTime = DateTime.UtcNow;
                user.DeletedBy = GetAuthenticatedUserId();
            }
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể cập nhật trạng thái tài khoản");
            
            // Tạo response tương ứng với loại tài khoản
            if (roles.Contains(Role.Manager.ToString()))
            {
                var manager = await _unitOfWork.GetRepository<Manager>()
                    .GetQueryable()
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.UserId == request.UserId);
                
                if (manager != null)
                    return manager.ToManagerResponse(Role.Manager.ToString());
            }
            else if (roles.Contains(Role.Staff.ToString()))
            {
                var staff = await _unitOfWork.GetRepository<Staff>()
                    .GetQueryable()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == request.UserId);
                
                if (staff != null)
                    return staff.ToStaffResponse(Role.Staff.ToString());
            }
            
            // Trường hợp là user thông thường
            return new StaffManagerResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = !user.DeletedTime.HasValue,
                CreatedTime = user.CreatedTime,
                Role = string.Join(", ", roles)
            };
        }

        public async Task DeleteAccountAsync(string userId)
        {
            EnsureAdminAccess();
            
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng");
            
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(Role.Manager.ToString()) && !roles.Contains(Role.Staff.ToString()))
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Người dùng không phải là Manager hoặc Staff");
            
            if (roles.Contains(Role.Manager.ToString()))
            {
                var manager = await _unitOfWork.GetRepository<Manager>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(m => m.UserId == userId);
                
                if (manager != null)
                    _unitOfWork.GetRepository<Manager>().Delete(manager);
            }
            else if (roles.Contains(Role.Staff.ToString()))
            {
                var staff = await _unitOfWork.GetRepository<Staff>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(s => s.UserId == userId);
                
                if (staff != null)
                    _unitOfWork.GetRepository<Staff>().Delete(staff);
            }
            
            // Xóa user
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể xóa tài khoản");
            
            await _unitOfWork.SaveAsync();
        }

        public async Task<StaffManagerResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            EnsureAdminAccess();
            
            var user = await _userManager.FindByIdAsync(request.UserId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng");
            
            // Kiểm tra xem người dùng có phải là Manager hoặc Staff không
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(Role.Manager.ToString()) && !roles.Contains(Role.Staff.ToString()))
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Người dùng không phải là Manager hoặc Staff");
            
            // Đổi mật khẩu trực tiếp
            var passwordHasher = new FixedSaltPasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions()));
            string hashedNewPassword = passwordHasher.HashPassword(user, request.NewPassword);
            
            user.PasswordHash = hashedNewPassword;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể cập nhật mật khẩu");
            
            // Trả về thông tin người dùng đã cập nhật
            if (roles.Contains(Role.Manager.ToString()))
            {
                var manager = await _unitOfWork.GetRepository<Manager>()
                    .GetQueryable()
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.UserId == request.UserId);
                
                return manager?.ToManagerResponse(Role.Manager.ToString(), request.NewPassword) 
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy Manager");
            }
            else
            {
                var staff = await _unitOfWork.GetRepository<Staff>()
                    .GetQueryable()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == request.UserId);
                
                return staff?.ToStaffResponse(Role.Staff.ToString(), request.NewPassword) 
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy Staff");
            }
        }

        #region Helper Methods
        private string GetAuthenticatedUserId()
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "Bạn chưa đăng nhập");
            
            return userId;
        }
        
        private void EnsureAdminAccess()
        {
            if (!_currentUserProvider.IsInRole(Role.Admin.ToString()))
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "Bạn không có quyền thực hiện hành động này");
        }
        
        private string GenerateUsername(string fullName)
        {
            // Tách tên thành các phần
            var nameParts = fullName.Trim().Split(' ');
            string firstName = nameParts.Last();
            
            // Lấy chữ cái đầu của các phần họ
            string initials = string.Join("", nameParts.Take(nameParts.Length - 1).Select(p => p[0]));
            
            // Tạo 3 số ngẫu nhiên
            string randomDigits = new Random().Next(100, 999).ToString();
            
            // Kết hợp: firstName + initials + randomDigits
            return $"{firstName}{initials}{randomDigits}";
        }
        
        private string GenerateSecurePassword()
        {
            // Tạo mật khẩu theo quy tắc: ít nhất 8 ký tự, 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            const string specialChars = "@$!%*?&.";
            
            var random = new Random();
            
            string password = 
                lowerChars[random.Next(lowerChars.Length)].ToString() +
                upperChars[random.Next(upperChars.Length)].ToString() +
                numbers[random.Next(numbers.Length)].ToString() +
                specialChars[random.Next(specialChars.Length)].ToString();
            
            // Thêm 4 ký tự ngẫu nhiên nữa để đạt độ dài tối thiểu 8
            for (int i = 0; i < 4; i++)
            {
                string allChars = lowerChars + upperChars + numbers + specialChars;
                password += allChars[random.Next(allChars.Length)];
            }
            
            // Trộn ngẫu nhiên các ký tự
            return new string(password.ToCharArray().OrderBy(c => Guid.NewGuid()).ToArray());
        }
        
        private string EncryptCitizenId(string citizenId)
        {
            // Sử dụng SHA256 để mã hóa
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(citizenId);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        #endregion
    }
}