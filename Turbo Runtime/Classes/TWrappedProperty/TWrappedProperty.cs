using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class TWrappedProperty : PropertyInfo, IWrappedMember
	{
		internal object obj;

		internal PropertyInfo property;

		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Property;
			}
		}

		public override string Name
		{
			get
			{
				if (this.obj is LenientGlobalObject && this.property.Name.StartsWith("Slow", StringComparison.Ordinal))
				{
					return this.property.Name.Substring(4);
				}
				return this.property.Name;
			}
		}

		public override Type DeclaringType
		{
			get
			{
				return this.property.DeclaringType;
			}
		}

		public override Type ReflectedType
		{
			get
			{
				return this.property.ReflectedType;
			}
		}

		public override PropertyAttributes Attributes
		{
			get
			{
				return this.property.Attributes;
			}
		}

		public override bool CanRead
		{
			get
			{
				return this.property.CanRead;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return this.property.CanWrite;
			}
		}

		public override Type PropertyType
		{
			get
			{
				return this.property.PropertyType;
			}
		}

		internal TWrappedProperty(PropertyInfo property, object obj)
		{
			this.obj = obj;
			this.property = property;
			if (obj is TObject)
			{
				var declaringType = property.DeclaringType;
				if (declaringType == Typeob.Object || declaringType == Typeob.String || declaringType.IsPrimitive || declaringType == Typeob.Array)
				{
					if (obj is BooleanObject)
					{
						this.obj = ((BooleanObject)obj).value;
						return;
					}
					if (obj is NumberObject)
					{
						this.obj = ((NumberObject)obj).value;
						return;
					}
					if (obj is StringObject)
					{
						this.obj = ((StringObject)obj).value;
						return;
					}
					if (obj is ArrayWrapper)
					{
						this.obj = ((ArrayWrapper)obj).value;
					}
				}
			}
		}

		internal virtual string GetClassFullName()
		{
			if (this.property is TProperty)
			{
				return ((TProperty)this.property).GetClassFullName();
			}
			return this.property.DeclaringType.FullName;
		}

		public override object[] GetCustomAttributes(Type t, bool inherit)
		{
			return CustomAttribute.GetCustomAttributes(this.property, t, inherit);
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.property.GetCustomAttributes(inherit);
		}

		[DebuggerHidden, DebuggerStepThrough]
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			return this.property.GetValue(this.obj, invokeAttr, binder, index, culture);
		}

		[DebuggerHidden, DebuggerStepThrough]
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			this.property.SetValue(this.obj, value, invokeAttr, binder, index, culture);
		}

		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			return this.property.GetAccessors(nonPublic);
		}

		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			var getMethod = TProperty.GetGetMethod(this.property, nonPublic);
			if (getMethod == null)
			{
				return null;
			}
			return new TWrappedMethod(getMethod, this.obj);
		}

		public override ParameterInfo[] GetIndexParameters()
		{
			return this.property.GetIndexParameters();
		}

		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			var setMethod = TProperty.GetSetMethod(this.property, nonPublic);
			if (setMethod == null)
			{
				return null;
			}
			return new TWrappedMethod(setMethod, this.obj);
		}

		public object GetWrappedObject()
		{
			return this.obj;
		}

		public override bool IsDefined(Type type, bool inherit)
		{
			return CustomAttribute.IsDefined(this.property, type, inherit);
		}
	}
}
