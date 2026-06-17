using Application.Features.Roles.Queries.GetRolesQuery;
using Asp.Versioning;
using AutoMapper;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models.Requests.Role;
using WebApi.Models.Responses.Base;
using WebApi.Models.Responses.Role;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public class RoleController(ILogger<RoleController> logger,
    IMapper mapper) : BaseController(logger, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResultRes<List<RolesRes>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(ISender sender, [FromQuery] RoleSearchReq search)
    {
        var response = new PaginationResultRes<List<RolesRes>>();

        try
        {
            var (errorCode, result) = await sender.Send(new GetRolesQuery
            {
                SearchDto = Mapper.Map<RolesSearchDto>(search),
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Result = Mapper.Map<List<RolesRes>>(result!.Items);
            response.Pagination = new PaginationRes
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                TotalItems = result.TotalItems,
            };

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("GetAll role failed {ex}", ex);
            response.ErrorCode = RoleControllerMsg.Get.Exception;
            return InternalServerError(response);
        }
    }
}