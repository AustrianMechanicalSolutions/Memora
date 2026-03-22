using AuthApi.Dtos;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult Error(string code, string message, int status = 400)
    {
        return StatusCode(status, new ApiError(code, message));
    }
}