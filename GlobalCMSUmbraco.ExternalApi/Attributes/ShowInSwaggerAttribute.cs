using System;

namespace GlobalCMSUmbraco.ExternalApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ShowInSwaggerAttribute : Attribute
    {
    }
}
