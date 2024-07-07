#include <iostream>

extern "C"
{
    __declspec(dllexport) void AfficherMessage()
    {
        std::cout << "Bonjour depuis la DLL C++ !!!" << std::endl;
    }

    __declspec(dllexport) int Ajouter(int a, int b)
    {
        return a + b;
    }
}
