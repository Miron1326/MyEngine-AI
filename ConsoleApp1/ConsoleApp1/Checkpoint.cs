using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Checkpoint
    {
        public Rectangle Rect;
        public bool IsCollected;
        public Color Color;

        public Checkpoint(float x, float y, float width, float height)
        {
            Rect = new Rectangle((int)x, (int)y, (int)width, (int)height);
            IsCollected = false;
            Color = Color.Gold;
        }
    }
}
