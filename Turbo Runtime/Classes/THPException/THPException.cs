using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
	[Serializable]
	public sealed class THPException : ExternalException
	{
		public new ETHPError ErrorCode => (ETHPError)HResult;

	    public THPException()
		{
		}

		public THPException(string message) : base(message)
		{
		}

		public THPException(string message, Exception innerException) : base(message, innerException)
		{
		}

	    private THPException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			HResult = (int)info.GetValue("VsaException_HResult", typeof(int));
			HelpLink = (string)info.GetValue("VsaException_HelpLink", typeof(string));
			Source = (string)info.GetValue("VsaException_Source", typeof(string));
		}

		[SecurityCritical]
		[PermissionSet(SecurityAction.Demand, Name = "FullTrust"), SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("VsaException_HResult", HResult);
			info.AddValue("VsaException_HelpLink", HelpLink);
			info.AddValue("VsaException_Source", Source);
		}

		public override string ToString()
		{
			if ("" != Message)
			{
				return string.Concat("Turbo.Runtime.THPException: ", Enum.GetName(((ETHPError)HResult).GetType(), (ETHPError)HResult), " (0x", string.Format(CultureInfo.InvariantCulture, "{0,8:X}", new object[]
				{
				    HResult
				}), "): ", Message);
			}
			return string.Concat("Turbo.Runtime.THPException: ", Enum.GetName(((ETHPError)HResult).GetType(), (ETHPError)HResult), " (0x", string.Format(CultureInfo.InvariantCulture, "{0,8:X}", new object[]
			{
			    HResult
			}), ").");
		}

		public THPException(ETHPError error) : base(string.Empty, (int)error)
		{
		}

		public THPException(ETHPError error, string message) : base(message, (int)error)
		{
		}

		public THPException(ETHPError error, string message, Exception innerException) : base(message, innerException)
		{
			HResult = (int)error;
		}
	}
}
