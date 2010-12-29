
using System;

namespace IMVU.IDL
{
	public class Buffer
	{
		public Buffer(byte[] data, int offset, int length)
			: this(data, offset, length, null)
		{
		}
		public Buffer(byte[] data, int offset, int length, string contenttype)
		{
			this.data = data;
			this.offset = offset;
			this.length = length;
			this.contenttype = contenttype;
		}
		
		public byte[] data;
		public int offset;
		public int length;
		public string contenttype;
	}
}
