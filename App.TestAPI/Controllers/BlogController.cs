//using Microsoft.AspNetCore.Mvc;
//using App.Core.Constants;
//using App.Core.Base;
//using App.Services.Interfaces;
//using App.DTOs.BlogDTOs;

//namespace App.TestAPI.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class BlogController : ControllerBase
//    {
//        #region DI Constructor
//        private readonly IBlogService _blogService;

//        public BlogController(IBlogService blogService)
//        {
//            _blogService = blogService;
//        }
//        #endregion

//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            return Ok(new BaseResponseModel<IEnumerable<BlogResponse>>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: await _blogService.GetAllAsync()));
//        }

//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetById(string id)
//        {
//            return Ok(new BaseResponseModel<BlogResponse>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: await _blogService.GetByIdAsync(id)));
//        }

//        [HttpPost]
//        public async Task<IActionResult> Create(CreateBlogRequest model)
//        {
//            await _blogService.CreateAsync(model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: "Blog created successfully"));
//        }


//        [HttpGet("page")]
//        public async Task<IActionResult> GetPage([FromQuery] int index = 0, [FromQuery] int pageSize = 10)
//        {
//            var result = await _blogService.GetPageBlogsAsync(index, pageSize);
//            return Ok(new BaseResponseModel<BasePaginatedList<BlogResponse>>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: result));
//        }
        
//        [HttpPut("{id}")]
//        public async Task<IActionResult> Update(string id, UpdateBlogRequest model)
//        {
//            await _blogService.UpdateAsync(id, model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: "Blog updated successfully"));
//        }

//        [HttpDelete("{id}")]
//        public async Task<IActionResult> Delete(string id)
//        {
//            await _blogService.DeleteAsync(id);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: "Blog deleted successfully"));
//        }

//    }
//}
