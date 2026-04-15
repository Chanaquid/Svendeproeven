using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // POST /api/reports
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReportDto>>> CreateReport(
            [FromBody] CreateReportDto dto)
        {
            var result = await _reportService.CreateReportAsync(Caller.UserId, dto);
            return Ok(ApiResponse<ReportDto>.Ok(result, "Report submitted successfully."));
        }

        // GET /api/reports/my
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetMyReports(
            [FromQuery] ReportFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _reportService.GetMyReportsAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<ReportDto>>.Ok(result));
        }

        // GET /api/reports/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(int id)
        {
            var result = await _reportService.GetByIdAsync(id, Caller.UserId, Caller.IsAdmin);
            return Ok(ApiResponse<ReportDto>.Ok(result));
        }

        //Admin endpoints

        // GET /api/reports
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetAll(
            [FromQuery] ReportFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _reportService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ReportDto>>.Ok(result));
        }

        // POST /api/reports/{id}/resolve
        [HttpPost("{id}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ReportDto>>> ResolveReport(
            int id,
            [FromBody] AdminResolveReportDto dto)
        {
            var result = await _reportService.ResolveReportAsync(id, Caller.UserId, dto);
            return Ok(ApiResponse<ReportDto>.Ok(result, "Report resolved successfully."));
        }
    }
}