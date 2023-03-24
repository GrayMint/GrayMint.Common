using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.Test.Api;
using GrayMint.Common.Test.WebApiSample;
using Microsoft.Extensions.Options;
using GrayMint.Common.Test.WebApiSample.Security;

namespace GrayMint.Common.Test.Helper;

public class TestInit : IDisposable
{
    public WebApplicationFactory<Program> WebApp { get; }
    public HttpClient HttpClient { get; set; }
    public IServiceScope Scope { get; }
    public AuthenticationHeaderValue AppCreatorAuthenticationHeader { get; private set; } = default!;
    public App App { get; private set; } = default!;
    public CognitoAuthenticationOptions CognitoAuthenticationOptions => WebApp.Services.GetRequiredService<IOptions<CognitoAuthenticationOptions>>().Value;
    public AppsClient AppsClient => new(HttpClient);
    public UsersClient UsersClient => new(HttpClient);
    public ItemsClient ItemsClient => new(HttpClient);


    private TestInit(Dictionary<string, string?> appSettings, string environment)
    {
        // Application
        WebApp = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                foreach (var appSetting in appSettings)
                    builder.UseSetting(appSetting.Key, appSetting.Value);

                builder.UseEnvironment(environment);
                builder.ConfigureServices(services =>
                {
                    _ = services;
                });
            });

        // Client
        HttpClient = WebApp.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Create System user
        Scope = WebApp.Services.CreateScope();
    }

    private async Task Init()
    {
        var appCreatorUser = await CreateUserAndAddToRole(NewEmail(), Roles.SystemAdmin);
        AppCreatorAuthenticationHeader = await CreateAuthorizationHeader(appCreatorUser.Email);
        HttpClient.DefaultRequestHeaders.Authorization = AppCreatorAuthenticationHeader;
        App = await AppsClient.CreateAppAsync(Guid.NewGuid().ToString());
    }

    public async Task<AuthenticationHeaderValue> CreateAuthorizationHeader(string email)
    {
        var tokenBuilder = Scope.ServiceProvider.GetRequiredService<BotAuthenticationTokenBuilder>();
        return await tokenBuilder.CreateAuthenticationHeader(email, email);
    }

    public Task<User<string>> CreateUserAndAddToRole(string email, SimpleRole simpleRole, string appId = "*")
    {
        return CreateUserAndAddToRole(email, simpleRole.RoleName, appId);
    }

    public async Task<User<string>> CreateUserAndAddToRole(string email, string roleName, string appId = "*")
    {
        // find the role
        var roleProvider = Scope.ServiceProvider.GetRequiredService<SimpleRoleProvider>();
        var role = await roleProvider.GetByName(roleName);

        // create user
        var userProvider = Scope.ServiceProvider.GetRequiredService<SimpleUserProvider>();
        var user = await userProvider.FindByEmail(email) ??
                   await userProvider.Create(new UserCreateRequest
                   {
                       Email = email,
                       FirstName = Guid.NewGuid().ToString(),
                       LastName = Guid.NewGuid().ToString(),
                       Description = Guid.NewGuid().ToString()
                   });

        await roleProvider.AddUser(role.RoleId, user.UserId, appId);
        return user;
    }

    public static async Task<TestInit> Create(Dictionary<string, string?>? appSettings = null, string environment = "Development")
    {
        appSettings ??= new Dictionary<string, string?>();
        var testInit = new TestInit(appSettings, environment);
        await testInit.Init();
        return testInit;
    }

    public static string NewEmail()
    {
        return $"{Guid.NewGuid()}@local";
    }

    public void Dispose()
    {
        Scope.Dispose();
        HttpClient.Dispose();
        WebApp.Dispose();
    }
}