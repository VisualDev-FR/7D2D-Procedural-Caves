#include <iostream>

extern "C"
{
    struct CavePrefabInput
    {
        int prefabIndex;

        int pos_x;
        int pos_y;
        int pos_z;

        int size_x;
        int size_y;
        int size_z;
    };

    __declspec(dllexport) int Ajouter(int a, int b)
    {
        return a + b;
    }

    __declspec(dllexport) int ProcessCavePrefab(CavePrefabInput *input)
    {
        return input->pos_x + input->pos_y + input->pos_z;
    }
}
