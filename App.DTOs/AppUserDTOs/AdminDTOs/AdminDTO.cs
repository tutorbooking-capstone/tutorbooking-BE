using App.Repositories.Models.User;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace App.DTOs.AppUserDTOs.AdminDTOs
{
    public enum StaffManagerType
    {
        Manager = 1,
        Staff = 2
    }

    public class CreateStaffManagerRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        public string CitizenId { get; set; } = string.Empty;
        
        [Required]
        public StaffManagerType AccountType { get; set; }
        
        public string? PhoneNumber { get; set; } 
    }
    
    public class CreateStaffManagerRequestValidator : AbstractValidator<CreateStaffManagerRequest>
    {
        public CreateStaffManagerRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên là bắt buộc")
                .MinimumLength(3).WithMessage("Họ và tên phải có ít nhất 3 ký tự")
                .MaximumLength(100).WithMessage("Họ và tên không được quá 100 ký tự");
            
            RuleFor(x => x.CitizenId)
                .NotEmpty().WithMessage("Số căn cước công dân là bắt buộc")
                .Matches(@"^\d{9,12}$").WithMessage("Số căn cước công dân phải có 9-12 chữ số");
        }
    }
    
    public class StaffManagerResponse
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Password { get; set; } // Thêm mật khẩu
    }
    
    public static class MappingExtensions
    {
        public static StaffManagerResponse ToStaffResponse(this Staff staff, string role, string? password = null)
        {
            return new StaffManagerResponse
            {
                Id = staff.UserId,
                FullName = staff.User?.FullName ?? string.Empty,
                Username = staff.User?.UserName ?? string.Empty,
                Email = staff.User?.Email ?? string.Empty,
                PhoneNumber = staff.User?.PhoneNumber ?? string.Empty,
                IsActive = !staff.User?.DeletedTime.HasValue ?? false,
                CreatedTime = staff.User?.CreatedTime ?? DateTimeOffset.MinValue,
                Role = role,
                Password = password
            };
        }
    
        public static StaffManagerResponse ToManagerResponse(this Manager manager, string role, string? password = null)
        {
            return new StaffManagerResponse
            {
                Id = manager.UserId,
                FullName = manager.User?.FullName ?? string.Empty,
                Username = manager.User?.UserName ?? string.Empty,
                Email = manager.User?.Email ?? string.Empty,
                PhoneNumber = manager.User?.PhoneNumber ?? string.Empty,
                IsActive = !manager.User?.DeletedTime.HasValue ?? false,
                CreatedTime = manager.User?.CreatedTime ?? DateTimeOffset.MinValue,
                Role = role,
                Password = password
            };
        }
    }
    
    public class AccountStatusRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public bool IsActive { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class StaffManagerFilterRequest
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }

    public class UserFilterRequest
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public Role? Role { get; set; }
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }

    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}