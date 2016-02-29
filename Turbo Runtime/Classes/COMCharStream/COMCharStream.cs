using System;
using System.IO;
using System.Text;

namespace Turbo.Runtime
{
	public class COMCharStream : Stream
	{
		private readonly IMessageReceiver messageReceiver;

		private StringBuilder buffer;

		public override bool CanWrite => true;

	    public override bool CanRead => false;

	    public override bool CanSeek => false;

	    public override long Length => buffer.Length;

	    public override long Position
		{
			get
			{
				return buffer.Length;
			}
			set
			{
			}
		}

		public COMCharStream(IMessageReceiver messageReceiver)
		{
			this.messageReceiver = messageReceiver;
			buffer = new StringBuilder(128);
		}

		public override void Close()
		{
			Flush();
		}

		public override void Flush()
		{
			messageReceiver.Message(buffer.ToString());
			buffer = new StringBuilder(128);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin) => 0L;

	    public override void SetLength(long value)
		{
			buffer.Length = (int)value;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			for (var i = count; i > 0; i--)
			{
				this.buffer.Append((char)buffer[offset++]);
			}
		}
	}
}
