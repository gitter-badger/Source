namespace Turbo.Runtime
{
	internal sealed class TKeyword
	{
		private readonly TKeyword next;

		private readonly TToken token;

		private readonly string name;

		private readonly int length;

		private TKeyword(TToken token, string name)
		{
			this.name = name;
			next = null;
			this.token = token;
			length = this.name.Length;
		}

		private TKeyword(TToken token, string name, TKeyword next)
		{
			this.name = name;
			this.next = next;
			this.token = token;
			length = this.name.Length;
		}

		internal static string CanBeIdentifier(TToken keyword)
		{
			switch (keyword)
			{
			case TToken.Package:
				return "package";
			case TToken.Internal:
				return "internal";
			case TToken.Abstract:
				return "abstract";
			case TToken.Public:
				return "public";
			case TToken.Static:
				return "static";
			case TToken.Private:
				return "private";
			case TToken.Protected:
				return "protected";
			case TToken.Final:
				return "final";
			case TToken.Event:
				return "event";
			default:
			        if (keyword == TToken.Void) return "void";
			        switch (keyword)
			        {
			            case TToken.Get:
			                return "get";
			            case TToken.Implements:
			                return "implements";
			            case TToken.Interface:
			                return "interface";
			            case TToken.Set:
			                return "set";
			            case TToken.Assert:
			                return "assert";
			            case TToken.Boolean:
			                return "boolean";
			            case TToken.Byte:
			                return "byte";
			            case TToken.Char:
			                return "char";
			            case TToken.Decimal:
			                return "decimal";
			            case TToken.Double:
			                return "double";
			            case TToken.Enum:
			                return "enum";
			            case TToken.Ensure:
			                return "ensure";
			            case TToken.Float:
			                return "float";
			            case TToken.Goto:
			                return "goto";
			            case TToken.Int:
			                return "int";
			            case TToken.Invariant:
			                return "invariant";
			            case TToken.Long:
			                return "long";
			            case TToken.Native:
			                return "native";
			            case TToken.Require:
			                return "require";
			            case TToken.Sbyte:
			                return "sbyte";
			            case TToken.Short:
			                return "short";
			            case TToken.Synchronized:
			                return "synchronized";
			            case TToken.Transient:
			                return "transient";
			            case TToken.Throws:
			                return "throws";
			            case TToken.Volatile:
			                return "volatile";
			            case TToken.Ushort:
			                return "ushort";
			            case TToken.Uint:
			                return "uint";
			            case TToken.Ulong:
			                return "ulong";
			            case TToken.Use:
			                return "use";
			        }
			        return null;
			}
		}

		internal TToken GetKeyword(Context token, int length)
		{
			var jSKeyword = this;
			IL_71:
			while (jSKeyword != null)
			{
				if (length == jSKeyword.length)
				{
					var i = 1;
					var num = token.startPos + 1;
					while (i < length)
					{
						var c = jSKeyword.name[i];
						var c2 = token.source_string[num];
						if (c != c2)
						{
							if (c2 < c)
							{
								return TToken.Identifier;
							}
							jSKeyword = jSKeyword.next;
							goto IL_71;
						}
					    i++;
					    num++;
					}
					return jSKeyword.token;
				}
				if (length < jSKeyword.length)
				{
					return TToken.Identifier;
				}
				jSKeyword = jSKeyword.next;
			}
			return TToken.Identifier;
		}

		internal static TKeyword[] InitKeywords()
		{
			var arg_22_0 = new TKeyword[26];
			var jSKeyword = new TKeyword(TToken.Abstract, "abstract");
			jSKeyword = new TKeyword(TToken.Assert, "assert", jSKeyword);
			arg_22_0[0] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Boolean, "boolean");
			jSKeyword = new TKeyword(TToken.Break, "break", jSKeyword);
			jSKeyword = new TKeyword(TToken.Byte, "byte", jSKeyword);
			arg_22_0[1] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Continue, "continue");
			jSKeyword = new TKeyword(TToken.Const, "const", jSKeyword);
			jSKeyword = new TKeyword(TToken.Class, "class", jSKeyword);
			jSKeyword = new TKeyword(TToken.Catch, "catch", jSKeyword);
			jSKeyword = new TKeyword(TToken.Char, "char", jSKeyword);
			jSKeyword = new TKeyword(TToken.Case, "case", jSKeyword);
			arg_22_0[2] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Debugger, "debugger");
			jSKeyword = new TKeyword(TToken.Default, "default", jSKeyword);
			jSKeyword = new TKeyword(TToken.Double, "double", jSKeyword);
			jSKeyword = new TKeyword(TToken.Delete, "delete", jSKeyword);
			jSKeyword = new TKeyword(TToken.Do, "do", jSKeyword);
			arg_22_0[3] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Extends, "extends");
			jSKeyword = new TKeyword(TToken.Export, "export", jSKeyword);
			jSKeyword = new TKeyword(TToken.Ensure, "ensure", jSKeyword);
			jSKeyword = new TKeyword(TToken.Event, "event", jSKeyword);
			jSKeyword = new TKeyword(TToken.Enum, "enum", jSKeyword);
			jSKeyword = new TKeyword(TToken.Else, "else", jSKeyword);
			arg_22_0[4] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Function, "function");
			jSKeyword = new TKeyword(TToken.Finally, "finally", jSKeyword);
			jSKeyword = new TKeyword(TToken.Float, "float", jSKeyword);
			jSKeyword = new TKeyword(TToken.Final, "final", jSKeyword);
			jSKeyword = new TKeyword(TToken.False, "false", jSKeyword);
			jSKeyword = new TKeyword(TToken.For, "for", jSKeyword);
			arg_22_0[5] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Goto, "goto");
			jSKeyword = new TKeyword(TToken.Get, "get", jSKeyword);
			arg_22_0[6] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Instanceof, "instanceof");
			jSKeyword = new TKeyword(TToken.Implements, "implements", jSKeyword);
			jSKeyword = new TKeyword(TToken.Invariant, "invariant", jSKeyword);
			jSKeyword = new TKeyword(TToken.Interface, "interface", jSKeyword);
			jSKeyword = new TKeyword(TToken.Internal, "internal", jSKeyword);
			jSKeyword = new TKeyword(TToken.Import, "import", jSKeyword);
			jSKeyword = new TKeyword(TToken.Int, "int", jSKeyword);
			jSKeyword = new TKeyword(TToken.In, "in", jSKeyword);
			jSKeyword = new TKeyword(TToken.If, "if", jSKeyword);
			arg_22_0[8] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Long, "long");
			arg_22_0[11] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Native, "native", jSKeyword);
			jSKeyword = new TKeyword(TToken.Null, "null", jSKeyword);
			jSKeyword = new TKeyword(TToken.New, "new", jSKeyword);
			arg_22_0[13] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Protected, "protected");
			jSKeyword = new TKeyword(TToken.Private, "private", jSKeyword);
			jSKeyword = new TKeyword(TToken.Package, "package", jSKeyword);
			jSKeyword = new TKeyword(TToken.Public, "public", jSKeyword);
			arg_22_0[15] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Require, "require");
			jSKeyword = new TKeyword(TToken.Return, "return", jSKeyword);
			arg_22_0[17] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Synchronized, "synchronized");
			jSKeyword = new TKeyword(TToken.Switch, "switch", jSKeyword);
			jSKeyword = new TKeyword(TToken.Static, "static", jSKeyword);
			jSKeyword = new TKeyword(TToken.Super, "super", jSKeyword);
			jSKeyword = new TKeyword(TToken.Short, "short", jSKeyword);
			jSKeyword = new TKeyword(TToken.Set, "set", jSKeyword);
			arg_22_0[18] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Transient, "transient");
			jSKeyword = new TKeyword(TToken.Typeof, "typeof", jSKeyword);
			jSKeyword = new TKeyword(TToken.Throws, "throws", jSKeyword);
			jSKeyword = new TKeyword(TToken.Throw, "throw", jSKeyword);
			jSKeyword = new TKeyword(TToken.True, "true", jSKeyword);
			jSKeyword = new TKeyword(TToken.This, "this", jSKeyword);
			jSKeyword = new TKeyword(TToken.Try, "try", jSKeyword);
			arg_22_0[19] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Volatile, "volatile");
			jSKeyword = new TKeyword(TToken.Void, "void", jSKeyword);
			jSKeyword = new TKeyword(TToken.Var, "var", jSKeyword);
			arg_22_0[21] = jSKeyword;
			jSKeyword = new TKeyword(TToken.Use, "use");
			arg_22_0[20] = jSKeyword;
			jSKeyword = new TKeyword(TToken.While, "while");
			jSKeyword = new TKeyword(TToken.With, "with", jSKeyword);
			arg_22_0[22] = jSKeyword;
			return arg_22_0;
		}
	}
}
