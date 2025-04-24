using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using FluentValidation.Results;

namespace App.Core.Base
{
    public class CoreException : Exception
    {
        public CoreException(string code, string message, int statusCode = StatusCodes.Status500InternalServerError)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }

        public string Code { get; }

        public int StatusCode { get; set; }

        public Dictionary<string, object>? AdditionalData { get; set; }

    }

    public class ErrorDetail
    {
        [JsonPropertyName("statusCode")] public int? StatusCode { get; set; }
        [JsonPropertyName("errorCode")] public string? ErrorCode { get; set; }
        [JsonPropertyName("errorMessage")] public object? ErrorMessage { get; set; }
    }

    public class BadRequestException : ErrorException
    {
        public BadRequestException(string errorCode, string message) : base(
            400, 
            errorCode, 
            message) { }

        public BadRequestException(ICollection<KeyValuePair<string, ICollection<string>>> errors) : base(
                400, 
                new ErrorDetail
                {
                    ErrorCode = "bad_request",
                    ErrorMessage = errors
                }) { }
    }

    public class ValidationException : ErrorException
    {
        public ValidationException(IEnumerable<ValidationFailure> failures) : base(
            StatusCodes.Status400BadRequest, 
            "validation_error", 
            "Dữ liệu không hợp lệ")
        {
            var errorDictionary = new Dictionary<string, ICollection<string>>();
            
            foreach (var failure in failures)
            {
                if (!errorDictionary.ContainsKey(failure.PropertyName))
                    errorDictionary[failure.PropertyName] = new List<string>();
                
                errorDictionary[failure.PropertyName].Add(failure.ErrorMessage);
            }
            
            this.ErrorDetail.ErrorMessage = errorDictionary;
        }
    }

    public class ErrorException : Exception
    {
        public int StatusCode { get; }

        public ErrorDetail ErrorDetail { get; }

        public ErrorException(int statusCode, string errorCode, string message)
        {
            StatusCode = statusCode;
            ErrorDetail = new ErrorDetail
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                ErrorMessage = message
            };
        }

        public ErrorException(int statusCode, ErrorDetail errorDetail)
        {
            StatusCode = statusCode;
            ErrorDetail = errorDetail;
        }
    }

    public class InvalidArgumentException : ErrorException
    {
        public InvalidArgumentException(string paramName, string message)
            : base(
                statusCode: StatusCodes.Status400BadRequest,
                errorCode: "invalid_argument",
                message: $"Tham số không hợp lệ: {paramName}. {message}")
        {
            this.ErrorDetail.ErrorMessage = new Dictionary<string, object>
            {
                { "paramName", paramName },
                { "details", message }
            };
        }
    }

    public class AlreadySeededException : ErrorException
    {
        public AlreadySeededException(string resourceName, object? seededData = null)
            : base(
                statusCode: StatusCodes.Status409Conflict,
                errorCode: "already_seeded",
                message: $"Tài nguyên '{resourceName}' đã được seed trước đó.")
        {
            this.ErrorDetail.ErrorMessage = new Dictionary<string, object>
            {
                { "resource", resourceName },
                { "seededData", seededData ?? new object() }
            };
        }
    }
}