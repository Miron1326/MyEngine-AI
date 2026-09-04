using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RayRectangle = Raylib_cs.Rectangle;
using RayColor = Raylib_cs.Color;
using System.Numerics;

namespace ConsoleApp1
{
    internal class Platform : ILevelObject
    {
        public RayRectangle Rect;
        public float? Health;
        public float? MaxHealth;
        public RayColor BaseColor;

        public string TypeName => "Platform";
        public RayRectangle Bounds => Rect;

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Platform(float x, float y, float width, float height, int? health = null)
        {
            Rect = new RayRectangle((int)x, (int)y, (int)width, (int)height);
            Health = health;
            MaxHealth = health;
            BaseColor = RayColor.LightGray;
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Rect, BaseColor);
            Raylib.DrawRectangleLinesEx(Rect, 2, RayColor.Black);

            if(Health < MaxHealth * 0.5f)
            {
                Raylib.DrawLine((int)Rect.X, (int)Rect.Y, (int)(Rect.X + Rect.Width), (int)(Rect.Y + Rect.Height), RayColor.Black);
            }
        }

        public bool CheckCollision(Vector2 ballPos, float ballRadius)
        {
            return Raylib.CheckCollisionCircleRec(ballPos, ballRadius, Rect);
        }
        public bool OnCollisionWithBallAndAditionActions(Vector2 ballPos, float ballRadius)
        {
            Health -= 1;

            if(Health <= 0)
            {
                OnDestroy();
                return true;
            }
            return false;
        }

        public void OnDestroy()
        {
            throw new NotImplementedException();
        }


    }
}
