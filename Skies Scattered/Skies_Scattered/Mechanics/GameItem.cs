using Skies_Scattered.Audio;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Skies_Scattered.UI;

namespace Skies_Scattered.Mechanics;
public sealed class MapGift
{
    public string Id { get; init; }
    public string Item { get; init; }
    public Rectangle Bounds { get; init; }
    public Rectangle Collision => new Rectangle(Bounds.X + 2, Bounds.Y + 12, Bounds.Width - 4, Bounds.Height - 12);

    public float DistanceTo(Vector2 feet)
    {
        var closest = new Vector2(MathHelper.Clamp(feet.X, Bounds.Left, Bounds.Right), MathHelper.Clamp(feet.Y, Bounds.Top, Bounds.Bottom));
        return Vector2.Distance(feet, closest);
    }
}

public static class GameItem
{
    public static void ShareInventory(List<CharacterStats> party)
    {
        var inventory = new List<string>();
        foreach (CharacterStats hero in party)
            hero.Items = inventory;
    }

    public static string Description(string item) => item switch
    {
        "Pan" => "Pan (+20 HP)",
        "Jugo" => "Jugo (+10 PP)",
        _ => item
    };
    public static bool TryUse(CharacterStats hero, int index, out string message)
    {
        if (index < 0 || index >= hero.Items.Count)
        {
            message = "No tienes ese objeto.";
            return false;
        }

        if (hero.HP <= 0)
        {
            message = "El personaje esta debilitado.";
            return false;
        }

        string item = hero.Items[index];
        int gained;
        switch (item)
        {
            case "Pan":
                gained = Math.Min(20, Math.Max(0, hero.MaxHP - hero.HP));
                if (gained == 0)
                {
                    message = "Ya tienes todos los HP.";
                    return false;
                }

                hero.HP += gained;
                message = $"{hero.Name} recupero {gained} HP.";
                break;
            case "Jugo":
                gained = Math.Min(10, Math.Max(0, hero.MaxPP - hero.PP));
                if (gained == 0)
                {
                    message = "Ya tienes todos los PP.";
                    return false;
                }

                hero.PP += gained;
                message = $"{hero.Name} recupero {gained} PP.";
                break;
            default:
                message = "Este objeto no se puede usar aqui.";
                return false;
        }

        hero.Items.RemoveAt(index);
        SoundEffects.Play(SoundCue.Item);
        return true;
    }
}
