using System;
using System.IO;
using UnityEngine;

namespace UnityTemplateProjects.Unity
{
    public static class BinaryIOExtensions
    {
        public static void WriteArray<T>(this BinaryWriter writer, T[] array, Action<T> innerWrite)
        {
            writer.Write(array.Length);
            foreach(var item in array)
                innerWrite(item);
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
        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            return new Vector2(x, y);
        }
    }
}