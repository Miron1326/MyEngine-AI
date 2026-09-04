using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RayColor = Raylib_cs.Color;
using RayRectangle = Raylib_cs.Rectangle;

namespace ConsoleApp1
{
    internal class Platform : ILevelObject
    {
        public float? Health;
        public float? MaxHealth;
        public RayColor BaseColor;

        [JsonIgnore]
        public string TypeName => "Platform";
        [JsonIgnore]
        public RayRectangle Bounds => new RayRectangle(X, Y, Width, Height);

        [JsonPropertyName("x")]
        public float X { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
        [JsonPropertyName("width")]
        public float Width { get; set; }
        [JsonPropertyName("height")]
        public float Height { get; set; }

        public Platform()
        {

        }

        public Platform(float x, float y, float width, float height, int? health = null)
        {
            X = x; Y = y; Width = width; Height = height;
            Health = health;
            MaxHealth = health;
            BaseColor = RayColor.LightGray;
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Bounds, RayColor.LightGray);
            Raylib.DrawRectangleLinesEx(Bounds, 2, RayColor.Magenta);

            if(Health < MaxHealth * 0.5f)
            {
                Raylib.DrawLine((int)X, (int)Y, (int)(X + Width), (int)(Y + Height), RayColor.Black);
            }
        }

        public bool CheckCollision(Vector2 ballPos, float ballRadius)
        {
            return Raylib.CheckCollisionCircleRec(ballPos, ballRadius, Bounds);
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
            return;
        }


    }
}
