
using System;

namespace IMVU.IDL
{
    public abstract class ApiType
	{
		protected ApiType(Type type)
		{
			this.Type = type;
			this.Name = type.Name;
		}
		
		public readonly Type Type;
		public readonly string Name;
		
		public abstract object ConvertFromString(string str);
		public abstract string ConvertToString(object obj);
	}
	
	public delegate object ConvertFromStringFunc(string str);
	public delegate string ConvertToStringFunc(object obj);
	public delegate T GenericFromStringFunc<T>(string str) where T : new();
	public delegate string GenericToStringFunc<T>(T t) where T : new();
	
	public class ApiTypeImpl : ApiType
	{
		public ApiTypeImpl(Type t, ConvertFromStringFunc fstr, ConvertToStringFunc tstr)
			: base(t)
		{
			this.fstr = fstr;
			this.tstr = tstr;
		}
		ConvertFromStringFunc fstr;
		ConvertToStringFunc tstr;
		public override object ConvertFromString(string str)
		{
			return fstr(str);
		}
		public override string ConvertToString(object obj)
		{
			return tstr(obj);
		}
	}
	
	public class ApiTypeGeneric<T> : ApiType where T : new()
	{
		public ApiTypeGeneric(GenericFromStringFunc<T> fstr, GenericToStringFunc<T> tstr)
			: base(typeof(T))
		{
			this.fstr = fstr;
			this.tstr = tstr;
		}
		
		GenericFromStringFunc<T> fstr;
		GenericToStringFunc<T> tstr;
		
		public override object ConvertFromString(string str)
		{
			return fstr(str);
		}
		
		public override string ConvertToString(object obj)
		{
			return tstr((T)obj);
		}
	}
}
