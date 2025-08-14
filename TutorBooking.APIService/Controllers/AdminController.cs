using App.Core.Base;
using App.DTOs.AppUserDTOs.AdminDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [AuthorizeRoles(Role.Admin)] 
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }
        
        private Dictionary<string, object> GetRoleMetadata()
        {
            var metadata = new Dictionary<string, object>();
            var enumMetadata = EnumHelper.GetEnumMetadata(typeof(Role), typeof(StaffManagerType));
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            var roleDescriptions = new Dictionary<string, string>
            {
                { "Admin", "Quản trị viên hệ thống với toàn quyền truy cập" },
                { "Manager", "Quản lý hệ thống với quyền truy cập vào báo cáo và thống kê" },
                { "Staff", "Nhân viên hỗ trợ với quyền truy cập hạn chế" },
                { "Tutor", "Gia sư cung cấp dịch vụ dạy học" },
                { "Learner", "Học viên sử dụng dịch vụ dạy học" }
            };
            
            metadata.Add("RoleDescriptions", roleDescriptions);
            return metadata;
        }
        
        [HttpGet("managers")]
        public async Task<IActionResult> GetAllManagers([FromQuery] StaffManagerFilterRequest filter)
        {
            var managers = await _adminService.GetAllManagersAsync(filter);
            return Ok(new BaseResponseModel<BasePaginatedList<StaffManagerResponse>>(
                data: managers,
                additionalData: GetRoleMetadata(),
                message: "Danh sách tài khoản Manager"
            ));
        }
        
        [HttpGet("staffs")]
        public async Task<IActionResult> GetAllStaffs([FromQuery] StaffManagerFilterRequest filter)
        {
            var staffs = await _adminService.GetAllStaffsAsync(filter);
            return Ok(new BaseResponseModel<BasePaginatedList<StaffManagerResponse>>(
                data: staffs,
                additionalData: GetRoleMetadata(),
                message: "Danh sách tài khoản Staff"
            ));
        }
        
        [HttpPost("create-account")]
        public async Task<IActionResult> CreateStaffManager([FromBody] CreateStaffManagerRequest request)
        {
            var response = await _adminService.CreateStaffManagerAsync(request);
            string accountType = request.AccountType == StaffManagerType.Manager ? "Manager" : "Staff";
            
            // Hiển thị mật khẩu trong thông báo khi tạo tài khoản mới
            string message = !string.IsNullOrEmpty(response.Password) 
                ? $"Tạo tài khoản {accountType} thành công. Tên đăng nhập: {response.Username}, Mật khẩu: {response.Password}"
                : $"Tạo tài khoản {accountType} thành công. Tên đăng nhập: {response.Username}";
            
            return Ok(new BaseResponseModel<StaffManagerResponse>(
                data: response,
                additionalData: GetRoleMetadata(),
                message: message
            ));
        }
        
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterRequest filter)
        {
            var users = await _adminService.GetAllUsersAsync(filter);
            return Ok(new BaseResponseModel<BasePaginatedList<UserResponse>>(
                data: users,
                additionalData: GetRoleMetadata(),
                message: "Danh sách người dùng"
            ));
        }

        [HttpPut("toggle-user-status")]
        public async Task<IActionResult> ToggleUserStatus([FromBody] AccountStatusRequest request)
        {
            var response = await _adminService.ToggleAccountStatusAsync(request, true);
            string status = request.IsActive ? "kích hoạt" : "vô hiệu hóa";
            
            return Ok(new BaseResponseModel<StaffManagerResponse>(
                data: response,
                additionalData: GetRoleMetadata(),
                message: $"Đã {status} tài khoản {response.Username} thành công"
            ));
        }
        
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteAccount(string userId)
        {
            await _adminService.DeleteAccountAsync(userId);
            
            return Ok(new BaseResponseModel<object>(
                data: null,
                additionalData: GetRoleMetadata(),
                message: "Xóa tài khoản thành công"
            ));
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var response = await _adminService.ChangePasswordAsync(request);
            
            return Ok(new BaseResponseModel<StaffManagerResponse>(
                data: response,
                additionalData: GetRoleMetadata(),
                message: $"Đổi mật khẩu cho tài khoản {response.Username} thành công. Mật khẩu mới: {response.Password}"
            ));
        }
        
        [HttpGet("metadata")]
        public IActionResult GetMetadata()
        {
            return Ok(new BaseResponseModel<object>(
                data: GetRoleMetadata(),
                message: "Metadata cho quản lý tài khoản"
            ));
        }
    }
}