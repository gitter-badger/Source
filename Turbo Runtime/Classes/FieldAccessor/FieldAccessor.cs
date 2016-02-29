using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Permissions;
using System.Threading;

namespace Turbo.Runtime
{
	public abstract class FieldAccessor
	{
		private static readonly SimpleHashtable accessorFor = new SimpleHashtable(32u);

		private static int count;

		[DebuggerHidden, DebuggerStepThrough]
		public abstract object GetValue(object thisob);

		[DebuggerHidden, DebuggerStepThrough]
		public abstract void SetValue(object thisob, object value);

		internal static FieldAccessor GetAccessorFor(FieldInfo field)
		{
			var fieldAccessor = accessorFor[field] as FieldAccessor;
			if (fieldAccessor != null)
			{
				return fieldAccessor;
			}
			var flag = false;
		    var obj = accessorFor;
		    Monitor.Enter(obj, ref flag);
		    fieldAccessor = SpitAndInstantiateClassFor(field);
		    accessorFor[field] = fieldAccessor;
		    return fieldAccessor;
		}

		[ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
		private static FieldAccessor SpitAndInstantiateClassFor(FieldInfo field)
		{
			var fieldType = field.FieldType;
			var typeBuilder = Runtime.ThunkModuleBuilder.DefineType("accessor" + count++, TypeAttributes.Public, typeof(FieldAccessor));
			var methodBuilder = typeBuilder.DefineMethod("GetValue", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual, typeof(object), new[]
			{
				typeof(object)
			});
			methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypeReferences.debuggerStepThroughAttributeCtor, new object[0]));
			methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypeReferences.debuggerHiddenAttributeCtor, new object[0]));
			var iLGenerator = methodBuilder.GetILGenerator();
			if (field.IsLiteral)
			{
				new ConstantWrapper(TypeReferences.GetConstantValue(field), null).TranslateToIL(iLGenerator, fieldType);
			}
			else if (field.IsStatic)
			{
				iLGenerator.Emit(OpCodes.Ldsfld, field);
			}
			else
			{
				iLGenerator.Emit(OpCodes.Ldarg_1);
				iLGenerator.Emit(OpCodes.Ldfld, field);
			}
			if (fieldType.IsValueType)
			{
				iLGenerator.Emit(OpCodes.Box, fieldType);
			}
			iLGenerator.Emit(OpCodes.Ret);
			methodBuilder = typeBuilder.DefineMethod("SetValue", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual, typeof(void), new[]
			{
				typeof(object),
				typeof(object)
			});
			methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypeReferences.debuggerStepThroughAttributeCtor, new object[0]));
			methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(TypeReferences.debuggerHiddenAttributeCtor, new object[0]));
			iLGenerator = methodBuilder.GetILGenerator();
			if (!field.IsLiteral)
			{
				if (!field.IsStatic)
				{
					iLGenerator.Emit(OpCodes.Ldarg_1);
				}
				iLGenerator.Emit(OpCodes.Ldarg_2);
				if (fieldType.IsValueType)
				{
					Convert.EmitUnbox(iLGenerator, fieldType, Type.GetTypeCode(fieldType));
				}
			    iLGenerator.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
			}
			iLGenerator.Emit(OpCodes.Ret);
			return (FieldAccessor)Activator.CreateInstance(typeBuilder.CreateType());
		}
	}
}
