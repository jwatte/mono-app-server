
using System;

namespace IMVU.IDL
{
	public class Buffer
	{
		public Buffer(byte[] data, int offset, int length)
		{
			this.data = data;
			this.offset = offset;
			this.length = length;
		}
		
		public byte[] data;
		public int offset;
		public int length;
	}
}
