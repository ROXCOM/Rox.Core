using System;
using System.Collections.Generic;
using System.Text;

namespace Rox.Core
{
    public delegate void ActionParams<in k>(k instance, params object[] parametrs);
    public delegate T FuncParams<in k, out T>(k instance, params object[] parametrs);
    public delegate T FuncParams<out T>(params object[] parametrs);
}
