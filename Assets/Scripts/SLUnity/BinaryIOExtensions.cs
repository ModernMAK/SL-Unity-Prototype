using System;
using System.IO;
using LibreMetaverse.PrimMesher;
using UnityEngine;

namespace SLUnity
{
    public static class BinaryIOExtensions
    {
        public static void WriteArray<T>(this BinaryWriter writer, T[] array, Action<T> innerWrite)
        {
            writer.Write(array.Length);
            foreach(var item in array)
                innerWrite(item);
        }

        public static void WriteArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> innerWrite)
            => WriteArray(writer, array, (value) => innerWrite(writer, value));
        public static void WriteArray2D<T>(this BinaryWriter writer, T[,] array, Action<T> innerWrite)
        {
            var d0 = array.GetLength(0);
            var d1 = array.GetLength(1);
            writer.Write(d0);
            writer.Write(d1);
            for(var x = 0; x < d0; x++)
            for (var y = 0; y < d1; y++)
                innerWrite(array[x, y]);
        }
        public static void WriteArray2D<T>(this BinaryWriter writer, T[,] array, Action<BinaryWriter,T> innerWrite)
            => WriteArray2D(writer, array, (value) => innerWrite(writer, value));
        public static void Write(this BinaryWriter writer, Guid g)
        {
            //16 Bytes
            writer.Write(g.ToByteArray());
        }
        public static Guid ReadGuid(this BinaryReader reader)
        {
            //16 Bytes
            var guid = reader.ReadBytes(16);
            return new Guid(guid);
        }
        public static void Write(this BinaryWriter writer, Quaternion q)
        {
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);
        }
        public static void Write(this BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }
        public static void Write(this BinaryWriter writer, Vector2 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
        }
        public static void WriteByteArray(this BinaryWriter writer, byte[] array)
        {
            writer.Write(array.Length);
            writer.Write(array);
        }
        public static T[] ReadArray<T>(this BinaryReader reader, Func<T> innerRead)
        {
            var size = reader.ReadInt32();
            var array = new T[size];
            for (var i = 0; i < size; i++)
                array[i] = innerRead();
            return array;
        }

        public static T[] ReadArray<T>(this BinaryReader reader, Func<BinaryReader, T> innerRead)
            => ReadArray(reader, () => innerRead(reader));
        public static T[,] ReadArray2D<T>(this BinaryReader reader, Func<T> innerRead)
        {
            var d0 = reader.ReadInt32();
            var d1 = reader.ReadInt32();
            var array = new T[d0,d1];
            for (var x = 0; x < d0; x++)
            for (var y = 0; y < d1; y++)
                array[x,y] = innerRead();
            return array;
        }

        public static T[,] ReadArray2D<T>(this BinaryReader reader, Func<BinaryReader, T> innerRead) =>
            ReadArray2D(reader, () => innerRead(reader));
        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            var size = reader.ReadInt32();
            return reader.ReadBytes(size);
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            return new Quaternion(x, y, z, w);
        }
        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            return new Vector2(x, y);
        }
        public static Color32 ReadColor32(this BinaryReader reader)
        {
            var rgba = reader.ReadBytes(4);
            return new Color32(rgba[0], rgba[1], rgba[2], rgba[3]);
        }
        public static void Write(this BinaryWriter writer, Color32 color)
        {
            var bytes = new byte[4]
            {
                color.r,
                color.g,
                color.b,
                color.a,
            };
            writer.Write(bytes);
        }
    }
}