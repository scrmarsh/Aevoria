using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace Aevoria.Persistence
{
    // Disposable products only. World archives, resources and excavation state
    // never enter this store. Every renderer ABI change must change its identity.
    public sealed class TerrainPresentationCache
    {
        public const int Format=2,MaximumEntryBytes=32*1024*1024;
        private readonly string root,identity;
        private readonly object gate=new object();
        private readonly Dictionary<string,LinkedListNode<(string key,byte[] bytes)>> memory=new Dictionary<string,LinkedListNode<(string,byte[])>>();
        private readonly LinkedList<(string key,byte[] bytes)> recent=new LinkedList<(string,byte[])>();
        private long budget;private long resident;
        public void SetMemoryBudget(int mebibytes){lock(gate){budget=(long)Math.Max(0,Math.Min(1024,mebibytes))*1024*1024;while(resident>budget){var last=recent.Last;resident-=last.Value.bytes.Length;memory.Remove(last.Value.key);recent.RemoveLast();}}}
        public int Hits {get;private set;}public int Misses {get;private set;}public int Rebuilt {get;private set;}
        public long ResidentBytes {get{lock(gate)return resident;}}
        public TerrainPresentationCache(string directory,string generationIdentity,int memoryMiB=64)
        {root=Path.GetFullPath(directory);identity=ArchiveCodec.Hash(Encoding.UTF8.GetBytes(generationIdentity));budget=(long)Math.Max(0,Math.Min(1024,memoryMiB))*1024*1024;}
        // Compact path components avoid Unity Mono's Windows path-length limit.
        // Full identities are still checked inside every file, including products.
        private static string ProductHash(string product)=>ArchiveCodec.Hash(Encoding.UTF8.GetBytes(product));
        public string PathFor(string product)=>Path.Combine(root,identity.Substring(0,16),ProductHash(product).Substring(0,24)+".terrain");
        public bool TryRead(string product,out byte[] payload)
        {
            lock(gate)
            {
                if(memory.TryGetValue(product,out var node)){recent.Remove(node);recent.AddFirst(node);payload=(byte[])node.Value.bytes.Clone();Hits++;return true;}
                payload=null;
                try
                {
                    string path=PathFor(product);if(!File.Exists(path)){Misses++;return false;}
                    var info=new FileInfo(path);if(info.Length<80||info.Length>MaximumEntryBytes+256)throw new InvalidDataException("Cache size");
                    using(var stream=File.OpenRead(path))using(var reader=new BinaryReader(stream,Encoding.UTF8))
                    {
                        if(reader.ReadInt32()!=0x41455643||reader.ReadInt32()!=Format||reader.ReadString()!=identity||reader.ReadString()!=ProductHash(product))throw new InvalidDataException("Cache identity");
                        int count=reader.ReadInt32();string checksum=reader.ReadString();if(count<0||count>MaximumEntryBytes||stream.Length-stream.Position!=count)throw new InvalidDataException("Cache length");
                        payload=reader.ReadBytes(count);if(ArchiveCodec.Hash(payload)!=checksum)throw new InvalidDataException("Cache checksum");
                    }
                    Remember(product,payload);Hits++;return true;
                }
                catch(Exception ex)when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is FormatException)
                {payload=null;Misses++;Rebuilt++;return false;}
            }
        }
        public void Write(string product,byte[] payload)
        {
            if(payload==null||payload.Length>MaximumEntryBytes)throw new ArgumentOutOfRangeException(nameof(payload));
            lock(gate)
            {
                string path=PathFor(product);Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
                try
                {
                    using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var writer=new BinaryWriter(stream,Encoding.UTF8))
                    {writer.Write(0x41455643);writer.Write(Format);writer.Write(identity);writer.Write(ProductHash(product));writer.Write(payload.Length);writer.Write(ArchiveCodec.Hash(payload));writer.Write(payload);writer.Flush();stream.Flush(true);}
                    if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                    Remember(product,payload);
                }
                finally{if(File.Exists(temp))File.Delete(temp);}
            }
        }
        private void Remember(string product,byte[] bytes)
        {
            if(memory.TryGetValue(product,out var old)){resident-=old.Value.bytes.Length;recent.Remove(old);memory.Remove(product);}
            if(bytes.Length>budget)return;
            var node=recent.AddFirst((product,(byte[])bytes.Clone()));memory.Add(product,node);resident+=bytes.Length;
            while(resident>budget){var last=recent.Last;resident-=last.Value.bytes.Length;memory.Remove(last.Value.key);recent.RemoveLast();}
        }
    }
}
