using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RayRectangle = Raylib_cs.Rectangle;

namespace ConsoleApp1
{
    internal class Platform
    {
        public RayRectangle Rect;
        public float? Health;
        public float? MaxHealth;
        public Color BaseColor;

        public Platform(float x, float y, float width, float height, int? health = null)
        {
            Rect = new RayRectangle((int)x, (int)y, (int)width, (int)height);
            Health = health;
            MaxHealth = health;
            BaseColor = Color.LightGray;
        }
    }
}
