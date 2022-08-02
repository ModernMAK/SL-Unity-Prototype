using System;
using SLUnity.Objects;

namespace SLUnity.Managers
{
    public class ObjectCreatedArgs : EventArgs
    {
        public ObjectCreatedArgs(SLPrimitive primitive)
        {
            Primitive = primitive;
        }

        public SLPrimitive Primitive { get; }
    }
}