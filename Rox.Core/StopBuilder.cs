using System.Reflection;

namespace Rox.Core
{
    public class StopBuilder : StartBuilder
    {
        public StopBuilder(MethodInfo configure) : base(configure)
        {
        }
    }
}