# Extensions for Azure Functions to run AspNetCore applications.

[![Master](https://dev.azure.com/dotnet-devops/DotNETDevOps/_apis/build/status/DotNetDevOps.DotNETDevOps.Extensions.AzureFunctions?branchName=master)](https://dev.azure.com/dotnet-devops/DotNETDevOps/_build/latest?definitionId=6&branchName=master)


## Usage

You may use the `AspNetDevelopmentRelativePathAttribute` to specify the relativepath from your function project to the project that has your aspnet core application. 
This allows it to use wwwroot folder from setting content root to your project folder. This is needed, since visual studio do not copy all files over when building/running in visual studio. 
On publish, everything works without needing this.

Remember to set `"ASPNETCORE_ENVIRONMENT": "Development"` in your ´local.settings.json`, otherwise the attribute wont be used.

Depend on `IAspNetCoreRunner<T>´ in your class that contains your function runner, and delegate the request to this for running the application. See example.


```
using IOBoard.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Hosting;
using System.Threading.Tasks;

[assembly: WebJobsStartup(typeof(AspNetCoreWebHostStartUp))]

namespace IOBoard.Portal.FunctionHost
{


    [AspNetDevelopmentRelativePath("../../../../../apps/IO-Board.Portal")]
    public class ServerlessApi
    {
        private readonly IAspNetCoreRunner<ServerlessApi> aspNetCoreRunner;

        public ServerlessApi(IAspNetCoreRunner<ServerlessApi> aspNetCoreRunner)
        {
            this.aspNetCoreRunner = aspNetCoreRunner;
        }


        [FunctionName("PortalBackend")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*all}")]HttpRequest req, ExecutionContext executionContext)
            => aspNetCoreRunner.RunAsync<PortalHostStartup>(req,executionContext);

       

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