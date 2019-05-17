using System;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class WebHostBuilderAttribute : Attribute
    {
        public WebHostBuilderAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
