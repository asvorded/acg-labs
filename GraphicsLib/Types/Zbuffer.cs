namespace GraphicsLib.Types
{
    public class Zbuffer
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private float[] buffer;
        public Zbuffer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            buffer = new float[width * height];
            Clear();
        }
        public void Clear()
        {
            for(int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = float.PositiveInfinity;
            }
        }
        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new ArgumentOutOfRangeException("x or y out of buffer range");
                }
                return buffer[y * Width + x];
            }
            set
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new ArgumentOutOfRangeException("x or y out of buffer range");
                }
                buffer[y * Width + x] = value;
            }
        }
        public bool TestAndSet(int x, int y, float depth)
        {
            float currentdepth = this[x, y];
            if(currentdepth > depth)
            {
                this[x, y] = depth;
                return true;
            } else
            {
                return false;
            }

        }
    }
}
