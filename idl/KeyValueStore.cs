using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace IMVU.IDL
{
	//	Persistent storage of key/value mappings.
	//	This uses a disk log file, that can be replayed when re-starting.
	//	This particular implementation doesn't scale forever. Oh, well.
	//	Todo: throw a transaction exception instead of return false on failure.
	//	Todo: allow multi-update batching into a single transaction.
	//	Todo: support networked operation
	public class KeyValueStore
	{
		static Dictionary<string, Record> data;
		static FileStream diskLog;
		static BinaryWriter diskWriter;
		
		class Record
		{
			internal long version;
			internal long expiry;
			internal Buffer text;
		}

		static string FileVersionString = "0.1";
		
		static KeyValueStore()
		{
			data = new Dictionary<string, Record>();
			lock (data)
			{
				try
				{
					long nowTicks = DateTime.Now.Ticks;
					using (FileStream fs = new FileStream("KeyValue.bin", FileMode.Open))
					{
						int nr = 0;
						BinaryReader br = new BinaryReader(fs, Encoding.UTF8);
						string key;
						long ver;
						key = br.ReadString();
						if (key.Split(';')[0] != FileVersionString)
						{
							throw new InvalidDataException("KeyValue file format is too new: " + key);
						}
						try
						{
							while ((key = br.ReadString()) != null)
							{
								data.Remove(key);
								ver = br.ReadInt64();
								if (ver > 0)
								{
									long exp = br.ReadInt64();
									int len = br.ReadInt32();
									byte[] buf = br.ReadBytes(len);
									Record r = new Record();
									r.version = ver;
									r.expiry = exp;
									r.text = new Buffer(buf, 0, len);
									if (r.expiry > nowTicks)
									{
										data.Add(key, r);
									}
								}
								++nr;
							}
						}
						catch (EndOfStreamException eos)
						{
							//	ignore end of stream -- I read everything that was there
							GC.KeepAlive(eos);
						}
						Services.Log("KeyValueStore loaded {0} records", nr);
					}	
					//	rewrite the log in compacted form (no duplicates)}
					using (FileStream fs = new FileStream("KeyValue.tmp", FileMode.Create))
					{
						int nr = 0;
						BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8);
						bw.Write(FileVersionString);	//	version, no more information
						foreach (KeyValuePair<string, Record> kvp in data)
						{
							Debug.Assert(kvp.Value.version > 0);
							bw.Write(kvp.Key);
							bw.Write(kvp.Value.version);
							bw.Write(kvp.Value.expiry);
							bw.Write(kvp.Value.text.length);
							bw.Write(kvp.Value.text.data, kvp.Value.text.offset, kvp.Value.text.length);
							++nr;
						}
						Services.Log("KeyValueStore saved {0} records after compaction", nr);
					}
					File.Delete("KeyValue.bin");
					File.Move("KeyValue.tmp", "KeyValue.bin");
				}
				catch (System.Exception x)
				{
					Services.Log("Exception when loading KeyValueStore: {0}", x.Message);
				}
				try
				{
					diskLog = new FileStream("KeyValue.bin", FileMode.Open);
					diskLog.Seek(0, SeekOrigin.End);
					diskWriter = new BinaryWriter(diskLog, Encoding.UTF8);
					Services.Log("Appending to existing file KeyValue.bin");
				}
				catch (FileNotFoundException fnf)
				{
					Services.Log("Creating new file KeyValue.bin");
					diskLog = new FileStream("KeyValue.bin", FileMode.CreateNew);
					diskWriter = new BinaryWriter(diskLog, Encoding.UTF8);
					diskWriter.Write(FileVersionString);
					GC.KeepAlive(fnf);	//	ignore fnf
				}
			}
		}
		
		static void DiskLogUpdate(string key, Record val)
		{
			//	todo: queue this to batch once a second or whatever.
			//	When doing so, copy the text and version number out of "record" because
			//	relation may go stale after this function returns.
			diskWriter.Write(key);
			Debug.Assert(val.version > 0);
			diskWriter.Write(val.version);
			diskWriter.Write(val.expiry);
			diskWriter.Write(val.text.length);
			diskWriter.Write(val.text.data, val.text.offset, val.text.length);
			diskWriter.Flush();
		}
		
		static void DiskLogErase(string key)
		{
			diskWriter.Write(key);
			diskWriter.Write((long)0);
		}
		
		public static Buffer FindBuffer(string key, out long prevVersion)
		{
			prevVersion = 0;
			Buffer t = null;
			lock (data)
			{
				Record ret;
				if (!data.TryGetValue(key, out ret))
				{
					return null;
				}
				prevVersion = ret.version;
				t = ret.text;
				if (ret.expiry < DateTime.Now.Ticks)
				{
					//	expired!
					data.Remove(key);
					return null;
				}
			}
			return t;
		}
		
		public static string FindString(string key, out long prevVersion)
		{
			Buffer t = FindBuffer(key, out prevVersion);
			if (t == null)
			{
				return null;
			}
			return Encoding.UTF8.GetString(t.data, t.offset, t.length);
		}
		
		//	Find the data with a given key, and return the current version (serial number) 
		//	of that value. If you want to remove or update this data, you have to provide 
		//	the serial version back. This prevents multiple conflicting edits to happen at 
		//	the same time.
		public static dict Find(string key, out long prevVersion)
		{
			Buffer t = FindBuffer(key, out prevVersion);
			if (t == null)
			{
				return null;
			}
			return Services.jsonf.Parse(t);
		}
		
		//	Store new data (if prevVersion == 0), or update existing data (if prevVersion > 0).
		//	Return true if the value of the previous version was found and replaced, or a new 
		//	value was stored.
		public static bool Store(string key, dict val, long prevVersion)
		{
			return Store(key, val, prevVersion, -1);
		}
		public static bool Store(string key, dict val, long prevVersion, int lifetimeSeconds)
		{
			Buffer text = Services.jsonf.Format(val);
			return StoreBuffer(key, text, prevVersion, lifetimeSeconds);
		}
		
		public static bool StoreString(string key, string val, long prevVersion)
		{
			return StoreString(key, val, prevVersion, -1);
		}
		public static bool StoreString(string key, string val, long prevVersion, int lifetimeSeconds)
		{
			byte[] data = Encoding.UTF8.GetBytes(val);
			Buffer text = new Buffer(data, 0, data.Length);
			return StoreBuffer(key, text, prevVersion);
		}
		
		public static bool StoreBuffer(string key, Buffer text, long prevVersion)
		{
			return StoreBuffer(key, text, prevVersion, -1);
		}
		public static bool StoreBuffer(string key, Buffer text, long prevVersion, int expirySeconds)
		{
			long expiry;
			if (expirySeconds < 0)
			{
				expiry = Int64.MaxValue;
			}
			else
			{
 				expiry = DateTime.Now.Ticks + expirySeconds * 10000000L;
			}
			lock (data)
			{
				Record ret;
				//	update the wrong previous version
				if (data.TryGetValue(key, out ret) && ret.version != prevVersion)
				{
					return false;
				}
				if (ret == null)
				{
					if (prevVersion != 0)
					{
						//	try to update something that doesn't exist
						return false;
					}
					ret = new Record();
					data.Add(key, ret);
				}
				//	This is tricky: I re-use the Record object. That is 
				//	safe, as long as the disk log doesn't hold on to it after 
				//	the lock is released. 
				ret.text = text;
				ret.version = prevVersion + 1;
				ret.expiry = expiry;
				DiskLogUpdate(key, ret);
			}
			return true;
		}
		
		//	Remove the data for a given key, assuming it hasn't been touched since you 
		//	last saw it. Return true if the value of the given version was found and removed.
		public static bool Erase(string key, long prevVersion)
		{
			bool ret = false;
			lock (data)
			{
				Record rec;
				if (data.TryGetValue(key, out rec) && rec.version == prevVersion)
				{
					data.Remove(key);
					ret = true;
					DiskLogErase(key);
				}
			}
			return ret;
		}
	}
}

