
using Microsoft.Xna.Framework.Input;

namespace MonoGameLibrary;

public static class Input
{
    private static KeyboardState _tecladoActual;
    private static KeyboardState _tecladoAnterior;

    public static void Update()
    {
        _tecladoAnterior = _tecladoActual;
        _tecladoActual = Keyboard.GetState();
    }

    public static bool IsKeyDown(Keys tecla)
    {
        return _tecladoActual.IsKeyDown(tecla);
    }

    public static bool IsKeyPressed(Keys tecla)
    {
        return _tecladoActual.IsKeyDown(tecla) &&
            _tecladoAnterior.IsKeyUp(tecla);
    }
}
