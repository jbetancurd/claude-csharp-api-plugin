// Landing Page Middleware - Shows health status and Swagger link on root path
// Place in: src/YourApi.Presentation/Middleware/LandingPageMiddleware.cs

using Microsoft.AspNetCore.Http;

namespace YourApi.Presentation.Middleware;

public class LandingPageMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only handle root path requests
        if (context.Request.Path == "/" && context.Request.Method == HttpMethods.Get)
        {
            context.Response.ContentType = "text/html; charset=utf-8";

            var isDevelopment = environment.IsDevelopment();
            var html = GenerateLandingPage(isDevelopment);

            await context.Response.WriteAsync(html);
            return;
        }

        await next(context);
    }

    private static string GenerateLandingPage(bool isDevelopment) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API Dashboard</title>
            <style>
                * {{
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }}

                body {{
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    min-height: 100vh;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    padding: 20px;
                }}

                .container {{
                    background: white;
                    border-radius: 12px;
                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
                    max-width: 600px;
                    width: 100%;
                    padding: 40px;
                }}

                .header {{
                    text-align: center;
                    margin-bottom: 30px;
                }}

                .logo {{
                    font-size: 32px;
                    margin-bottom: 10px;
                }}

                h1 {{
                    color: #333;
                    font-size: 28px;
                    margin-bottom: 5px;
                }}

                .subtitle {{
                    color: #666;
                    font-size: 14px;
                }}

                .status-card {{
                    background: #f8f9fa;
                    border-left: 4px solid #667eea;
                    padding: 20px;
                    border-radius: 8px;
                    margin-bottom: 20px;
                }}

                .status-label {{
                    color: #666;
                    font-size: 12px;
                    text-transform: uppercase;
                    letter-spacing: 1px;
                    margin-bottom: 5px;
                }}

                .status-value {{
                    color: #333;
                    font-size: 18px;
                    font-weight: 600;
                }}

                .status-badge {{
                    display: inline-block;
                    background: #10b981;
                    color: white;
                    padding: 4px 12px;
                    border-radius: 20px;
                    font-size: 12px;
                    margin-left: 10px;
                }}

                .links {{
                    display: grid;
                    gap: 12px;
                    margin-top: 30px;
                }}

                .link-button {{
                    display: flex;
                    align-items: center;
                    padding: 15px 20px;
                    background: #f0f2f5;
                    border: 2px solid #e0e3e8;
                    border-radius: 8px;
                    text-decoration: none;
                    color: #333;
                    font-weight: 500;
                    transition: all 0.3s ease;
                    cursor: pointer;
                }}

                .link-button:hover {{
                    border-color: #667eea;
                    background: #f0f2f5;
                    transform: translateX(4px);
                }}

                .link-button.primary {{
                    background: #667eea;
                    border-color: #667eea;
                    color: white;
                }}

                .link-button.primary:hover {{
                    background: #5568d3;
                    border-color: #5568d3;
                }}

                .link-icon {{
                    font-size: 20px;
                    margin-right: 12px;
                }}

                .endpoints {{
                    margin-top: 30px;
                    padding-top: 30px;
                    border-top: 1px solid #e0e3e8;
                }}

                .endpoints-title {{
                    color: #333;
                    font-size: 14px;
                    font-weight: 600;
                    text-transform: uppercase;
                    letter-spacing: 1px;
                    margin-bottom: 15px;
                }}

                .endpoint-item {{
                    display: flex;
                    align-items: center;
                    padding: 12px 0;
                    font-size: 14px;
                    color: #666;
                }}

                .endpoint-method {{
                    display: inline-block;
                    width: 50px;
                    font-weight: 600;
                    font-size: 12px;
                    text-transform: uppercase;
                }}

                .method-get {{
                    color: #3b82f6;
                }}

                .environment-badge {{
                    display: inline-block;
                    background: #fbbf24;
                    color: #92400e;
                    padding: 4px 12px;
                    border-radius: 4px;
                    font-size: 12px;
                    font-weight: 600;
                    margin-bottom: 20px;
                    text-align: center;
                }}

                .footer {{
                    text-align: center;
                    margin-top: 30px;
                    color: #999;
                    font-size: 12px;
                }}
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <div class="logo">🚀</div>
                    <h1>API Dashboard</h1>
                    <p class="subtitle">Welcome to your API</p>
                </div>

                {(isDevelopment ? """
                <div class="environment-badge">
                    🔧 DEVELOPMENT MODE
                </div>
                """ : "")}

                <div class="status-card">
                    <div class="status-label">Health Status</div>
                    <div class="status-value">
                        Operational
                        <span class="status-badge">✓ OK</span>
                    </div>
                </div>

                <div class="links">
                    {(isDevelopment ? """
                    <a href="/swagger" class="link-button primary">
                        <span class="link-icon">📖</span>
                        <span>API Documentation (Swagger)</span>
                    </a>

                    <a href="/health" class="link-button">
                        <span class="link-icon">💚</span>
                        <span>Health Check Endpoint</span>
                    </a>
                    """ : """
                    <a href="/health" class="link-button primary">
                        <span class="link-icon">💚</span>
                        <span>Health Check Status</span>
                    </a>
                    """)}
                </div>

                <div class="endpoints">
                    <div class="endpoints-title">Available Endpoints</div>
                    <div class="endpoint-item">
                        <span class="endpoint-method method-get">GET</span>
                        <span>/health</span>
                    </div>
                    {(isDevelopment ? """
                    <div class="endpoint-item">
                        <span class="endpoint-method method-get">GET</span>
                        <span>/swagger</span>
                    </div>
                    <div class="endpoint-item">
                        <span class="endpoint-method method-get">GET</span>
                        <span>/swagger/v1/swagger.json</span>
                    </div>
                    """ : "")}
                </div>

                <div class="footer">
                    <p>API is running and ready for requests</p>
                </div>
            </div>
        </body>
        </html>
        """;
}
