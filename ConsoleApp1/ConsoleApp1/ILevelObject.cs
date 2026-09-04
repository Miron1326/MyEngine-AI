using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RayRectangle = Raylib_cs.Rectangle;

namespace ConsoleApp1
{
    internal interface ILevelObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        string TypeName {  get; }
        RayRectangle Bounds { get; }

        void Draw();
        bool CheckCollision(Vector2 ballPos, float ballRadius);
        bool OnCollisionWithBallAndAditionActions(Vector2 ballPos, float ballRadius);
        void OnDestroy();
    }
}
