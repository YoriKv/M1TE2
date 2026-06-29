
namespace M1TE2
{
    public static class Maps
    {
        // Maps are stored internally as a fixed-stride grid so that map width can
        // be a real variable (32 or 64) without re-laying-out the arrays.
        //   index = layer * LAYER + y * W + x
        // The active editable region is bounded by Form1.map_width / map_height
        // (each <= 64). A 32-wide map simply uses columns 0..31 of each 64-wide row.
        public const int W = 64;          // allocated stride width (array row stride)
        public const int H = 64;          // allocated stride height
        public const int LAYER = W * H;   // entries per layer (4096)

        public static int[] tile = new int[LAYER * 3]; //x, y, 3 layers
        //tile can be value 0-1023, high 2 bits references the tileset

        public static int[] palette = new int[LAYER * 3]; //x, y, 3 layers
        public static int[] h_flip = new int[LAYER * 3]; //x, y, 3 layers
        public static int[] v_flip = new int[LAYER * 3]; //x, y, 3 layers
        public static int[] priority = new int[LAYER * 3]; //x, y, 3 layers
        // priority affects how sprite layers show above or below
        // but, you can't see the difference here

        //if the height is not 64, it will save a shorter map

        // index of tile (x,y) on layer for the current stride
        public static int Idx(int layer, int x, int y)
        {
            return (layer * LAYER) + (y * W) + x;
        }
    }

    public static class MapsC // copy and paste backup
    {
        public static int[] tile = new int[Maps.LAYER]; //x, y, 1 layer
                                                         //tile can be value 0-1023, high 2 bits references the tileset

        public static int[] palette = new int[Maps.LAYER]; //x, y, 1 layer
        public static int[] h_flip = new int[Maps.LAYER]; //x, y, 1 layer
        public static int[] v_flip = new int[Maps.LAYER]; //x, y, 1 layer
        public static int[] priority = new int[Maps.LAYER]; //x, y, 1 layer
        // priority affects how sprite layers show above or below
        // but, you can't see the difference here

    }
}
