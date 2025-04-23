//using FluentValidation;
//using FluentValidation.AspNetCore;
//using FluentValidation.Results;
//using Microsoft.AspNetCore.Mvc; // Cần cho ActionContext

//namespace App.Core.Utils // Hoặc namespace phù hợp (Lưu ý: file đang nằm trong App.Core/Config)
//{
//    public class CustomValidationInterceptor : IValidatorInterceptor
//    {
//        // Phương thức này chạy SAU KHI FluentValidation hoàn tất validate
//        // Sử dụng ActionContext thay vì ControllerContext
//        public ValidationResult AfterAspNetValidation(ActionContext actionContext, IValidationContext validationContext, ValidationResult result)
//        {
//            Console.WriteLine("\n\nAspValidate\n\n");

//            // Nếu kết quả validation không hợp lệ (có lỗi)
//            if (!result.IsValid)
//            {
//                // Ném trực tiếp exception custom của bạn, sử dụng danh sách lỗi từ kết quả
//                // Constructor của ValidationException đã biết cách xử lý List<ValidationFailure>
//                throw new Base.ValidationException(result.Errors);
//            }
//            // Nếu hợp lệ, trả về kết quả gốc
//            return result;
//        }

//        // Phương thức này chạy TRƯỚC KHI FluentValidation validate
//        // Sử dụng ActionContext thay vì ControllerContext
//        public IValidationContext BeforeAspNetValidation(ActionContext actionContext, IValidationContext commonContext)
//        {
//            Console.WriteLine("\n\nAspValidate\n\n");
//            // Thường không cần làm gì ở đây, trả về context gốc
//            return commonContext;
//        }
//    }
//}