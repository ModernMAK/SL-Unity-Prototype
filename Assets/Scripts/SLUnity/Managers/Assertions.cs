using System;

namespace SLUnity.Managers
{
    public static class Assertions
    {
        public static void AssertSingleton(object self, object singletonReference, string name = null)
        {
            if (singletonReference == null || singletonReference == self) return;
            name ??= "Singleton";//no name given, just set to singleton
            //TODO change exception to more specific exception
            throw new Exception($"Multiple instances of `{name}` found!");
        }
    }
}