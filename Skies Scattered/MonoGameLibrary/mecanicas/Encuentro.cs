using System;

namespace MonoGameLibrary.Mecanicas;

public class Encuentro
{
    private readonly Random _rng = new Random();
    private float _distanciaAcumulada = 0f;

    public float Probabilidad { get; private set; }

    public bool Activo { get; private set; }

    public float PixelesPorPaso = 100f;

    public float AumentoPorPaso = 0.005f;

    public void Update(float distanciaRecorrida)
    {
        if (Activo || distanciaRecorrida <= 0f)
            return;

        _distanciaAcumulada += distanciaRecorrida;

        // Puede completarse más de un paso en un frame si la velocidad es alta
        while (_distanciaAcumulada >= PixelesPorPaso)
        {
            _distanciaAcumulada -= PixelesPorPaso;

            // El contador sube con cada paso...
            Probabilidad = Math.Min(1f, Probabilidad + AumentoPorPaso);

            // ...y en cada paso se tira el dado
            if (_rng.NextDouble() < Probabilidad)
            {
                Activo = true;
                return;
            }
        }
    }

    /// <summary>Llamar al terminar el encuentro.</summary>
    public void Reiniciar()
    {
        Activo = false;
        Probabilidad = 0f;
        _distanciaAcumulada = 0f;
    }
}