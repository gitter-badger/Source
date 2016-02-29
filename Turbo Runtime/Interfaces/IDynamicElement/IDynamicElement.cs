using System;
using System.Reflection;
// ReSharper disable UnusedParameter.Global

namespace Turbo.Runtime {
    public interface IDynamicElement : IReflect {
        FieldInfo AddField(string name);

        MethodInfo AddMethod(string name, Delegate method);
       
        PropertyInfo AddProperty(string name);
       
        void RemoveMember(MemberInfo m);
    }
}
