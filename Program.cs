using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace ElastikExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                FileInfo fi = new FileInfo(args[i]);
                string base_folder = fi.Name.TrimEnd(fi.Extension.ToCharArray());

                FileStream file_stream = new FileStream(args[i], FileMode.Open, FileAccess.Read);

                Header header = ReadHeader(file_stream);

                if (header.Ident == Header.HEADER_IDENT)
                    Console.WriteLine("VALID Ueberschall file!");
                else
                {
                    Console.WriteLine("INVALID Ueberschall file!");
                    continue;
                }

                List<Chunk> chunks = ReadChunks(file_stream, header);

                ListContentsToFile(chunks);
                CreateDirectories(chunks, base_folder);
                ReadAndSaveBinaryData(chunks, file_stream, base_folder);
            }
        }

        public static Header ReadHeader(FileStream file_stream)
        {
            Header s = new Header();

            byte[] bytes = new byte[sizeof(int)];

            file_stream.Read(bytes, 0, sizeof(int));
            s.Ident = System.Text.Encoding.UTF8.GetString(bytes);   //  File identifier.

            ReadBigEndian(file_stream, bytes, sizeof(int));
            s.Ver = BitConverter.ToInt32(bytes, 0);

            ReadBigEndian(file_stream, bytes, sizeof(int));
            s.Unk1 = BitConverter.ToInt32(bytes, 0);                //  Padding?

            ReadBigEndian(file_stream, bytes, sizeof(int));
            s.Unk2 = BitConverter.ToInt32(bytes, 0);                //  More padding?

            ReadBigEndian(file_stream, bytes, sizeof(int));
            s.NumEntries = BitConverter.ToInt32(bytes, 0);          //  Number of file entries.

            return s;
        }

        private static void ReadAndSaveBinaryData(List<Chunk> chunks, FileStream file_stream, string base_directory)
        {
            //  Beginning of the binary data.
            long bin_offset = file_stream.Position;

            Console.Clear();

            //  Binary files
            for (int i = 0; i < chunks.Count; ++i)
            {
                if (chunks[i].Type == 0)
                {
                    using (FileStream stream = new FileStream(System.IO.Path.Combine(base_directory, chunks[i].FullName), FileMode.Create))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            Console.SetCursorPosition(0, 0);
                            Console.WriteLine(i + "/" + chunks.Count);
                            byte[] bytes = new byte[chunks[i].Size];
                            file_stream.Read(bytes, 0, chunks[i].Size);
                            writer.Write(bytes);
                        }
                    }
                }
            }

            Console.SetCursorPosition(0, 0);
            Console.WriteLine(chunks.Count + "/" + chunks.Count);
        }

        private static void ListContentsToFile(List<Chunk> chunks)
        {
            using (StreamWriter outfile = new StreamWriter("LISTING.TXT"))
            {
                for (int i = 0; i < chunks.Count; ++i)
                {
                    outfile.Write(Chunk.ChunksFullPath(chunks[i], chunks));
                    outfile.WriteLine(chunks[i]);
                }
            }
        }

        private static List<Chunk> ReadChunks(FileStream file_stream, Header header)
        {
            List<Chunk> chunks = new List<Chunk>();

            for (int i = 0; i < header.NumEntries; ++i)
            {
                chunks.Add(ReadChunk(file_stream));

                if (chunks[i].Type == 128)
                    chunks[i].Id = i + 1;
            }

            return chunks;
        }

        public static Chunk ReadChunk(FileStream file_stream)
        {
            Chunk c = new Chunk();

            byte[] bytes = new byte[sizeof(int)];

            ReadBigEndian(file_stream, bytes, sizeof(int));
            c.Offset = BitConverter.ToInt32(bytes, 0);                  //  Chunk size.

            bytes = new byte[4];
            file_stream.Read(bytes, 0, 4);                              //  Padding?

            bytes = new byte[sizeof(int)];
            file_stream.Read(bytes, 0, sizeof(int));
            c.Type = BitConverter.ToInt32(bytes, 0);

            bytes = new byte[sizeof(int)];
            ReadBigEndian(file_stream, bytes, sizeof(int));
            c.Size = BitConverter.ToInt32(bytes, 0);

            bytes = new byte[16];
            file_stream.Read(bytes, 0, 16);                             //   16 bytes of garbage? No clue honestly.

            bytes = new byte[sizeof(int)];
            ReadBigEndian(file_stream, bytes, sizeof(int));
            c.Parent = BitConverter.ToInt32(bytes, 0);

            c.Name = ReadString(file_stream);

            int stringSize = System.Text.Encoding.BigEndianUnicode.GetByteCount(c.Name); //  x2 because of unicode...

            int off = c.Offset - 32 - stringSize - sizeof(int) - 2;     //  -2 for string null terminator.

            bytes = new byte[off];
            file_stream.Read(bytes, 0, off);                            //  Remainder of chunk data we have no clue.

            return c;
        }

        public static void CreateDirectories(List<Chunk> chunks, string base_directory)
        {
            for (int i = 0; i < chunks.Count; ++i)
            {
                string chnk_full_path = Chunk.ChunksFullPath(chunks[i], chunks);
                chunks[i].FullName = chnk_full_path;

                if (chunks[i].Type == 128)
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(base_directory,chunks[i].FullName));
                }
            }
        }

        private static void ReadBigEndian(FileStream file_stream, byte[] bytes, int size)
        {
            file_stream.Read(bytes, 0, sizeof(int));
            Array.Reverse(bytes);
        }

        public static string ReadString(FileStream file_stream)
        {
            string s = String.Empty;

            byte[] bytes = new byte[2];
            
            file_stream.Read(bytes, 0, 2);

            while (!(bytes[0] == 0 && bytes[1] == 0))
            {
                s += System.Text.Encoding.BigEndianUnicode.GetString(bytes, 0, 2);
                file_stream.Read(bytes, 0, 2);
            }

            return s;
        }
    }
}
