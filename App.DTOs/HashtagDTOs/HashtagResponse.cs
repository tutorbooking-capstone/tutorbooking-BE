using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Repositories.Models;

namespace App.DTOs.HashtagDTOs
{
    public class HashtagResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UsageCount { get; set; } 
    }

    #region Mapping
    public static class HashtagResponseExtensions
    {
        public static HashtagResponse ToHashtagResponse(this Hashtag entity)
        {
            return new HashtagResponse
            {
                Id = entity.Id,
                Description = entity.Description,
                UsageCount = entity.UsageCount
            };
        }
    }
    #endregion
}
