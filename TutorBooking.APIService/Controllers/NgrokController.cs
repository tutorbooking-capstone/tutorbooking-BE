using App.Core.Base;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NgrokController : ControllerBase
    {
        #region Sample Data
        private static readonly List<string> SampleTunnels = new()
        {
            "https://abc123.ngrok.io",
            "https://def456.ngrok.io",
            "https://ghi789.ngrok.io"
        };

        private static readonly List<NgrokTunnelInfo> SampleTunnelInfos = new()
        {
            new NgrokTunnelInfo { Id = "1", PublicUrl = "https://abc123.ngrok.io", Protocol = "https", Region = "us" },
            new NgrokTunnelInfo { Id = "2", PublicUrl = "https://def456.ngrok.io", Protocol = "https", Region = "eu" },
            new NgrokTunnelInfo { Id = "3", PublicUrl = "https://ghi789.ngrok.io", Protocol = "https", Region = "ap" }
        };
        #endregion

        [HttpGet("tunnels")]
        public IActionResult GetTunnels()
        {
            return Ok(new BaseResponseModel<List<string>>(
                data: SampleTunnels,
                message: "Danh sách các tunnel đang hoạt động."
            ));
        }

        [HttpGet("tunnel-infos")]
        public IActionResult GetTunnelInfos()
        {
            return Ok(new BaseResponseModel<List<NgrokTunnelInfo>>(
                data: SampleTunnelInfos,
                message: "Danh sách thông tin chi tiết các tunnel."
            ));
        }

        [HttpGet("tunnel-infos/{id}")]
        public IActionResult GetTunnelInfoById(string id)
        {
            var tunnel = SampleTunnelInfos.FirstOrDefault(t => t.Id == id);
            if (tunnel == null)
            {
                return NotFound(new BaseResponseModel<string>(
                    message: $"Không tìm thấy tunnel với ID: {id}."
                ));
            }

            return Ok(new BaseResponseModel<NgrokTunnelInfo>(
                data: tunnel,
                message: "Thông tin tunnel."
            ));
        }
    }

    public class NgrokTunnelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
