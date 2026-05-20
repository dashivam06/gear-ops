using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace gearOps.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        int status = httpContext.Response.StatusCode;
        bool isSuccess = status >= 200 && status < 300;

        object data = null;
        string message = "Request completed successfully";

        switch (context.Result)
        {
            case ObjectResult objResult:
                data = objResult.Value;
                status = objResult.StatusCode ?? status;
                break;
            case JsonResult jsonResult:
                data = jsonResult.Value;
                break;
            case ContentResult contentResult:
                data = contentResult.Content;
                status = contentResult.StatusCode ?? status;
                break;
            case EmptyResult:
                data = null;
                break;
            case StatusCodeResult statusCodeResult:
                status = statusCodeResult.StatusCode;
                break;
        }

        var envelope = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            status = status,
            success = isSuccess,
            message = isSuccess ? message : "Request completed with errors",
            path = httpContext.Request?.Path.Value ?? string.Empty,
            method = httpContext.Request?.Method ?? string.Empty,
            data = data
        };

        context.Result = new ObjectResult(envelope) { StatusCode = status };

        await next();
    }
}
