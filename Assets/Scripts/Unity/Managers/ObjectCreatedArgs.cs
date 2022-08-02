using System;

namespace Unity.Managers
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