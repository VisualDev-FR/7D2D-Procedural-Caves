using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential)]
public struct CavePrefabInterop
{
    public int prefabIndex;

    public int pos_x;

    public int pos_y;

    public int pos_z;

    public int size_x;

    public int size_y;

    public int size_z;
}


public class CppPlugin
{

#if RELEASE
    public const string dllName = @"Mods\TheDescent\Libs\CaveBuilder.dll";
#else
    public const string dllName = @"..\Libs\CaveBuilder.dll";
#endif

    [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ajouter(int a, int b);

    [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ProcessCavePrefab(ref CavePrefabInterop prefab);
}
