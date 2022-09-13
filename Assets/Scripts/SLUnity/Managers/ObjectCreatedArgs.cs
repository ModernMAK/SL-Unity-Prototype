using System;
using SLUnity.Objects;

namespace SLUnity.Managers
{
    public class ObjectCreatedArgs : EventArgs
    {
        public ObjectCreatedArgs(UPrimitive primitive)
        {
            Primitive = primitive;
        }

        public UPrimitive Primitive { get; }
    }
}