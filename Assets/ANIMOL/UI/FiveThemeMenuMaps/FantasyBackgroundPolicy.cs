using System;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu
{
    public static class FantasyBackgroundPolicy
    {
        public const int Width=352, Height=704, FootY=526, RabbitY=436;
        public const double Period=5, Transition=.36;
        public static int Round(double x) => (int)Math.Floor(x+.5);
        public static int ThemeAt(double t) => (int)(Math.Floor(Math.Max(0,t)/Period)%5);
        public static int FrameAt(double t) => (int)(Math.Floor(Math.Max(0,t)*14)%8);
        // Matches reference/animation.js for seed=0. Salts keep the two choices independent.
        public static int Direction(long slot,int salt,uint seed=0)
        {
            unchecked {
                uint x=((uint)(slot+11)*92821u)^((uint)salt*0x9e3779b1u)^seed;
                x+=117; x^=x<<13; x^=x>>17; x^=x<<5;
                return x<0x80000000u ? -1 : 1;
            }
        }
        public static RectInt Plane(double p,int direction,float scale,float speed)
        {
            int w=Round(Width*scale),h=Round(Height*scale);
            return new RectInt(Round((Width-w)/2.0+direction*88*(p-.5)*speed),
                Round((Height-h)/2.0+72*(p-.5)*speed),w,h);
        }
        public static int RabbitX(double p,int direction) => Round(direction==1 ? -64+p*416 : 352-p*416);
        public static bool NewBlock(int x,int y,double local) => .5*(x/8*8)/Width+.5*(y/8*8)/Height < local/Transition*1.22-.11;
    }
}
