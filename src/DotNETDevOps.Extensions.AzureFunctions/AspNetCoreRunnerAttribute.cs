using System;
using Microsoft.Azure.WebJobs.Description;


namespace DotNETDevOps.Extensions.AzureFunctions
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class AspNetCoreRunnerAttribute : Attribute
    {
        public Type Startup { get; set; }

        public string ConnectionName { get; set; }

        [AutoResolve]
        public string TaskHub { get; set; }
        public Type WebBuilderExtension { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    this.TaskHub?.GetHashCode() ?? 0 +
                    this.ConnectionName?.GetHashCode() ?? 0 +
                    this.Startup?.GetHashCode() ?? 0;
            }
        }
        public override bool Equals(object obj)
        {
            return this.Equals(obj as AspNetCoreRunnerAttribute);
        }
        public bool Equals(AspNetCoreRunnerAttribute other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(this.TaskHub, other.TaskHub, StringComparison.OrdinalIgnoreCase)
                && string.Equals(this.ConnectionName, other.ConnectionName, StringComparison.OrdinalIgnoreCase)
                && this.Startup == other.Startup;
        }
    }
}
