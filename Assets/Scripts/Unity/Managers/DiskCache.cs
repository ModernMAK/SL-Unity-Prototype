using System;
using System.IO;
using System.Threading.Tasks;

namespace Unity.Managers
{
    public class DiskCache<TKey, TValue> : IDiskCache<TKey,TValue>
    {
        private readonly string _cacheLocation;
        private readonly Func<TKey, string> _pathFunc;
        private readonly ISerializer<TValue> _serializer;

        public DiskCache(string cacheLocation, Func<TKey, string> pathFunc, ISerializer<TValue> serializer)
        {
            _cacheLocation = cacheLocation;
            _pathFunc = pathFunc;
            _serializer = serializer;
        }

        public virtual void Store(TKey key, TValue value)
        {
            var filePath = Path.Combine(_cacheLocation, _pathFunc(key));
            using (var file = new FileStream(filePath, FileMode.OpenOrCreate))
            using (var writer = new BinaryWriter(file))
                _serializer.Write(writer, value);
        }

        public Task StoreAsync(TKey key, TValue value)
        {
            void Wrapper() => Store(key, value);
            return  new Task(Wrapper);
        }


        public virtual bool Load(TKey key, out TValue value)
        {
            var filePath = Path.Combine(_cacheLocation, _pathFunc(key));
            try
            {
                using (var file = new FileStream(filePath, FileMode.Open))
                using (var reader = new BinaryReader(file))
                {
                    value = _serializer.Read(reader);
                    return true;
                }
            }
            catch (FileNotFoundException fnf)
            {
                value = default;
                return false;
            }
        }

        public Task<Tuple<bool, TValue>> LoadAsync(TKey key)
        {
            Tuple<bool, TValue> Wrapper()
            {
                var result = Load(key, out var value);
                return new Tuple<bool,TValue>(result, value);

            }

            return new Task<Tuple<bool, TValue>>(Wrapper);
        }
    }
}