using Raylib_cs;
using System.Numerics;
using System.Text.Json.Serialization;
using RayColor = Raylib_cs.Color;
using RayRectangle = Raylib_cs.Rectangle;

namespace ConsoleApp1
{
    internal class Checkpoint : ILevelObject
    {
        public bool IsCollected;
        public RayColor Color;
        [JsonIgnore]
        public string TypeName => "CheckPoint";
        [JsonIgnore]
        public RayRectangle Bounds => new RayRectangle(X, Y, Width, Height);

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Checkpoint()
        {

        }

        public Checkpoint(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
            IsCollected = false;
            Color = RayColor.Gold;
        }

        public bool CheckCollision(Vector2 ballPos, float ballRadius)
        {
            return !IsCollected && Raylib.CheckCollisionCircleRec(ballPos, ballRadius, Bounds);
        }

        public void Draw()
        {
            if (!IsCollected)
            {
                Raylib.DrawRectangleRec(Bounds, RayColor.Gold);
                Raylib.DrawRectangleLinesEx(Bounds, 2, RayColor.White);

                //пульсация
                float pulse = MathF.Sin((float)Raylib.GetTime() * 5) * 5;
                Raylib.DrawCircle((int)(X + Width / 2), (int)(Y + Height / 2), 10 + pulse, RayColor.Yellow);
            }
            else
            {
                Raylib.DrawRectangleRec(Bounds, new RayColor(100, 100, 100, 100));
                Raylib.DrawRectangleLinesEx(Bounds, 1, new RayColor(150, 150, 150, 100));
            }
        }

        public bool OnCollisionWithBallAndAditionActions(Vector2 ballPos, float ballRadius)
        {
            IsCollected = true;
            return false;
        }

        public void OnDestroy()
        {

        }
    }
}
