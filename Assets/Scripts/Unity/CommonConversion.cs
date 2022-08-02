using UnityEngine;

public static class CommonConversion
{
    public static Vector3 CoordToUnity(OpenMetaverse.Vector3 v) => CoordConverter.Converter.SLToUnity(v.CastUnity());
    public static Vector2 UVToUnity(OpenMetaverse.Vector2 v) => v.CastUnity();

    public static Quaternion RotToUnity(OpenMetaverse.Quaternion q) =>
        CoordConverter.Converter.SLToUnity(q.CastUnity());
}