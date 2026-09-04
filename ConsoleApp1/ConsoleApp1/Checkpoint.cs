using Raylib_cs;
using System.Numerics;
using RayColor = Raylib_cs.Color;
using RayRectangle = Raylib_cs.Rectangle;

namespace ConsoleApp1
{
    internal class Checkpoint : ILevelObject
    {
        public RayRectangle Rect;
        public bool IsCollected;
        public RayColor Color;
        public string TypeName => "CheckPoint";
        public RayRectangle Bounds => Rect;

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Checkpoint(float x, float y, float width, float height)
        {
            Rect = new RayRectangle((int)x, (int)y, (int)width, (int)height);
            IsCollected = false;
            Color = RayColor.Gold;
        }

        public bool CheckCollision(Vector2 ballPos, float ballRadius)
        {
            return !IsCollected && Raylib.CheckCollisionCircleRec(ballPos, ballRadius, Rect);
        }

        public void Draw()
        {
            if (!IsCollected)
            {
                Raylib.DrawRectangleRec(Rect, Color);
                Raylib.DrawRectangleLinesEx(Rect, 2, RayColor.White);

                //пульсация
                float pulse = MathF.Sin((float)Raylib.GetTime() * 5) * 5;
                Raylib.DrawCircle((int)(Rect.X + Rect.Width / 2), (int)(Rect.Y + Rect.Height / 2), 10 + pulse, RayColor.Yellow);
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
