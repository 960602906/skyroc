using Application.DTOs.ImportExport;
using Application.interfaces;
using Domain.Entities.ImportExport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>统一 CSV 导入导出控制器，提供模板、同步作业、CSV 下载和当前用户任务状态查询。</summary>
[ApiController]
[Route("api/import-export/jobs")]
[Authorize]
[PermissionResource(PermissionCodes.Business.ImportExport.Resource)]
public class ImportExportJobsController(IImportExportJobService service) : ControllerBase
{
    /// <summary>下载指定业务类型的 CSV 导入模板；当前支持商品类型。</summary>
    /// <param name="jobType">模板所属业务类型，1 表示商品。</param>
    /// <returns>UTF-8 BOM 编码的 CSV 模板文件。</returns>
    [HttpGet("templates/{jobType}")]
    [Authorize(Policy = PermissionCodes.Business.ImportExport.Create)]
    [ResourcePermission(PermissionActions.Create)]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "text/csv")]
    public async Task<FileContentResult> DownloadTemplate(ImportExportJobType jobType)
    {
        var result = await service.DownloadTemplateAsync(jobType);
        return File(result.Content, "text/csv; charset=utf-8", result.FileName);
    }

    /// <summary>读取商品 CSV 并在整文件校验通过后写入商品资料；失败时不写入任何商品行。</summary>
    /// <param name="jobType">导入业务类型，1 表示商品。</param>
    /// <param name="upload">仅接受不超过 2 MiB 的 CSV 文件，不保存为独立上传文件。</param>
    /// <returns>包含总行数、成功数、失败数和错误摘要的任务结果。</returns>
    [HttpPost("import/{jobType}")]
    [Authorize(Policy = PermissionCodes.Business.ImportExport.Create)]
    [ResourcePermission(PermissionActions.Create)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ImportExportJobDto>>> Import(ImportExportJobType jobType, [FromForm] ImportExportUploadDto upload)
    {
        var file = upload.File;
        if (file.Length == 0 || file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(ApiResponse<ImportExportJobDto>.Fail("CSV 文件必须大于 0 且不超过 2 MiB"));
        }
        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<ImportExportJobDto>.Fail("仅支持 CSV 文件"));
        }
        await using var stream = file.OpenReadStream();
        var result = await service.ImportAsync(jobType, file.FileName, stream);
        return Ok(ApiResponse<ImportExportJobDto>.Ok(result));
    }

    /// <summary>导出当前商品数据为 CSV；响应头 X-Import-Export-Job-Id 可用于查询本次任务状态。</summary>
    /// <param name="jobType">导出业务类型，1 表示商品。</param>
    /// <returns>UTF-8 BOM 编码的 CSV 导出文件。</returns>
    [HttpGet("export/{jobType}")]
    [Authorize(Policy = PermissionCodes.Business.ImportExport.Read)]
    [ResourcePermission(PermissionActions.Read)]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "text/csv")]
    public async Task<FileContentResult> Export(ImportExportJobType jobType)
    {
        var result = await service.ExportAsync(jobType);
        Response.Headers.Append("X-Import-Export-Job-Id", result.Job.Id.ToString());
        return File(result.Content, "text/csv; charset=utf-8", result.FileName);
    }

    /// <summary>查询当前操作人创建的导入或导出任务状态，避免泄露其他用户的文件处理结果。</summary>
    /// <param name="id">任务主键。</param>
    /// <returns>任务执行方向、行数统计、完成时间和失败摘要。</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.Business.ImportExport.Read)]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<ImportExportJobDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<ImportExportJobDto>.Ok(result));
    }
}
