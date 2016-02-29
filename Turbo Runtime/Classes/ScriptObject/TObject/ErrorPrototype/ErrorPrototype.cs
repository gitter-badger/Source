namespace Turbo.Runtime
{
	public class ErrorPrototype : TObject
	{
		public readonly string name;

		internal static readonly ErrorPrototype ob = new ErrorPrototype(ObjectPrototype.ob, "Error");

		internal ErrorConstructor _constructor;

		public ErrorConstructor constructor => _constructor;

	    internal ErrorPrototype(ScriptObject parent, string name) : base(parent)
		{
			this.name = name;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Error_toString)]
		public static string toString(object thisob)
		{
			if (!(thisob is ErrorObject))
			{
				return thisob.ToString();
			}
			var message = ((ErrorObject)thisob).Message;
		    return message.Length == 0
		        ? LateBinding.GetMemberValue(thisob, "name").ToString()
		        : LateBinding.GetMemberValue(thisob, "name") + ": " + message;
		}
	}
}
