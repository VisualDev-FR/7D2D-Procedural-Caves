// Mock the game assembly for testing purpose

# pragma warning disable CA1050, CA2211, IDE0290, IDE0060

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


public class Vector3i
{
    public int x;

    public int y;

    public int z;

    public static Vector3i one = new Vector3i(1, 1, 1);
    public static Vector3i zero = new Vector3i(0, 0, 0);

    public Vector3i(int _x, int _y, int _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }

    public Vector3i() { }

    public Vector3i(Vector3i other)
    {
        x = other.x;
        y = other.y;
        z = other.z;
    }

    public static Vector3i operator +(Vector3i v1, Vector3i v2)
    {
        return new Vector3i(
            v1.x + v2.x,
            v1.y + v2.y,
            v1.z + v2.z
        );
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector3i other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        return false;
    }

    public static bool operator !=(Vector3i p1, Vector3i p2)
    {
        return !(p1 == p2);
    }

    public static bool operator ==(Vector3i p1, Vector3i p2)
    {
        if (p1 is Vector3i)
            return p1.Equals(p2);

        if (p2 is Vector3i)
            return false;

        return true;
    }

    public static Vector3i operator -(Vector3i p1, Vector3i p2)
    {
        return new Vector3i(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z);
    }

    public static Vector3i operator *(Vector3i vec, int mult)
    {
        return new Vector3i(vec.x * mult, vec.y * mult, vec.z * mult);
    }

    public static Vector3i operator /(Vector3i vec, int mult)
    {
        return new Vector3i(vec.x / mult, vec.y / mult, vec.z / mult);
    }

    public override int GetHashCode()
    {
        return x * 61646 + y * 75797 + z;
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }
}

public class PrefabData
{
    public Vector3i size;
    public string Name;

    public List<Prefab.Marker> POIMarkers = new List<Prefab.Marker>();

    public List<Prefab.Marker> RotatePOIMarkers(bool bLeft, int rotation)
    {
        return new List<Prefab.Marker>(POIMarkers);
    }
}

public class Prefab
{
    public class Marker
    {
        public Vector3i start;
        public Vector3i size;
        public enum MarkerTypes
        {
            None,
        }

        public Marker(Vector3i markerPos, Vector3i markerSize, MarkerTypes markerType, string groupName, FastTags<TagGroup.Poi> tags)
        {
            start = markerPos;
            size = markerSize;
        }

        public FastTags<TagGroup.Poi> tags;
    }

}

public class PrefabDataInstance
{
    public Vector3i boundingBoxPosition;

    public Vector3i boundingBoxSize;

    public byte rotation;

    public PrefabData prefab;

    public int id;

    public PrefabDataInstance(int id, Vector3i position, byte rotation, PrefabData prefabData)
    {
        this.id = id;
        this.rotation = rotation;
        boundingBoxPosition = position;
        boundingBoxSize = prefabData.size;
    }
}

public static class CavePlanner
{
    public static int entrancesAdded = 0;

    public static int maxPlacementAttempts = 20;

    public static int cavePrefabTerrainMargin = 10;

    public static int cavePrefabBedRockMargin = 2;

    public static int radiationZoneMargin = 0;

    public static int overLapMargin = 30;
}

public static class Log
{
    public static void Out(string message)
    {
        Console.WriteLine($"{"INFO",-10} {message}");
    }


    public static void Error(string message)
    {
        Console.WriteLine($"{"ERROR",-10} {message}");
    }

    public static void Warning(string message)
    {
        Console.WriteLine($"{"WARNING",-10} {message}");
    }
}

namespace WorldGenerationEngineFinal
{
    public class WorldBuilder
    {
        public static class Instance
        {
            public static int GetHeight(int x, int z)
            {
                return 128;
            }
        };
    }
}

public class FastTags<T>
{
    public static FastTags<TagGroup.Poi> none = null;

    public static FastTags<T> Parse(string tag)
    {
        return null;
    }
    public bool Test_AnySet(FastTags<TagGroup.Poi> tags)
    {
        return true;
    }
}

public class TagGroup
{
    public class Poi { }


}

public class StreetTile
{
    public static int TileSize = 150;
}


public partial struct Vector3 : IEquatable<Vector3>, IFormattable
{
    // *Undocumented*
    public const float kEpsilon = 0.00001F;
    // *Undocumented*
    public const float kEpsilonNormalSqrt = 1e-15F;

    // X component of the vector.
    public float x;
    // Y component of the vector.
    public float y;
    // Z component of the vector.
    public float z;

    // Linearly interpolates between two vectors.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Vector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    // Linearly interpolates between two vectors without clamping the interpolant
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    // Moves a point /current/ in a straight line towards a /target/ point.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        // avoid vector ops because current scripting backends are terrible at inlining
        float toVector_x = target.x - current.x;
        float toVector_y = target.y - current.y;
        float toVector_z = target.z - current.z;

        float sqdist = toVector_x * toVector_x + toVector_y * toVector_y + toVector_z * toVector_z;

        if (sqdist == 0 || (maxDistanceDelta >= 0 && sqdist <= maxDistanceDelta * maxDistanceDelta))
            return target;
        var dist = (float)Math.Sqrt(sqdist);

        return new Vector3(current.x + toVector_x / dist * maxDistanceDelta,
            current.y + toVector_y / dist * maxDistanceDelta,
            current.z + toVector_z / dist * maxDistanceDelta);
    }

    // Gradually changes a vector towards a desired goal over time.
    public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
    {
        float output_x = 0f;
        float output_y = 0f;
        float output_z = 0f;

        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = Mathf.Max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);

        float change_x = current.x - target.x;
        float change_y = current.y - target.y;
        float change_z = current.z - target.z;
        Vector3 originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;

        float maxChangeSq = maxChange * maxChange;
        float sqrmag = change_x * change_x + change_y * change_y + change_z * change_z;
        if (sqrmag > maxChangeSq)
        {
            var mag = (float)Math.Sqrt(sqrmag);
            change_x = change_x / mag * maxChange;
            change_y = change_y / mag * maxChange;
            change_z = change_z / mag * maxChange;
        }

        target.x = current.x - change_x;
        target.y = current.y - change_y;
        target.z = current.z - change_z;

        float temp_x = (currentVelocity.x + omega * change_x) * deltaTime;
        float temp_y = (currentVelocity.y + omega * change_y) * deltaTime;
        float temp_z = (currentVelocity.z + omega * change_z) * deltaTime;

        currentVelocity.x = (currentVelocity.x - omega * temp_x) * exp;
        currentVelocity.y = (currentVelocity.y - omega * temp_y) * exp;
        currentVelocity.z = (currentVelocity.z - omega * temp_z) * exp;

        output_x = target.x + (change_x + temp_x) * exp;
        output_y = target.y + (change_y + temp_y) * exp;
        output_z = target.z + (change_z + temp_z) * exp;

        // Prevent overshooting
        float origMinusCurrent_x = originalTo.x - current.x;
        float origMinusCurrent_y = originalTo.y - current.y;
        float origMinusCurrent_z = originalTo.z - current.z;
        float outMinusOrig_x = output_x - originalTo.x;
        float outMinusOrig_y = output_y - originalTo.y;
        float outMinusOrig_z = output_z - originalTo.z;

        if (origMinusCurrent_x * outMinusOrig_x + origMinusCurrent_y * outMinusOrig_y + origMinusCurrent_z * outMinusOrig_z > 0)
        {
            output_x = originalTo.x;
            output_y = originalTo.y;
            output_z = originalTo.z;

            currentVelocity.x = (output_x - originalTo.x) / deltaTime;
            currentVelocity.y = (output_y - originalTo.y) / deltaTime;
            currentVelocity.z = (output_z - originalTo.z) / deltaTime;
        }

        return new Vector3(output_x, output_y, output_z);
    }

    // Access the x, y, z components using [0], [1], [2] respectively.
    public float this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            switch (index)
            {
                case 0: return x;
                case 1: return y;
                case 2: return z;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index!");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            switch (index)
            {
                case 0: x = value; break;
                case 1: y = value; break;
                case 2: z = value; break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index!");
            }
        }
    }

    // Creates a new vector with given x, y, z components.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    // Creates a new vector with given x, y components and sets /z/ to zero.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3(float x, float y) { this.x = x; this.y = y; z = 0F; }

    // Set x, y and z components of an existing Vector3.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }

    // Multiplies two vectors component-wise.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }

    // Multiplies every component of this vector by the same component of /scale/.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scale(Vector3 scale) { x *= scale.x; y *= scale.y; z *= scale.z; }

    // Cross Product of two vectors.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
    {
        return new Vector3(
            lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.x * rhs.y - lhs.y * rhs.x);
    }

    // used to allow Vector3s to be used as keys in hash tables
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
    }

    // also required for being able to use Vector3s as keys in hash tables
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object other)
    {
        if (other is Vector3 v)
            return Equals(v);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector3 other)
    {
        return x == other.x && y == other.y && z == other.z;
    }

    // Reflects a vector off the plane defined by a normal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
    {
        float factor = -2F * Dot(inNormal, inDirection);
        return new Vector3(factor * inNormal.x + inDirection.x,
            factor * inNormal.y + inDirection.y,
            factor * inNormal.z + inDirection.z);
    }

    // *undoc* --- we have normalized property now
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Normalize(Vector3 value)
    {
        float mag = Magnitude(value);
        if (mag > kEpsilon)
            return value / mag;
        else
            return zero;
    }

    // Makes this vector have a ::ref::magnitude of 1.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        float mag = Magnitude(this);
        if (mag > kEpsilon)
            this = this / mag;
        else
            this = zero;
    }

    // Returns this vector with a ::ref::magnitude of 1 (RO).
    public Vector3 normalized
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return Vector3.Normalize(this); }
    }

    // Dot Product of two vectors.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 lhs, Vector3 rhs) { return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z; }

    // Returns the angle in degrees between /from/ and /to/. This is always the smallest
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(Vector3 from, Vector3 to)
    {
        // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
        float denominator = (float)Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
        if (denominator < kEpsilonNormalSqrt)
            return 0F;

        float dot = Mathf.Clamp(Dot(from, to) / denominator, -1F, 1F);
        return ((float)Math.Acos(dot)) * Mathf.Rad2Deg;
    }

    // The smaller of the two possible angles between the two vectors is returned, therefore the result will never be greater than 180 degrees or smaller than -180 degrees.
    // If you imagine the from and to vectors as lines on a piece of paper, both originating from the same point, then the /axis/ vector would point up out of the paper.
    // The measured angle between the two vectors would be positive in a clockwise direction and negative in an anti-clockwise direction.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        float unsignedAngle = Angle(from, to);

        float cross_x = from.y * to.z - from.z * to.y;
        float cross_y = from.z * to.x - from.x * to.z;
        float cross_z = from.x * to.y - from.y * to.x;
        float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
        return unsignedAngle * sign;
    }

    // Returns the distance between /a/ and /b/.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3 a, Vector3 b)
    {
        float diff_x = a.x - b.x;
        float diff_y = a.y - b.y;
        float diff_z = a.z - b.z;
        return (float)Math.Sqrt(diff_x * diff_x + diff_y * diff_y + diff_z * diff_z);
    }

    // Returns a copy of /vector/ with its magnitude clamped to /maxLength/.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
    {
        float sqrmag = vector.sqrMagnitude;
        if (sqrmag > maxLength * maxLength)
        {
            float mag = (float)Math.Sqrt(sqrmag);
            //these intermediate variables force the intermediate result to be
            //of float precision. without this, the intermediate result can be of higher
            //precision, which changes behavior.
            float normalized_x = vector.x / mag;
            float normalized_y = vector.y / mag;
            float normalized_z = vector.z / mag;
            return new Vector3(normalized_x * maxLength,
                normalized_y * maxLength,
                normalized_z * maxLength);
        }
        return vector;
    }

    // *undoc* --- there's a property now
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Magnitude(Vector3 vector) { return (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z); }

    // Returns the length of this vector (RO).
    public float magnitude
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (float)Math.Sqrt(x * x + y * y + z * z); }
    }

    // *undoc* --- there's a property now
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrMagnitude(Vector3 vector) { return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z; }

    // Returns the squared length of this vector (RO).
    public float sqrMagnitude { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return x * x + y * y + z * z; } }

    // Returns a vector that is made from the smallest components of two vectors.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Min(Vector3 lhs, Vector3 rhs)
    {
        return new Vector3(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));
    }

    // Returns a vector that is made from the largest components of two vectors.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Max(Vector3 lhs, Vector3 rhs)
    {
        return new Vector3(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));
    }

    static readonly Vector3 zeroVector = new Vector3(0F, 0F, 0F);
    static readonly Vector3 oneVector = new Vector3(1F, 1F, 1F);
    static readonly Vector3 upVector = new Vector3(0F, 1F, 0F);
    static readonly Vector3 downVector = new Vector3(0F, -1F, 0F);
    static readonly Vector3 leftVector = new Vector3(-1F, 0F, 0F);
    static readonly Vector3 rightVector = new Vector3(1F, 0F, 0F);
    static readonly Vector3 forwardVector = new Vector3(0F, 0F, 1F);
    static readonly Vector3 backVector = new Vector3(0F, 0F, -1F);
    static readonly Vector3 positiveInfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    static readonly Vector3 negativeInfinityVector = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

    // Shorthand for writing @@Vector3(0, 0, 0)@@
    public static Vector3 zero { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return zeroVector; } }
    // Shorthand for writing @@Vector3(1, 1, 1)@@
    public static Vector3 one { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return oneVector; } }
    // Shorthand for writing @@Vector3(0, 0, 1)@@
    public static Vector3 forward { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return forwardVector; } }
    public static Vector3 back { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return backVector; } }
    // Shorthand for writing @@Vector3(0, 1, 0)@@
    public static Vector3 up { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return upVector; } }
    public static Vector3 down { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return downVector; } }
    public static Vector3 left { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return leftVector; } }
    // Shorthand for writing @@Vector3(1, 0, 0)@@
    public static Vector3 right { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return rightVector; } }
    // Shorthand for writing @@Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)@@
    public static Vector3 positiveInfinity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return positiveInfinityVector; } }
    // Shorthand for writing @@Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)@@
    public static Vector3 negativeInfinity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return negativeInfinityVector; } }

    // Adds two vectors.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
    // Subtracts one vector from another.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
    // Negates a vector.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
    // Multiplies a vector by a number.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
    // Multiplies a vector by a number.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
    // Divides a vector by a number.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }

    // Returns true if the vectors are equal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3 lhs, Vector3 rhs)
    {
        // Returns false in the presence of NaN values.
        float diff_x = lhs.x - rhs.x;
        float diff_y = lhs.y - rhs.y;
        float diff_z = lhs.z - rhs.z;
        float sqrmag = diff_x * diff_x + diff_y * diff_y + diff_z * diff_z;
        return sqrmag < kEpsilon * kEpsilon;
    }

    // Returns true if vectors are different.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3 lhs, Vector3 rhs)
    {
        // Returns true in the presence of NaN values.
        return !(lhs == rhs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return ToString(null, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string format)
    {
        return ToString(format, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string format, IFormatProvider formatProvider)
    {
        return $"{x}, {y}, {z}";
    }

    [System.Obsolete("Use Vector3.forward instead.")]
    public static Vector3 fwd
    {
        get
        {
            return new Vector3(0F, 0F, 1F);
        }
    }

    [System.Obsolete("Use Vector3.Angle instead. AngleBetween uses radians instead of degrees and was deprecated for this reason")]
    public static float AngleBetween(Vector3 from, Vector3 to)
    {
        return (float)Math.Acos(Mathf.Clamp(Vector3.Dot(from.normalized, to.normalized), -1F, 1F));
    }
}


public partial struct Mathf
{
    // Returns the sine of angle /f/ in radians.
    public static float Sin(float f) { return (float)Math.Sin(f); }

    // Returns the cosine of angle /f/ in radians.
    public static float Cos(float f) { return (float)Math.Cos(f); }

    // Returns the tangent of angle /f/ in radians.
    public static float Tan(float f) { return (float)Math.Tan(f); }

    // Returns the arc-sine of /f/ - the angle in radians whose sine is /f/.
    public static float Asin(float f) { return (float)Math.Asin(f); }

    // Returns the arc-cosine of /f/ - the angle in radians whose cosine is /f/.
    public static float Acos(float f) { return (float)Math.Acos(f); }

    // Returns the arc-tangent of /f/ - the angle in radians whose tangent is /f/.
    public static float Atan(float f) { return (float)Math.Atan(f); }

    // Returns the angle in radians whose ::ref::Tan is @@y/x@@.
    public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }

    // Returns square root of /f/.
    public static float Sqrt(float f) { return (float)Math.Sqrt(f); }

    // Returns the absolute value of /f/.
    public static float Abs(float f) { return Math.Abs(f); }

    // Returns the absolute value of /value/.
    public static int Abs(int value) { return Math.Abs(value); }

    /// *listonly*
    public static float Min(float a, float b) { return a < b ? a : b; }
    // Returns the smallest of two or more values.
    public static float Min(params float[] values)
    {
        int len = values.Length;
        if (len == 0)
            return 0;
        float m = values[0];
        for (int i = 1; i < len; i++)
        {
            if (values[i] < m)
                m = values[i];
        }
        return m;
    }

    /// *listonly*
    public static int Min(int a, int b) { return a < b ? a : b; }
    // Returns the smallest of two or more values.
    public static int Min(params int[] values)
    {
        int len = values.Length;
        if (len == 0)
            return 0;
        int m = values[0];
        for (int i = 1; i < len; i++)
        {
            if (values[i] < m)
                m = values[i];
        }
        return m;
    }

    /// *listonly*
    public static float Max(float a, float b) { return a > b ? a : b; }
    // Returns largest of two or more values.
    public static float Max(params float[] values)
    {
        int len = values.Length;
        if (len == 0)
            return 0;
        float m = values[0];
        for (int i = 1; i < len; i++)
        {
            if (values[i] > m)
                m = values[i];
        }
        return m;
    }

    /// *listonly*
    public static int Max(int a, int b) { return a > b ? a : b; }
    // Returns the largest of two or more values.
    public static int Max(params int[] values)
    {
        int len = values.Length;
        if (len == 0)
            return 0;
        int m = values[0];
        for (int i = 1; i < len; i++)
        {
            if (values[i] > m)
                m = values[i];
        }
        return m;
    }

    // Returns /f/ raised to power /p/.
    public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }

    // Returns e raised to the specified power.
    public static float Exp(float power) { return (float)Math.Exp(power); }

    // Returns the logarithm of a specified number in a specified base.
    public static float Log(float f, float p) { return (float)Math.Log(f, p); }

    // Returns the natural (base e) logarithm of a specified number.
    public static float Log(float f) { return (float)Math.Log(f); }

    // Returns the base 10 logarithm of a specified number.
    public static float Log10(float f) { return (float)Math.Log10(f); }

    // Returns the smallest integer greater to or equal to /f/.
    public static float Ceil(float f) { return (float)Math.Ceiling(f); }

    // Returns the largest integer smaller to or equal to /f/.
    public static float Floor(float f) { return (float)Math.Floor(f); }

    // Returns /f/ rounded to the nearest integer.
    public static float Round(float f) { return (float)Math.Round(f); }

    // Returns the smallest integer greater to or equal to /f/.
    public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }

    // Returns the largest integer smaller to or equal to /f/.
    public static int FloorToInt(float f) { return (int)Math.Floor(f); }

    // Returns /f/ rounded to the nearest integer.
    public static int RoundToInt(float f) { return (int)Math.Round(f); }

    // Returns the sign of /f/.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sign(float f) { return f >= 0F ? 1F : -1F; }

    // The infamous ''3.14159265358979...'' value (RO).
    public const float PI = (float)Math.PI;

    // A representation of positive infinity (RO).
    public const float Infinity = Single.PositiveInfinity;

    // A representation of negative infinity (RO).
    public const float NegativeInfinity = Single.NegativeInfinity;

    // Degrees-to-radians conversion constant (RO).
    public const float Deg2Rad = PI * 2F / 360F;

    // Radians-to-degrees conversion constant (RO).
    public const float Rad2Deg = 1F / Deg2Rad;

    // We cannot round to more decimals than 15 according to docs for System.Math.Round.
    internal const int kMaxDecimals = 15;

    // Clamps a value between a minimum float and maximum float value.
    public static float Clamp(float value, float min, float max)
    {
        if (value < min)
            value = min;
        else if (value > max)
            value = max;
        return value;
    }

    // Clamps value between min and max and returns value.
    // Set the position of the transform to be that of the time
    // but never less than 1 or more than 3
    //
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
            value = min;
        else if (value > max)
            value = max;
        return value;
    }

    // Clamps value between 0 and 1 and returns value
    public static float Clamp01(float value)
    {
        if (value < 0F)
            return 0F;
        else if (value > 1F)
            return 1F;
        else
            return value;
    }

    // Interpolates between /a/ and /b/ by /t/. /t/ is clamped between 0 and 1.
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp01(t);
    }

    // Interpolates between /a/ and /b/ by /t/ without clamping the interpolant.
    public static float LerpUnclamped(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    // Same as ::ref::Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
    public static float LerpAngle(float a, float b, float t)
    {
        float delta = Repeat((b - a), 360);
        if (delta > 180)
            delta -= 360;
        return a + delta * Clamp01(t);
    }

    // Moves a value /current/ towards /target/.
    static public float MoveTowards(float current, float target, float maxDelta)
    {
        if (Mathf.Abs(target - current) <= maxDelta)
            return target;
        return current + Mathf.Sign(target - current) * maxDelta;
    }

    // Same as ::ref::MoveTowards but makes sure the values interpolate correctly when they wrap around 360 degrees.
    static public float MoveTowardsAngle(float current, float target, float maxDelta)
    {
        float deltaAngle = DeltaAngle(current, target);
        if (-maxDelta < deltaAngle && deltaAngle < maxDelta)
            return target;
        target = current + deltaAngle;
        return MoveTowards(current, target, maxDelta);
    }

    // Interpolates between /min/ and /max/ with smoothing at the limits.
    public static float SmoothStep(float from, float to, float t)
    {
        t = Mathf.Clamp01(t);
        t = -2.0F * t * t * t + 3.0F * t * t;
        return to * t + from * (1F - t);
    }

    //*undocumented
    public static float Gamma(float value, float absmax, float gamma)
    {
        bool negative = value < 0F;
        float absval = Abs(value);
        if (absval > absmax)
            return negative ? -absval : absval;

        float result = Pow(absval / absmax, gamma) * absmax;
        return negative ? -result : result;
    }

    // Gradually changes a value towards a desired goal over time.
    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
    {
        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = Mathf.Max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
        float change = current - target;
        float originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;
        change = Mathf.Clamp(change, -maxChange, maxChange);
        target = current - change;

        float temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        float output = target + (change + temp) * exp;

        // Prevent overshooting
        if (originalTo - current > 0.0F == output > originalTo)
        {
            output = originalTo;
            currentVelocity = (output - originalTo) / deltaTime;
        }

        return output;
    }

    // Gradually changes an angle given in degrees towards a desired goal angle over time.
    public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
    {
        target = current + DeltaAngle(current, target);
        return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    // Loops the value t, so that it is never larger than length and never smaller than 0.
    public static float Repeat(float t, float length)
    {
        return Clamp(t - Mathf.Floor(t / length) * length, 0.0f, length);
    }

    // PingPongs the value t, so that it is never larger than length and never smaller than 0.
    public static float PingPong(float t, float length)
    {
        t = Repeat(t, length * 2F);
        return length - Mathf.Abs(t - length);
    }

    // Calculates the ::ref::Lerp parameter between of two values.
    public static float InverseLerp(float a, float b, float value)
    {
        if (a != b)
            return Clamp01((value - a) / (b - a));
        else
            return 0.0f;
    }

    // Calculates the shortest difference between two given angles.
    public static float DeltaAngle(float current, float target)
    {
        float delta = Mathf.Repeat((target - current), 360.0F);
        if (delta > 180.0F)
            delta -= 360.0F;
        return delta;
    }

    static internal long RandomToLong(System.Random r)
    {
        var buffer = new byte[8];
        r.NextBytes(buffer);
        return (long)(System.BitConverter.ToUInt64(buffer, 0) & System.Int64.MaxValue);
    }

    internal static float ClampToFloat(double value)
    {
        if (double.IsPositiveInfinity(value))
            return float.PositiveInfinity;

        if (double.IsNegativeInfinity(value))
            return float.NegativeInfinity;

        if (value < float.MinValue)
            return float.MinValue;

        if (value > float.MaxValue)
            return float.MaxValue;

        return (float)value;
    }

    internal static int ClampToInt(long value)
    {
        if (value < int.MinValue)
            return int.MinValue;

        if (value > int.MaxValue)
            return int.MaxValue;

        return (int)value;
    }

    internal static uint ClampToUInt(long value)
    {
        if (value < uint.MinValue)
            return uint.MinValue;

        if (value > uint.MaxValue)
            return uint.MaxValue;

        return (uint)value;
    }

    internal static float RoundToMultipleOf(float value, float roundingValue)
    {
        if (roundingValue == 0)
            return value;
        return Mathf.Round(value / roundingValue) * roundingValue;
    }

    internal static float GetClosestPowerOfTen(float positiveNumber)
    {
        if (positiveNumber <= 0)
            return 1;
        return Mathf.Pow(10, Mathf.RoundToInt(Mathf.Log10(positiveNumber)));
    }

    internal static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
    {
        return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, kMaxDecimals);
    }

    internal static int GetNumberOfDecimalsForMinimumDifference(double minDifference)
    {
        return (int)System.Math.Max(0.0, -System.Math.Floor(System.Math.Log10(System.Math.Abs(minDifference))));
    }

    internal static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
    {
        if (minDifference == 0)
            return DiscardLeastSignificantDecimal(valueToRound);
        return (float)System.Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference),
            System.MidpointRounding.AwayFromZero);
    }

    internal static double RoundBasedOnMinimumDifference(double valueToRound, double minDifference)
    {
        if (minDifference == 0)
            return DiscardLeastSignificantDecimal(valueToRound);
        return System.Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference),
            System.MidpointRounding.AwayFromZero);
    }

    internal static float DiscardLeastSignificantDecimal(float v)
    {
        int decimals = Mathf.Clamp((int)(5 - Mathf.Log10(Mathf.Abs(v))), 0, kMaxDecimals);
        return (float)System.Math.Round(v, decimals, System.MidpointRounding.AwayFromZero);
    }

    internal static double DiscardLeastSignificantDecimal(double v)
    {
        int decimals = System.Math.Max(0, (int)(5 - System.Math.Log10(System.Math.Abs(v))));
        try
        {
            return System.Math.Round(v, decimals);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            // This can happen for very small numbers.
            return 0;
        }
    }

    public static int NextPowerOfTwo(int value)
    {
        value -= 1;
        value |= value >> 16;
        value |= value >> 8;
        value |= value >> 4;
        value |= value >> 2;
        value |= value >> 1;
        return value + 1;
    }
    public static int ClosestPowerOfTwo(int value)
    {
        int nextPower = NextPowerOfTwo(value);
        int prevPower = nextPower >> 1;
        if (value - prevPower < nextPower - value)

            return prevPower;
        else
            return nextPower;
    }
    public static bool IsPowerOfTwo(int value)
    {
        return (value & (value - 1)) == 0;
    }
}
