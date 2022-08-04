using System;
using UnityEngine;

namespace SLUnity
{
    public static class CoordConverter
    {
        public abstract class ConverterLogic
        {
            // NOTE; THIS SHOULD NEVER CHANGE THE X/Y/Z VALUES, ONLY THEIR ORDER AND SIGN
            public abstract Vector3 SLToUnity(Vector3 sl);
        
            // NOTE; THIS SHOULD NEVER CHANGE THE X/Y/Z VALUES, ONLY THEIR ORDER AND SIGN
            public abstract Vector3 UnityToSL(Vector3 unity);
        
            /// <summary>
            /// Numbers of axis swaps that are performed.
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// Needed to perform handedness fixes on Quaternions' Angle (w) component.
            /// </remarks>
            public abstract int Swaps { get; }

            private static Quaternion FixQuaternion(Quaternion input, Func<Vector3, Vector3> fixAxis, int swaps)
            {
            
                var axis = new Vector3(input.x, input.y, input.z); // The Axis part of the quaternion
                var angle = input.w; // The Angle part of the quaternion

                var fixedAxis = fixAxis(axis);
                float fixedAngle;
                if (swaps % 2 == 1) 
                    fixedAngle = -angle; // Flip handedness
                else
                    fixedAngle = angle;
            
                return new Quaternion(fixedAxis.x, fixedAxis.y, fixedAxis.z, fixedAngle);
            }

            public Quaternion SLToUnity(Quaternion sl) => FixQuaternion(sl, SLToUnity, Swaps);
            public Quaternion UnityToSL(Quaternion unity) => FixQuaternion(unity, UnityToSL, Swaps);
        }
        private class NoneConverter : ConverterLogic
        {
            //WORKING UNDER THESE ASSUMPTIONS
            //SL uses : X (-L/R+), Y (-D,U+), Z (-B,F+)
            //Unity uses : X (-L/R+), Y (-D,U+), Z (-B,F+)

            // Unity.X = SL.X
            // Unity.Y = SL.Z
            // Unity.Z = SL.Y
            public override Vector3 SLToUnity(Vector3 sl) => sl;

            // SL.X = Unity.X
            // SL.Y = Unity.Z
            // SL.Z = Unity.Y
            public override Vector3 UnityToSL(Vector3 unity) => unity;

            public override int Swaps => 0;
        }
        private class MAKConverter : ConverterLogic
        {
            //WORKING UNDER THESE ASSUMPTIONS
            //SL uses : X (-L/R+), Y (-B/F+), Z (-D/U+)
            //Unity uses : X (-L/R+), Y (-D,U+), Z (-B,F+)

            // Unity.X = SL.X
            // Unity.Y = SL.Z
            // Unity.Z = SL.Y
            public override Vector3 SLToUnity(Vector3 sl) => new Vector3(sl.x, sl.z, sl.y);

            // SL.X = Unity.X
            // SL.Y = Unity.Z
            // SL.Z = Unity.Y
            public override Vector3 UnityToSL(Vector3 unity) => new Vector3(unity.x, unity.z, unity.y);

            // 1 Swaps total; Handedness changed (Odd # of Swaps)
            public override int Swaps => 1;
        }
        private class CatnipConverter : ConverterLogic
        {
            //WORKING UNDER THESE ASSUMPTIONS (suggested by alexis catnip)
            //SL uses : X (-B/F+), Y (-R/L+), Z (-D/U+)
            //Unity uses : X (-L/R+), Y (-D,U+), Z (-B,F+)
        
            // Unity.X = -SL.Y
            // Unity.Y = SL.Z
            // Unity.Z = SL.X
            public override Vector3 SLToUnity(Vector3 sl) => new Vector3(-sl.y, sl.z, sl.x);

            // SL.X = Unity.Z 
            // SL.Y = -Unity.X
            // SL.Z = Unity.Y
            public override Vector3 UnityToSL(Vector3 unity) => new Vector3(unity.z, -unity.x, unity.y);

            // X => Z, Y => X, and the negation of X; 3 swaps
            // X, Y, Z => Z, Y, X (1 Swap; X swaps with Z)
            // Z, Y, X => Z, X, Y (1 Swap; X swaps with Y)
            // Z, X, Y => Z, -X, Y (1 Swap; X swaps with -X)
            // 3 Swaps total; Handedness changed (Odd # of Swaps)
            public override int Swaps => 3; 
        }

        private static readonly ConverterLogic None = new NoneConverter();
        private static readonly ConverterLogic MAK = new MAKConverter();
        private static readonly ConverterLogic CATNIP = new CatnipConverter();
    
        public enum ConverterMode
        {
            None,
            MAK, CATNIP
        }

        public static ConverterMode Mode = ConverterMode.MAK; 
        public static ConverterLogic Converter
        {
            get
            {
                switch (Mode)
                {
                    case ConverterMode.None:
                        return None;
                    case ConverterMode.MAK:
                        return MAK;
                    case ConverterMode.CATNIP:
                        return CATNIP;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

    }
}