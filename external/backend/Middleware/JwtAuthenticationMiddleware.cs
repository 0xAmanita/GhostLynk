using backend_deob.Services;

namespace backend_deob.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            var userId = jwtService.ValidateToken(token);
            if (userId != null)
            {
                context.Items["UserId"] = userId;
            }
        }

        await _next(context);
    }
}
