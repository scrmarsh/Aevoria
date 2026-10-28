using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Aevoria.World;

namespace Aevoria.Persistence
{
    [Serializable] public sealed class CivilizationHistoryDocument
    {
        public int Version=1;
        public List<SocialHistoryEvent> Events=new List<SocialHistoryEvent>();
    }
    // Immutable content-addressed pages are shared by a repository, so world
    // copies and backups retain their history references without rewriting pages.
    // Only a bounded recent window and page descriptors remain in active society.
    public sealed class CivilizationHistoryArchive:ICivilizationHistoryArchive
    {
        public string Root{get;}
        public CivilizationHistoryArchive(string root){Root=Path.GetFullPath(root??throw new ArgumentNullException(nameof(root)));}
        private string Resolve(string key)
        {
            if(key==null||!Regex.IsMatch(key,@"^[A-F0-9]{64}\.xml$"))throw new InvalidDataException("Invalid historical page key.");
            string file=Path.GetFullPath(Path.Combine(Root,key));if(!file.StartsWith(Root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Historical page escaped its archive.");return file;
        }
        public SocialHistoryPage Store(IReadOnlyList<SocialHistoryEvent> events)
        {
            if(events==null||events.Count<1||events.Count>CivilizationModel.HistoryPageSize)throw new ArgumentException("Invalid history page size.");
            var document=new CivilizationHistoryDocument();long expected=events[0].Sequence;
            foreach(var e in events){if(e==null||e.Sequence!=expected++)throw new InvalidDataException("History page sequence is discontinuous.");document.Events.Add(e.Copy());}
            byte[] bytes=ArchiveCodec.Encode(document);string hash=ArchiveCodec.Hash(bytes),key=hash+".xml",file=Resolve(key);Directory.CreateDirectory(Root);
            if(File.Exists(file)){if(ArchiveCodec.Hash(File.ReadAllBytes(file))!=hash)throw new InvalidDataException("An immutable history page is damaged; the existing file is preserved.");}
            else
            {
                // Unique temporary files never replace an existing immutable page.
                string temporary=file+"."+Guid.NewGuid().ToString("N")+".pending";
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                File.Move(temporary,file);
            }
            return new SocialHistoryPage{Key=key,Sha256=hash,First=events[0].Sequence,Last=events[events.Count-1].Sequence,Count=events.Count};
        }
        public IReadOnlyList<SocialHistoryEvent> Read(SocialHistoryPage page)
        {
            if(page==null||page.Count<1||page.Count>CivilizationModel.HistoryPageSize||page.Last-page.First+1!=page.Count||page.Key!=page.Sha256+".xml")throw new InvalidDataException("Invalid historical page reference.");
            string file=Resolve(page.Key);if(new FileInfo(file).Length>4*1024*1024)throw new InvalidDataException("Historical page exceeds its bounded document size.");byte[] bytes=File.ReadAllBytes(file);if(ArchiveCodec.Hash(bytes)!=page.Sha256)throw new InvalidDataException("Historical page checksum mismatch; the original is preserved.");
            var doc=ArchiveCodec.Decode<CivilizationHistoryDocument>(bytes);if(doc.Version!=1||doc.Events==null||doc.Events.Count!=page.Count)throw new InvalidDataException("Unsupported historical page.");
            long next=page.First;foreach(var e in doc.Events)if(e==null||e.Sequence!=next++)throw new InvalidDataException("Historical page contains a broken sequence.");return doc.Events.AsReadOnly();
        }
        public void Verify(IEnumerable<SocialHistoryPage> pages){foreach(var page in pages)Read(page);}
    }
}
