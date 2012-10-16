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

                FileStream file_stream = new FileStream(args[i], FileMode.Open);

                BinaryReader binary_reader = new BinaryReader(file_stream, System.Text.Encoding.BigEndianUnicode);
                Header header = ReadHeader(binary_reader);

                if (header.Ident == Header.HEADER_IDENT)
                    Console.WriteLine("VALID Ueberschall file!");
                else
                    Console.WriteLine("INVALID Ueberschall file!");

                List<Chunk> chunks = ReadChunks(binary_reader, header);

                ListContentsToFile(chunks);
                CreateDirectories(chunks, base_folder);
                ReadAndSaveBinaryData(chunks, binary_reader, base_folder);
            }
        }

        public static Header ReadHeader(BinaryReader binary_reader)
        {
            Header s = new Header();

            byte[] bytes;
            bytes = binary_reader.ReadBytes(sizeof(int));
            s.Ident = System.Text.Encoding.UTF8.GetString(bytes);   //  File identifier.

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            s.Ver = BitConverter.ToInt32(bytes, 0);                 //  Version number?

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            s.Unk1 = BitConverter.ToInt32(bytes, 0);                //  Padding?

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            s.Unk2 = BitConverter.ToInt32(bytes, 0);                //  More padding?

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            s.NumEntries = BitConverter.ToInt32(bytes, 0);          //  Number of file entries.

            return s;
        }

        private static void ReadAndSaveBinaryData(List<Chunk> chunks, BinaryReader binary_reader, string base_directory)
        {
            //  Beginning of the binary data.
            long bin_offset = binary_reader.BaseStream.Position;

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
                            writer.Write(binary_reader.ReadBytes(chunks[i].Size));
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

        private static List<Chunk> ReadChunks(BinaryReader binary_reader, Header header)
        {
            List<Chunk> chunks = new List<Chunk>();

            for (int i = 0; i < header.NumEntries; ++i)
            {
                chunks.Add(ReadChunk(binary_reader));

                if (chunks[i].Type == 128)
                    chunks[i].Id = i + 1;
            }

            return chunks;
        }

        public static Chunk ReadChunk(BinaryReader binary_reader)
        {
            Chunk c = new Chunk();

            byte[] bytes;

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            c.Offset = BitConverter.ToInt32(bytes, 0);              //  Chunk size.

            bytes = binary_reader.ReadBytes(4);                                //  Padding?

            bytes = binary_reader.ReadBytes(sizeof(int));
            c.Type = BitConverter.ToInt32(bytes, 0);

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            c.Size = BitConverter.ToInt32(bytes, 0);

            bytes = binary_reader.ReadBytes(16);                                //   16 bytes of garbage? No clue honestly.

            bytes = binary_reader.ReadBytes(sizeof(int));
            Array.Reverse(bytes);
            c.Parent = BitConverter.ToInt32(bytes, 0);

            c.Name = ReadString(binary_reader);

            int stringSize = c.Name.Length * 2;                                 //  x2 because of unicode...

            int off = c.Offset - 32 - stringSize - sizeof(int) - 2;             //  -2 for string null terminator.

            bytes = binary_reader.ReadBytes(off);                               //  Remainder of chunk data we have no clue.

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

        public static string ReadString(BinaryReader binary_reader)
        {
            string s = String.Empty;
            char chr = binary_reader.ReadChar();

            while (chr != '\0')
            {
                s += chr;
                chr = binary_reader.ReadChar();
            }

            return s;
        }
    }
}
