namespace ANIMOL.StarwayV04.Editor
{
    public static class StarwayV04FinalBatch
    {
        public static void ApplyAndCapture()
        {
            StarwayV04Builder.Build();
            StarwayV04Builder.Capture();
        }
    }
}
