using System;
using System.Collections.Generic;
using System.Text;

namespace ElastikExtractor
{
    class Header
    {
        public static string HEADER_IDENT = "GNFA";
        public string Ident;            //  File identifier.
        public int Ver;                 //  Version number?
        public int Unk1;                //  Padding?
        public int Unk2;                //  More padding?
        public int NumEntries;          //  Number of file entries.
    }

    class Chunk
    {
        public int Id;
        public int Offset;              //  Next chunk offset
        public int Type;                //  128 = Folder,   0 = Binary
        public int Size;
        public int Parent;
        public string Name;             //  Some sort of identifier
        public string FullName;

        public static string ChunksFullPath(Chunk chnk, List<Chunk> chnks)
        {
            string path = chnk.Name;

            if (chnks[chnks.IndexOf(chnk)].Parent != 0)
                path = System.IO.Path.Combine(ChunksFullPath(chnks[chnks[chnks.IndexOf(chnk)].Parent - 1], chnks), chnk.Name);

            return path;
        }

        public override string ToString()
        {
            if (Type == 128)
                return String.Empty;
            else
                return " - " + Size + " - " + Parent;
        }
    }
}
