using System.Runtime.InteropServices;
using UnityEngine;

/* TODO:
    To make the dll accessible from here, it have to placed into 7 Days To Die\7DaysToDie_Data\Plugins
*/

class Builder
{
    [DllImport("lib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AfficherMessage();

    [DllImport("lib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ajouter(int a, int b);
}