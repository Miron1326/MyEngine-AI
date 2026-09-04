using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class LevelData
    {
        public string Name { get; set; } = "";
        public List<Platform> Platforms { get; set; } = new List<Platform>();
        public List<Checkpoint> Checkpoints { get; set; } = new List<Checkpoint>();
        public float? FinishLineX { get; set; }
        public float? FinishLineY { get; set; }
        public float? FinishLineWidth { get; set; }
        public float? FinishLineHeight { get; set; }
    }
}
