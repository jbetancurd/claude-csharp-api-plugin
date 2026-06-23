# Authentication Strategy Guide

Choose and implement the right authentication method for your API.

---

## Authentication Options

### Option 1: No Authentication (Development Only)
```csharp
// No authentication required
[HttpGet("api/v1/users")]
public async Task<ActionResult<List<UserDto>>> GetUsers()
{
    // Anyone can access
}
```

✅ **Use when:**
- Development/sandbox API
- Internal-only API (firewalled)
- Learning/tutorial projects
- Public read-only data

❌ **Issues:**
- No security
- Anyone can access
- Not suitable for production

---

### Option 2: API Keys (Simple)
```
Header: X-API-Key: sk_prod_abc123xyz
```

```csharp
[Authorize(Policy = "ApiKey")]
[HttpGet("api/v1/users")]
public async Task<ActionResult<List<UserDto>>> GetUsers()
{
    // Only with valid API key
}
```

**Setup**:
```bash
dotnet add package AspNetCore.Authentication.ApiKey
```

**Program.cs**:
```csharp
builder.Services
    .AddAuthentication(ApiKeySchemeOptions.DefaultScheme)
    .AddApiKey(options =>
    {
        options.KeyName = "X-API-Key";
        options.Scheme = "ApiKey";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKey", policy =>
        policy.RequireAuthenticatedUser());
});
```

✅ **Use when:**
- Simple server-to-server APIs
- Service integrations
- Limited number of clients
- Easy to rotate keys

❌ **Issues:**
- Keys in headers (log exposure)
- Hard to revoke
- No user context
- Limited scopes

---

### Option 3: JWT Tokens (Self-Issued)
```
Header: Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

```csharp
[Authorize]
[HttpGet("api/v1/users")]
public async Task<ActionResult<List<UserDto>>> GetUsers()
{
    var userId = User.FindFirst("sub")?.Value;
    // Authenticated user accessing
}
```

**Installation**:
```bash
dotnet add package System.IdentityModel.Tokens.Jwt
```

**Program.cs Setup**:
```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://yourdomain.com",
            
            ValidateAudience = true,
            ValidAudience = "your-api",
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("your-super-secret-key-min-32-chars")
            ),
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
```

**Generate Token**:
```csharp
[HttpPost("api/v1/auth/login")]
public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
{
    // Verify credentials
    var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
    if (user == null)
        return Unauthorized();

    // Create JWT
    var token = GenerateJwtToken(user);

    return Ok(new LoginResponseDto { Token = token });
}

private string GenerateJwtToken(User user)
{
    var securityKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("your-super-secret-key-min-32-chars")
    );
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim("sub", user.Id.ToString()),
        new Claim("email", user.Email),
        new Claim("name", user.Name)
    };

    var token = new JwtSecurityToken(
        issuer: "https://yourdomain.com",
        audience: "your-api",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

✅ **Use when:**
- Single-domain applications
- Web + Mobile apps
- Stateless authentication
- Short-lived tokens
- Self-contained credentials

❌ **Issues:**
- Token can't be revoked easily
- Large tokens (more network data)
- Secret key management
- Clock skew issues

---

### Option 4: OAuth 2.0 (3rd Party Login)
```
User → "Login with Google/GitHub"
  ↓
OAuth Provider ↔ Your API
  ↓
Access Token issued
```

**Popular Providers:**
- Google OAuth
- GitHub OAuth
- Microsoft Azure AD
- Facebook Login
- Apple Sign In

**Setup Example (Google)**:
```bash
dotnet add package Google.Apis.Auth
```

**Program.cs**:
```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];
});
```

**appsettings.json**:
```json
{
  "Google": {
    "ClientId": "xxx.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-yyy"
  }
}
```

**Controller**:
```csharp
[HttpGet("api/v1/auth/login/google")]
public IActionResult LoginGoogle()
{
    var redirectUrl = Url.Action("GoogleCallback", "Auth");
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    return Challenge(properties, "Google");
}

[HttpGet("api/v1/auth/callback/google")]
public async Task<IActionResult> GoogleCallback(string returnUrl = null)
{
    var result = await HttpContext.AuthenticateAsync("Google");
    var claims = result.Principal.Identities.FirstOrDefault()?.Claims;

    var userEmail = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var userName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

    // Create or update user in your database
    var user = await _userService.GetOrCreateUserAsync(userEmail, userName);

    // Generate your own JWT or set session
    var token = GenerateJwtToken(user);

    return Redirect($"{returnUrl}?token={token}");
}
```

✅ **Use when:**
- Federated authentication
- Consumer apps (need 3rd party login)
- Don't want to manage passwords
- Social login features
- Enterprise integrations (Azure AD)

❌ **Issues:**
- Dependency on external provider
- Complex setup
- CORS/redirect issues
- Provider changes affect your app

---

### Option 5: Identity Server / Azure AD
```
Your App ↔ Identity Server ↔ User
         ↔ Other Apps
```

**Azure AD Example**:
```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.MicrosoftGraph
```

**Program.cs**:
```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    },
    options => builder.Configuration.Bind("AzureAd", options));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequiredRole", policy =>
        policy.Requirements.Add(new RoleRequirement("Admin")));
});
```

**appsettings.json**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

✅ **Use when:**
- Enterprise applications
- Azure ecosystem
- Multi-tenant apps
- Complex authorization rules
- Centralized user management

❌ **Issues:**
- Complex setup
- Vendor lock-in (Azure)
- Higher licensing costs
- Learning curve

---

### Option 6: Session-Based Authentication (Traditional)
```csharp
[HttpPost("api/v1/auth/login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
    if (user == null)
        return Unauthorized();

    // Create session
    HttpContext.Session.SetString("UserId", user.Id.ToString());
    HttpContext.Session.SetString("UserEmail", user.Email);

    return Ok(new { message = "Logged in" });
}

[HttpPost("api/v1/auth/logout")]
public IActionResult Logout()
{
    HttpContext.Session.Clear();
    return Ok(new { message = "Logged out" });
}

[Authorize]
[HttpGet("api/v1/users")]
public async Task<ActionResult<List<UserDto>>> GetUsers()
{
    var userId = HttpContext.Session.GetString("UserId");
    // Get users for authenticated session
}
```

✅ **Use when:**
- Server-side rendered apps
- Same-origin requests only
- Simpler setup
- Stateful applications

❌ **Issues:**
- Not suitable for APIs
- Doesn't work cross-domain
- Server memory overhead
- Not scalable (sessions in memory)

---

## FREE & ZERO-BUDGET OPTIONS ✅ (Best for Startups & Personal Projects)

### Google OAuth (Completely FREE)
```
Login with Google - Zero Cost
```

**Benefits**:
- ✅ **Free forever** (no credit card required)
- ✅ 1.9 billion active users
- ✅ Simple setup (15 minutes)
- ✅ Works worldwide
- ✅ No API cost until millions of requests

**Setup**:
1. Go to https://console.cloud.google.com
2. Create project (free)
3. Enable "Google+ API" (free)
4. Create OAuth 2.0 credentials (free)
5. Get Client ID & Secret (free)

```bash
dotnet add package Google.Apis.Auth
```

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie()
.AddGoogle(options =>
{
    // Get from Google Cloud Console (FREE)
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];
});
```

---

### GitHub OAuth (Completely FREE)
```
Login with GitHub - Zero Cost
Perfect for developers and open source
```

**Benefits**:
- ✅ **Free forever**
- ✅ Every developer has GitHub account
- ✅ Perfect for dev tools, open source, internal apps
- ✅ Simple setup (10 minutes)
- ✅ No payment needed

**Setup**:
1. GitHub Settings → Developer Settings → OAuth Apps
2. Create new OAuth App (free)
3. Get Client ID & Secret (free)

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "GitHub";
})
.AddCookie()
.AddOAuth("GitHub", options =>
{
    options.ClientId = builder.Configuration["GitHub:ClientId"];      // FREE
    options.ClientSecret = builder.Configuration["GitHub:ClientSecret"]; // FREE
    options.CallbackPath = "/signin-github";
    
    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
    options.UserInformationEndpoint = "https://api.github.com/user";
});
```

---

### Facebook OAuth (Completely FREE)
```
Login with Facebook - Zero Cost
2.9 billion monthly active users
```

**Benefits**:
- ✅ **Free forever**
- ✅ 2.9 billion users
- ✅ Simple setup (15 minutes)
- ✅ No credit card required
- ✅ Good for consumer apps

**Setup**:
1. https://developers.facebook.com
2. Create app (free)
3. Get App ID & Secret (free)

```csharp
builder.Services.AddOAuth("Facebook", options =>
{
    options.ClientId = builder.Configuration["Facebook:AppId"];       // FREE
    options.ClientSecret = builder.Configuration["Facebook:AppSecret"]; // FREE
    options.CallbackPath = "/signin-facebook";
    
    options.AuthorizationEndpoint = "https://www.facebook.com/v12.0/dialog/oauth";
    options.TokenEndpoint = "https://graph.facebook.com/v12.0/oauth/access_token";
    options.UserInformationEndpoint = "https://graph.facebook.com/me?fields=id,name,email";
});
```

---

### Microsoft/Azure AD (FREE TIER - 50K Users)
```
Free tier up to 50,000 Monthly Active Users
```

**Benefits**:
- ✅ **Free tier available** (no credit card)
- ✅ Up to 50,000 users free
- ✅ Enterprise-grade when you scale
- ✅ Works with Office 365 accounts
- ✅ Grows with you (pay only when scaling)

**Free Tier Includes**:
- SSO (Single Sign-On)
- Directory management
- Basic API access
- User management

**When to Upgrade**:
- More than 50,000 monthly active users
- Need production SLA
- Enterprise features

---

### Okta Developer Tier (FREE - 100 Users)
```
Surprisingly FREE with dev tier!
```

**Benefits**:
- ✅ **Completely free developer tier**
- ✅ Up to 100 users free
- ✅ SSO, MFA, API access all included
- ✅ No credit card needed
- ✅ Scale to paid when ready

**Free Tier Includes**:
- User management dashboard
- SSO (Single Sign-On)
- MFA (Multi-Factor Authentication)
- Full API access
- Email support

**When to Upgrade**:
- More than 100 users
- Production use (dev tier not for prod)
- Enterprise features

**Setup**:
```bash
dotnet add package Okta.AspNetCore
```

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    // Get from Okta dev org (FREE)
    options.Authority = builder.Configuration["Okta:Domain"];
    options.ClientId = builder.Configuration["Okta:ClientId"];
    options.ClientSecret = builder.Configuration["Okta:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});
```

---

## FREE Options Comparison

| Provider | Cost | Free Users | Setup Time | Best For |
|----------|------|------------|------------|----------|
| **Google OAuth** | ✅ FREE | Unlimited | 15 min | Consumer apps, widespread access |
| **GitHub OAuth** | ✅ FREE | Unlimited | 10 min | Developer tools, internal apps, open source |
| **Facebook OAuth** | ✅ FREE | Unlimited | 15 min | Consumer/social apps |
| **Microsoft/Azure AD** | ⚠️ FREE tier | Up to 50K | 20 min | Enterprise, Office 365 users |
| **Okta Dev Tier** | ⚠️ FREE tier | Up to 100 | 20 min | Enterprise SSO, small teams |

---

## Cost Comparison: All Options

| Method | Cost | Best For |
|--------|------|----------|
| **JWT (Self-Hosted)** | ✅ FREE | Any size app |
| **Google OAuth** | ✅ FREE | Consumer apps |
| **GitHub OAuth** | ✅ FREE | Dev tools, open source |
| **Facebook OAuth** | ✅ FREE | Consumer/social apps |
| **Azure AD Free** | ✅ FREE (< 50K users) | Teams, small enterprise |
| **Okta Free Dev Tier** | ✅ FREE (< 100 users) | Testing, dev, small teams |
| **JWT + API Keys** | ✅ FREE | Any size app |
| **JWT + OAuth (Hybrid)** | ✅ FREE | Maximum flexibility |
| **Okta Paid** | 💰 $2-4 per user/month | Enterprise, 100+ users |
| **Azure AD Paid** | 💰 $1-4 per user/month | Enterprise, 50K+ users |

---

## Recommended: Zero-Budget Google OAuth Example

Complete working example with **zero cost**:

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

```json
// appsettings.Development.json
{
  "Google": {
    "ClientId": "xxx.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-yyy"
  }
}
```

```csharp
// AuthController.cs
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("login/google")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action("GoogleCallback", "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback(string returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded)
            return BadRequest("Google authentication failed");

        var userEmail = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var userName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

        // Generate JWT or set session
        var token = GenerateJwtToken(userEmail, userName);

        return Ok(new 
        { 
            token, 
            userEmail, 
            userName,
            message = "Successfully logged in with Google - Zero cost!" 
        });
    }

    private string GenerateJwtToken(string email, string name)
    {
        // Your JWT generation logic
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-key"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "your-app",
            audience: "your-app",
            claims: new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Total Cost: $0 forever** ✅

---

## Comparison Matrix

| Method | Setup | Security | Scalability | 3rd Party | Cost | Best For |
|--------|-------|----------|-------------|-----------|------|----------|
| **None** | ⭐ Easy | ❌ None | N/A | No | ✅ FREE | Dev/internal |
| **API Key** | ⭐ Easy | ⭐⭐ Basic | ⭐⭐⭐ Good | No | ✅ FREE | Service-to-service |
| **JWT** | ⭐⭐ Medium | ⭐⭐⭐ Good | ⭐⭐⭐⭐⭐ Excellent | No | ✅ FREE | Modern APIs |
| **Google OAuth** | ⭐⭐ Easy | ⭐⭐⭐⭐ Strong | ⭐⭐⭐⭐⭐ Excellent | Yes | ✅ **FREE** | **Consumer apps** |
| **GitHub OAuth** | ⭐⭐ Easy | ⭐⭐⭐⭐ Strong | ⭐⭐⭐⭐⭐ Excellent | Yes | ✅ **FREE** | **Dev tools, open source** |
| **Facebook OAuth** | ⭐⭐ Medium | ⭐⭐⭐⭐ Strong | ⭐⭐⭐⭐⭐ Excellent | Yes | ✅ **FREE** | **Social apps** |
| **Azure AD** | ⭐⭐⭐ Hard | ⭐⭐⭐⭐⭐ Strong | ⭐⭐⭐⭐⭐ Excellent | Yes | ⚠️ FREE (50K) | **Enterprise** |
| **Okta** | ⭐⭐⭐ Medium | ⭐⭐⭐⭐⭐ Strong | ⭐⭐⭐⭐⭐ Excellent | Yes | ⚠️ FREE (100) | **Enterprise SSO** |
| **Sessions** | ⭐ Easy | ⭐⭐⭐ Good | ⭐⭐ Poor | No | ✅ FREE | Web apps |

---

## Swagger Configuration with Authentication

### JWT in Swagger UI

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

In Swagger UI, users see:
```
[Authorize ▼] Button
  ↓
Enter token: [________________________]
  ↓
All requests include: Authorization: Bearer <token>
```

### OAuth in Swagger UI

```csharp
options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
{
    Type = SecuritySchemeType.OAuth2,
    Flows = new OpenApiOAuthFlows
    {
        Implicit = new OpenApiOAuthFlow
        {
            AuthorizationUrl = new Uri("https://yourdomain.com/oauth/authorize"),
            TokenUrl = new Uri("https://yourdomain.com/oauth/token"),
            Scopes = new Dictionary<string, string>
            {
                { "read", "Read access" },
                { "write", "Write access" }
            }
        }
    }
});
```

---

## Authorization (Different from Authentication)

**Authentication**: "Who are you?" (Verified by token)  
**Authorization**: "What can you do?" (Verified by claims/roles)

```csharp
// Authentication: User is verified via JWT
[Authorize]
public async Task<ActionResult> GetProfile()
{
    // This runs only if token is valid
}

// Authorization: User must be Admin
[Authorize(Roles = "Admin")]
public async Task<ActionResult> DeleteUser(int id)
{
    // This runs only if user is Admin
}

// Authorization: User must have specific claim
[Authorize(Policy = "RequireEmail")]
public async Task<ActionResult> SendEmail()
{
    // This runs only if user has email claim
}
```

**Authorization Policies**:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireEmail", policy =>
        policy.RequireClaim("email"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("PremiumUsers", policy =>
        policy.RequireClaim("subscription", "premium"));

    options.AddPolicy("OwnerOrAdmin", policy =>
        policy.Requirements.Add(new OwnerOrAdminRequirement()));
});
```

---

## Token Refresh Strategy

For better security, use short-lived access tokens + long-lived refresh tokens:

```csharp
[HttpPost("api/v1/auth/login")]
public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
{
    var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);

    return Ok(new LoginResponseDto
    {
        AccessToken = GenerateAccessToken(user),      // Expires in 15 minutes
        RefreshToken = GenerateRefreshToken(user),    // Expires in 7 days
        ExpiresIn = 900  // 15 minutes
    });
}

[HttpPost("api/v1/auth/refresh")]
public async Task<ActionResult<LoginResponseDto>> RefreshToken(
    [FromBody] RefreshTokenDto dto)
{
    // Validate refresh token
    var userId = ValidateRefreshToken(dto.RefreshToken);
    if (userId == null)
        return Unauthorized();

    var user = await _userService.GetByIdAsync(userId.Value);

    return Ok(new LoginResponseDto
    {
        AccessToken = GenerateAccessToken(user),
        RefreshToken = GenerateRefreshToken(user),
        ExpiresIn = 900
    });
}
```

---

## Security Best Practices

✅ **DO**

- Use HTTPS in production (TLS 1.2+)
- Store secrets in environment variables (not appsettings.json)
- Use short-lived tokens (15-60 minutes)
- Implement token refresh mechanism
- Hash passwords (bcrypt, Argon2)
- Use strong secret keys (32+ characters)
- Validate all inputs
- Log failed authentication attempts
- Implement rate limiting
- Use CORS carefully

❌ **DON'T**

- Send credentials in URL query strings
- Store passwords in plain text
- Use weak secret keys
- Log sensitive information (tokens, passwords)
- Trust client-side validation only
- Disable HTTPS for "convenience"
- Use hardcoded secrets
- Allow unlimited login attempts
- Store tokens in localStorage (use httpOnly cookies)

---

## Recommended Approach: JWT + Refresh Token

```
Client              Your API
  │                   │
  ├─ Login ──────────→ │
  │ (username/pwd)    │
  │                   │ Hash password
  │                   │ Verify user
  │                   │
  │ ←─ Access Token ──┤ (15 min)
  │    Refresh Token  (7 days)
  │                   │
  ├─ Request ────────→ │
  │ + Access Token    │
  │                   │ Validate token
  │ ←─ Response ──────┤
  │                   │
  │ [15 min later]    │
  │                   │
  ├─ Refresh ────────→ │
  │ + Refresh Token   │
  │                   │ Validate refresh token
  │ ←─ New Token ─────┤
```

---

## Quick Decision Guide

| Scenario | Recommendation |
|----------|-----------------|
| Internal API (firewalled) | No authentication |
| Service-to-service | API Keys |
| Mobile/Web app | JWT + Refresh Token |
| Social login (Google, GitHub) | OAuth 2.0 |
| Enterprise/Microsoft stack | Azure AD |
| Server-rendered web app | Sessions |

---

**See Also**:
- [JWT Handbook](https://auth0.com/resources/ebooks/jwt-handbook)
- [OAuth 2.0 RFC 6749](https://tools.ietf.org/html/rfc6749)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
