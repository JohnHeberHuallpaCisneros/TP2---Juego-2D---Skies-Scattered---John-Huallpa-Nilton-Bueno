using System;

namespace Skies_Scattered.Mechanics;

public class Encounter
{
    private readonly Random _rng = new Random();
    private float _distanciaAcumulada = 0f;
    private int _pasosRestantes = 0;
    
    public float PixelesPorPaso { get; set; } = 32f;
    public bool Activo { get; private set; } = false;

    // Propiedad pública para ver los pasos restantes en la ventana (ideal para depurar)
    public int PasosRestantes => _pasosRestantes;

    public Encounter()
    {
        GenerarNuevosPasosObjetivo();
    }

    public void Update(float distanciaRecorrida)
    {
        if (Activo || distanciaRecorrida <= 0f)
            return;

        _distanciaAcumulada += distanciaRecorrida;

        while (_distanciaAcumulada >= PixelesPorPaso)
        {
            _distanciaAcumulada -= PixelesPorPaso;
            _pasosRestantes--;

            if (_pasosRestantes <= 0)
            {
                Activo = true;
                break;
            }
        }
    }

    private void GenerarNuevosPasosObjetivo()
    {
        // Rango de pasos más largo (ej. entre 25 y 45 pasos)
        _pasosRestantes = _rng.Next(25, 46);
    }

    public void Reiniciar()
    {
        Activo = false;
        _distanciaAcumulada = 0f;
        GenerarNuevosPasosObjetivo();
    }
}