using App.Core.Constants;
using Microsoft.AspNetCore.Http;

namespace App.Core.Base
{
    public class BaseResponseModel<T>
    {
        public BaseResponseModel() 
        {
            StatusCode = StatusCodes.Status200OK;
            Code = ResponseCodeConstants.SUCCESS;
        }
        
        public T? Data { get; set; }
        public object? AdditionalData { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string Code { get; set; } = ResponseCodeConstants.SUCCESS;

        public BaseResponseModel(T? data, object? additionalData = null, string? message = null)
        {
            Data = data;
            AdditionalData = additionalData;
            Message = message;
        }

        public BaseResponseModel(string? message)
        {
            Message = message;
        }

        public static BaseResponseModel<T> OkResponseModel(T data, object? additionalData = null)
        {
            return new BaseResponseModel<T>(data, additionalData);
        }

        public static BaseResponseModel<T> NotFoundResponseModel(T? data = default, object? additionalData = null)
        {
            return new BaseResponseModel<T>
            {
                StatusCode = StatusCodes.Status404NotFound,
                Code = ResponseCodeConstants.NOT_FOUND,
                Data = data,
                AdditionalData = additionalData
            };
        }

        public static BaseResponseModel<T> BadRequestResponseModel(T? data = default, object? additionalData = null)
        {
            return new BaseResponseModel<T>
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Code = ResponseCodeConstants.FAILED,
                Data = data,
                AdditionalData = additionalData
            };
        }

        public static BaseResponseModel<T> InternalErrorResponseModel(T? data = default, object? additionalData = null)
        {
            return new BaseResponseModel<T>
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Code = ResponseCodeConstants.FAILED,
                Data = data,
                AdditionalData = additionalData
            };
        }
    }
}
