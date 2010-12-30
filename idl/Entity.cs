using System;
using System.Reflection;
using IMVU.IDL;
using System.ComponentModel;

namespace entity
{
	public class EntityInfo
	{
		public EntityInfo(
		                  string typeName,
		                  string keyName,
		                  string createPermission,
		                  string readPermission,
		                  string updatePermission,
		                  string deletePermission)
		{
			this.typeName = typeName;
			this.keyName = keyName;
			this.createPermission = createPermission;
			this.readPermission = readPermission;
			this.updatePermission = updatePermission;
			this.deletePermission = deletePermission;
		}
		public readonly string typeName;
		public readonly string keyName;
		public readonly string createPermission;
		public readonly string readPermission;
		public readonly string updatePermission;
		public readonly string deletePermission;
	}
	
	public abstract class Entity
	{
		protected Entity(EntityInfo info)
		{
			this.entityInfo = info;
		}
		
		//	name of the property that contains the key value
		[NonSerialized] public readonly EntityInfo entityInfo;
		//	version of the data as gotten from KeyValueStore
		[NonSerialized] public long lastVersion;

		//	return the value of the property that is the key
		public abstract string KeyValue { get; }

		public static T Load<T>(string key, IContext ctx) where T : Entity
		{
			return (T)Load(key, ctx, typeof(T));
		}
		
		public static Entity Load(string key, IContext ctx, Type t)
		{
			if (!typeof(Entity).IsAssignableFrom(t))
			{
				throw new ArgumentException("Only Entity subtypes can be loaded with Entity.Load()");
			}
			Entity inst = (Entity)t.Assembly.CreateInstance(t.Name);
			return LoadInstance(key, ctx, inst);
		}
		
		private static Entity LoadInstance(string key, IContext ctx, Entity inst)
		{
			ctx.VerifyPermission(inst.entityInfo.readPermission, key, inst.entityInfo.keyName, "load " + inst.entityInfo.typeName + " " + key);
			dict data = KeyValueStore.Find(inst.entityInfo.typeName + ":" + key.ToString(), out inst.lastVersion);
			if (data == null)
			{
				return null;
			}
			LoadFromDict(data, inst);
			return inst;
		}

		private static void LoadFromDict(dict data, Entity ent)
		{
			Type t = ent.GetType();
			PropertyInfo[] pis = t.GetProperties(BindingFlags.Public | BindingFlags.SetProperty);
			foreach (PropertyInfo pi in pis)
			{
				object val;
				if (data.TryGetValue(pi.Name, out val))
				{
					pi.SetValue(ent, CoerceType(val, pi.PropertyType, pi.Name, t.Name), null);
				}
			}
		}

		static object CoerceType(object val, Type toType, string name, string entName)
		{
			if (val == null)
			{
				if (toType.IsValueType)
				{
					throw new ArgumentException("Cannot coerce null to value type " + toType.Name +
					                            " for property " + name + " of " + entName);
				}
				return null;
			}
			if (toType.IsAssignableFrom(val.GetType()))
			{
				return val;
			}
			if (typeof(IJSONString).IsAssignableFrom(toType))
			{
				return toType.Assembly.CreateInstance(toType.Name, false, BindingFlags.Default, null, new object[] { val.ToString() }, null, null);
			}
			try
			{
				MethodInfo mi = toType.GetMethod("Parse", BindingFlags.InvokeMethod | BindingFlags.Static);
				if (mi != null)
				{
					return mi.Invoke(null, new object[] { val.ToString() });
				}
			}
			catch (System.Exception)
			{
				//	don't do anything here, because I will throw a better error right below
			}
			throw new InvalidOperationException("Cannot coerce " + val.GetType().Name + " to " +
			                                    toType.Name + " for property " + name + " of " + entName);
		}
	}
}
