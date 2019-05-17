using System;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    [Obsolete("Use IWebHostBuilderExtension instead to get access to the hole webbuilder pipeline")]
    public class AspNetDevelopmentRelativePathAttribute : Attribute
    {

        public AspNetDevelopmentRelativePathAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
