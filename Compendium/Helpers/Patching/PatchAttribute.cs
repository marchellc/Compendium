using System;

namespace Compendium.Helpers.Patching
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : Attribute
    {
        public PatchData? Patch { get; set; }

        public PatchAttribute() { }
        public PatchAttribute(Type type, string targetName)
        {
            Patch = PatchData
                .New()
                .WithTarget(type, targetName);
        }
    }
}