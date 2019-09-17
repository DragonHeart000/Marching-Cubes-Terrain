using UnityEngine;

namespace MarchingCubes
{
    public static class Utils
    {
        public static float FloorToNearestX(this float n, float x)
        {
            return Mathf.Floor(n / x) * x;
        }

        public static Vector3 Abs(this Vector3 n)
        {
            var x = Mathf.Abs(n.x);
            var y = Mathf.Abs(n.y);
            var z = Mathf.Abs(n.z);

            return new Vector3(x, y, z);
        }
        
        public static Vector3 FloorToNearestX(this Vector3 n, float x)
        {
            var flooredX = FloorToNearestX(n.x, x);
            var flooredY = FloorToNearestX(n.y, x);
            var flooredZ = FloorToNearestX(n.z, x);

            return new Vector3(flooredX, flooredY, flooredZ);
        }

        public static Vector3Int Floor(this Vector3 n)
        {
            var flooredX = Mathf.FloorToInt(n.x);
            var flooredY = Mathf.FloorToInt(n.y);
            var flooredZ = Mathf.FloorToInt(n.z);

            return new Vector3Int(flooredX, flooredY, flooredZ);
        }

        public static float RoundToNearestX(this float n, float x)
        {
            return Mathf.Round(n / x) * x;
        }

        public static Vector3 RoundToNearestX(this Vector3 n, float x)
        {
            var roundedX = n.x.RoundToNearestX(x);
            var roundedY = n.y.RoundToNearestX(x);
            var roundedZ = n.z.RoundToNearestX(x);

            return new Vector3(roundedX, roundedY, roundedZ);
        }

        public static Vector3 Mod(this Vector3 n, float x)
        {
            var modX = Mod(n.x, x);
            var modY = Mod(n.y, x);
            var modZ = Mod(n.z, x);

            return new Vector3(modX, modY, modZ);
        }

        public static float Mod(this float n, float x)
        {
            return (n % x + x) % x;
        }

        public static float Map(this float value, float x1, float y1, float x2, float y2)
        {
            return (value - x1) / (y1 - x1) * (y2 - x2) + x2;
        }
    }
}