using System;

namespace Compendium.Update
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class UpdateIgnoreUnityWarningsAttribute : Attribute
    {
    }
}