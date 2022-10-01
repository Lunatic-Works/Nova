using System;

namespace Nova
{
    // prevent subclass from being exported
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class ExportCustomType : Attribute { }
}
