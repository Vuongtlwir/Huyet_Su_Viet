using System;
using System.Numerics;

public struct Vec3
{
    public float x;
    public float y;
    public float z;
    public override string ToString() => $"({x}, {y}, {z})";

}
class Program
{
    static void Main(string[] args)
    {
        Vec3 x1 = new Vec3 {x = 2, y = 3, z = 4};
        Vec3 x2 = new Vec3{x= 3, y = 5,z = 6};
        // x3 = x1+x2;  (ĐÚNG)
        Vec3 x3 = new Vec3 {x = x1.x+x2.x, y = x1.y+x2.y, z = x1.z+x2.z};
        Vector3 v1 = new Vector3(x1.x, x1.y, x1.z);
        Vector3 v2 = new Vector3(x2.x, x2.y, x2.z);
        Vector3 y3 = v1 +v2;
        Console.WriteLine($"+ {x3} ,{y3}");

        // x4 = x1-x2;  (ĐÚNG)
        Vec3 x4 = new Vec3 {x = x1.x-x2.x, y = x1.y-x2.y, z = x1.z-x2.z};
        Vector3 y4 = v1-v2;
        Console.WriteLine($"- {x4} ,{y4}");

        //x5 = k*x1;
        float k = 5;
        Vec3 x5 = new Vec3 {x = k*x1.x, y = k* x1.y, z = k* x1.z};  // (ĐÚNG: nhân vô hướng)
        Vector3 y5 = k*v1;
        Console.WriteLine($"+ {x5} ,{y5}");
        // magnitude
        float lengthVec = (float)Math.Sqrt((x1.x)*(x1.x)+ (x1.y)*(x1.y) + (x1.z)*(x1.z));  // (ĐÚNG)
        // SAI: System.Numerics.Vector3 KHÔNG có Vector3.Magnitude, và tham số phải là Vector3.
        //       Dùng v1.Length().
        float lengthVector3 = v1.Length();

        Console.WriteLine($"+ {lengthVec} ,{lengthVector3}");
        //Dot
        float x6 = x1.x* x2.x + x1.y*x2.y + x1.z*x2.z;  // (ĐÚNG)
        float y6 = Vector3.Dot(v1,v2);
        Console.WriteLine($". {x6} ,{y6}");
        // Cross
        Vec3 x7 = new Vec3 {x = x1.y * x2.z - x1.z * x2.y, y = x1.z*x2.x - x1.x* x2.z, z = x1.x*x2.y - x1.y*x2.x};  // (ĐÚNG)
        Vector3 y7 = Vector3.Cross(v1,v2);
        Console.WriteLine($"* {x7} ,{y7}");
        //normalize
        Vec3 x8 = new Vec3 {x = x1.x/(lengthVec),y = x1.y/ (lengthVec),z = x1.z/(lengthVec)};  // (ĐÚNG)

        Vector3 y8 = Vector3.Normalize(v1);
        Console.WriteLine($" {x8} ,{y8}");

        //Distance x1, x2
        float dis = (float)Math.Sqrt((x4.x)*(x4.x)+ (x4.y)*(x4.y) + (x4.z)*(x4.z));
        float disVector3 = Vector3.Distance(v1,v2);
        Console.WriteLine($"+ {dis} ,{disVector3}");

    }
}
