using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Turbo.Runtime
{
	public abstract class MethodInvoker
	{
		private static readonly SimpleHashtable invokerFor = new SimpleHashtable(64u);

		private static int count;

		[DebuggerHidden, DebuggerStepThrough]
		public abstract object Invoke(object thisob, object[] parameters);

		private static bool DoesCallerRequireFullTrust(MethodInfo method)
		{
			var assembly = method.DeclaringType.Assembly;
			new FileIOPermission(PermissionState.None)
			{
				AllFiles = FileIOPermissionAccess.PathDiscovery
			}.Assert();
			var publicKey = assembly.GetName().GetPublicKey();
			return publicKey != null && publicKey.Length != 0 && CustomAttribute.GetCustomAttributes(assembly, typeof(AllowPartiallyTrustedCallersAttribute), true).Length == 0;
		}

		internal static MethodInvoker GetInvokerFor(MethodInfo method)
		{
			if (method.DeclaringType == typeof(CodeAccessPermission) && (method.Name == "Deny" || method.Name == "Assert" || method.Name == "PermitOnly"))
			{
				throw new TurboException(TError.CannotCallSecurityMethodLateBound);
			}
			var methodInvoker = invokerFor[method] as MethodInvoker;
			if (methodInvoker != null)
			{
				return methodInvoker;
			}
			if (!SafeToCall(method))
			{
				return null;
			}
			var requiresDemand = DoesCallerRequireFullTrust(method);
			var flag = false;
		    var obj = invokerFor;
		    Monitor.Enter(obj, ref flag);
		    methodInvoker = SpitAndInstantiateClassFor(method, requiresDemand);
		    invokerFor[method] = methodInvoker;
		    return methodInvoker;
		}

		private static bool SafeToCall(MethodBase meth)
		{
			var declaringType = meth.DeclaringType;
			return declaringType != null && declaringType != typeof(Activator) && declaringType != typeof(AppDomain) && declaringType != typeof(IsolatedStorageFile) && declaringType != typeof(MethodRental) && declaringType != typeof(TypeLibConverter) && declaringType != typeof(SecurityManager) && !typeof(Assembly).IsAssignableFrom(declaringType) && !typeof(MemberInfo).IsAssignableFrom(declaringType) && !typeof(ResourceManager).IsAssignableFrom(declaringType) && !typeof(Delegate).IsAssignableFrom(declaringType) && (declaringType.Attributes & TypeAttributes.HasSecurity) == TypeAttributes.NotPublic && (meth.Attributes & MethodAttributes.HasSecurity) == MethodAttributes.PrivateScope && (meth.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PrivateScope;
		}

		[ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
		private static MethodInvoker SpitAndInstantiateClassFor(MethodInfo method, bool requiresDemand)
		{
			var typeBuilder = Runtime.ThunkModuleBuilder.DefineType("invoker" + count++, TypeAttributes.Public, typeof(MethodInvoker));
			var methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual, typeof(object), new[]
			{
				typeof(object),
				typeof(object[])
			});
			if (requiresDemand)
			{
				methodBuilder.AddDeclarativeSecurity(SecurityAction.Demand, new NamedPermissionSet("FullTrust"));
			}
			methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypeReferences.debuggerStepThroughAttributeCtor, new object[0]));
			methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypeReferences.debuggerHiddenAttributeCtor, new object[0]));
			var iLGenerator = methodBuilder.GetILGenerator();
			if (!method.DeclaringType.IsPublic)
			{
				method = method.GetBaseDefinition();
			}
			var declaringType = method.DeclaringType;
			if (!method.IsStatic)
			{
				iLGenerator.Emit(OpCodes.Ldarg_1);
				if (declaringType.IsValueType)
				{
					Convert.EmitUnbox(iLGenerator, declaringType, Type.GetTypeCode(declaringType));
					Convert.EmitLdloca(iLGenerator, declaringType);
				}
				else
				{
					iLGenerator.Emit(OpCodes.Castclass, declaringType);
				}
			}
			var parameters = method.GetParameters();
			LocalBuilder[] array = null;
			var i = 0;
			var num = parameters.Length;
			while (i < num)
			{
				iLGenerator.Emit(OpCodes.Ldarg_2);
				ConstantWrapper.TranslateToILInt(iLGenerator, i);
				var type = parameters[i].ParameterType;
				if (type.IsByRef)
				{
					type = type.GetElementType();
					if (array == null)
					{
						array = new LocalBuilder[num];
					}
					array[i] = iLGenerator.DeclareLocal(type);
					iLGenerator.Emit(OpCodes.Ldelem_Ref);
					if (type.IsValueType)
					{
						Convert.EmitUnbox(iLGenerator, type, Type.GetTypeCode(type));
					}
					iLGenerator.Emit(OpCodes.Stloc, array[i]);
					iLGenerator.Emit(OpCodes.Ldloca, array[i]);
				}
				else
				{
					iLGenerator.Emit(OpCodes.Ldelem_Ref);
					if (type.IsValueType)
					{
						Convert.EmitUnbox(iLGenerator, type, Type.GetTypeCode(type));
					}
				}
				i++;
			}
			if (!method.IsStatic && method.IsVirtual && !method.IsFinal && (!declaringType.IsSealed || !declaringType.IsValueType))
			{
				iLGenerator.Emit(OpCodes.Callvirt, method);
			}
			else
			{
				iLGenerator.Emit(OpCodes.Call, method);
			}
			var returnType = method.ReturnType;
			if (returnType == typeof(void))
			{
				iLGenerator.Emit(OpCodes.Ldnull);
			}
			else if (returnType.IsValueType)
			{
				iLGenerator.Emit(OpCodes.Box, returnType);
			}
			if (array != null)
			{
				var j = 0;
				var num2 = parameters.Length;
				while (j < num2)
				{
					var localBuilder = array[j];
					if (localBuilder != null)
					{
						iLGenerator.Emit(OpCodes.Ldarg_2);
						ConstantWrapper.TranslateToILInt(iLGenerator, j);
						iLGenerator.Emit(OpCodes.Ldloc, localBuilder);
						var elementType = parameters[j].ParameterType.GetElementType();
						if (elementType.IsValueType)
						{
							iLGenerator.Emit(OpCodes.Box, elementType);
						}
						iLGenerator.Emit(OpCodes.Stelem_Ref);
					}
					j++;
				}
			}
			iLGenerator.Emit(OpCodes.Ret);
			return (MethodInvoker)Activator.CreateInstance(typeBuilder.CreateType());
		}
	}
}
