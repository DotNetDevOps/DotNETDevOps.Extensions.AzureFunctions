<h1 align="center">
  <br>
  Extensions for Azure Functions to run AspNetCore applications
  <br>
</h1>

> If you run into any inconsistencies, bugs or incompatibilities, kindly let us know and we'll do our best to address them.

<p align="center">
<a href="https://www.nuget.org/packages/DotNETDevOps.Extensions.AzureFunctions/"><img src="https://img.shields.io/nuget/v/DotNETDevOps.Extensions.AzureFunctions.svg?style=flat"></a>
<a href="https://www.dotnetdevops.org"><img src="https://img.shields.io/badge/Web-dotnetdevops.org-orange.svg"></a>
<a href="https://twitter.com/pksorensen"><img src="https://img.shields.io/badge/Twitter-%40pksorensen-blue.svg"></a>    
</p>

## Status
| Branch | Status | myget |
| ------ | ------ | ----- |
| Master | [![Master](https://dev.azure.com/dotnet-devops/DotNETDevOps/_apis/build/status/DotNETDevOps.Extensions.AzureFunctions?branchName=master)](https://dev.azure.com/dotnet-devops/DotNETDevOps/_build/latest?definitionId=6&branchName=master) |  <a href="https://www.myget.org/feed/dotnet-devops/package/nuget/DotNETDevOps.Extensions.AzureFunctions"><img src="https://img.shields.io/myget/dotnet-devops/v/DotNETDevOps.Extensions.AzureFunctions.svg"></a>   |
| Dev    | [![Build Status](https://dev.azure.com/dotnet-devops/DotNETDevOps/_apis/build/status/DotNETDevOps.Extensions.AzureFunctions?branchName=dev)](https://dev.azure.com/dotnet-devops/DotNETDevOps/_build/latest?definitionId=6&branchName=dev) | <a href="https://www.myget.org/feed/dotnet-devops/package/nuget/DotNETDevOps.Extensions.AzureFunctions"><img src="https://img.shields.io/myget/dotnet-devops/vpre/DotNETDevOps.Extensions.AzureFunctions.svg"></a>   
 |
 


## Usage

Create your function project and use the following boilerplate for a catchall route that delegates to the aspnet app using a custom binding.
```cs
    public class ServerlessApi 
    { 
        [FunctionName("AspNetCoreHost")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*all}")]HttpRequest req,
            [AspNetCoreRunner(Startup = typeof(Startup))] IAspNetCoreRunner aspNetCoreRunner,
            ExecutionContext executionContext)
        {

            return aspNetCoreRunner.RunAsync(executionContext);
        }
    }
```

if you want to customize the WebHostBuilder for the application, you may do so using the following example. Using the WebJobStartup AspNetCoreWebHostStartUp<TWebBuilder,TStartup>, it will auto registere it with DI and fire it up in your function.
```cs

    [assembly: WebJobsStartup(typeof(AspNetCoreWebHostStartUp<pksorensen.web.FunctionHost.WebBuilder, pksorensen.web.Startup>))]

    public class WebBuilder : IWebHostBuilderExtension<Startup>
    {
        private readonly IHostingEnvironment environment;

        public WebBuilder(IHostingEnvironment environment)
        {
            this.environment = environment;
        }
        public void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
        {

        }
        private void Logging(ILoggingBuilder b)
        {
            //b.AddProvider(new SerilogLoggerProvider(
            //            new LoggerConfiguration()
            //               .MinimumLevel.Verbose()
            //               .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose)
            //               .Enrich.FromLogContext()
            //                .WriteTo.File($"apptrace.log", buffered: true, flushToDiskInterval: TimeSpan.FromSeconds(30), rollOnFileSizeLimit: true, fileSizeLimitBytes: 1024 * 1024 * 32, rollingInterval: RollingInterval.Hour)
            //               .CreateLogger()));
        }

        public void ConfigureWebHostBuilder(ExecutionContext executionContext, WebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(ConfigureAppConfiguration);
            builder.ConfigureLogging(Logging);

            if (environment.IsDevelopment())
            {
                builder.UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../apps/pksorensen.web"));
            }
        }
    }

```

## Razor Pages and MVC apps

The following steps should allow you to setup a function host for your MVC/Razor application.

1. Open your solution with existing razor/mvc app, or create a new project with this and create your site.
2. Create new AzureFunction with http trigger. Project.Web.FunctionHost ect
3. Add a nuget.config file, for using prerelease packages (until its on nuget)
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="DotNetDevOps" value="https://www.myget.org/F/dotnet-devops" />
    <add key="NuGet" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```
4. Update your dependencies of the FunctionHost app, and also aspnet core 2.2 `<TargetFramework>netcoreapp2.2</TargetFramework>`
```
    <PackageReference Include="DotNETDevOps.Extensions.AzureFunctions" Version="1.0.0-pre-2019050410" />
    <PackageReference Include="Microsoft.AspNetCore.Hosting" Version="2.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.RazorPages" Version="2.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.Razor.Runtime" Version="2.2.0" />
    <PackageReference Include="Microsoft.NET.Sdk.Functions" Version="1.0.27" />
    <PackageReference Include="Newtonsoft.Json" Version="12.0.2" />
```
5. Update the function to the code snippet above and update the startup class name to yours.
6. Update host.json to remove api prefix on routes and add `    "ASPNETCORE_ENVIRONMENT": "Development" ` to local.settings.json
```
{
  "version": "2.0",
  "extensions": {
    "http": {
      "routePrefix": ""
    }
  }
}
```
7. Add `[assembly: WebJobsStartup(typeof(AspNetCoreWebHostStartUp))]` to your function.cs
8. To get the razor views compiled and outputed to your function bin folder you most update your FunctionHost.csproj file with post build events
```
xcopy /y "$(TargetDir)*.Views.dll" "$(TargetDir)bin\"
xcopy /y "$(TargetDir)*.Views.pdb" "$(TargetDir)bin\"
```
or copy paste 
```
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="xcopy /y &quot;$(TargetDir)*.Views.dll&quot; &quot;$(TargetDir)bin\&quot;&#xD;&#xA;xcopy /y &quot;$(TargetDir)*.Views.pdb&quot; &quot;$(TargetDir)bin\&quot;" />
  </Target>
 ```
 9. and aspnet core project.csproj with
 ```
    <RazorCompileOnBuild>True</RazorCompileOnBuild>
    <RazorCompileOnPublish>True</RazorCompileOnPublish>
    <RazorEmbeddedResource>True</RazorEmbeddedResource>
    <PreserveCompilationContext>true</PreserveCompilationContext>
    <MvcRazorExcludeRefAssembliesFromPublish>false</MvcRazorExcludeRefAssembliesFromPublish>
```