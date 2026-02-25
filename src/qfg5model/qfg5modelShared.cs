using System;

namespace QFG5Extractor.qfg5model
{
    public static class Constants
    {
        public const int MDL_HEADER_NAME_ADDRESS = 0x0C;
        public const int MDL_HEADER_NUMBER_OF_SUBMESHES_ADDRESS = 0x1C;
        public const int MDL_HEADER_BITMAPPALETTE_ADDRESS = 0x2D;
        public const int MDL_HEADER_BITMAPHEADERADDRESSREFERENCE_ADDRESS = 0x428;
        public const int MDL_HEADER_3DDATA_ADDRESS = 0x42c;

        public const int BITMAP_PALETTE_LENGTH = 1019;
        public const int SUBMESH_NAME_LENGTH = 10;
        public const int SUBMESH_DATAPOINTS_PER_VERTEX = 3;
        public const int SUBMESH_DATAPOINTS_PER_UV_COORDS = 2;
        public const int SUBMESH_DATAPOINTS_PER_FACE = 10;
        public const int SUBMESH_DATAPOINTS_VERTICES_PER_FACE = 3;
        public const int SUBMESH_DATAPOINTS_UVCOORDS_PER_FACE = 3;
        public const int SUBMESH_DATAPOINTS_PER_NORMAL = 3;
        public const int SUBMESH_DATAPOINTS_NORMALS_PER_FACE = 1;
        public const int SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_VERTEX = 0;
        public const int SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD = 3;
        public const int SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN = 6;
        public const int SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL = 7;
        public const int SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY = 4;
        public const int SUBMESH_NUMBER_OF_LIGHTING_ENTRIES_PER_VERTEX = SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY;
        public const int SUBMESH_HEADER_NAME_ADDRESS = 0x00;
        public const int SUBMESH_HEADER_NUMBER_OF_VERTICES_ADDRESS = 0x60;
        public const int SUBMESH_HEADER_NUMBER_OF_UVCOORDS_ADDRESS = 0x64;
        public const int SUBMESH_HEADER_NUMBER_OF_FACES_ADDRESS = 0x68;
        public const int SUBMESH_HEADER_VERTICES_LIST_ADDRESS = 0x7C;

        public const int BYTES_PER_INT = 4;

        public const float SOME_LARGE_DISTANCE = 10000000.0f;
        public const float DOUBLE_MIN_PRECISION = 0.00001f;
        public const float DOUBLE_RELAXED_PRECISION = 0.01f;
        public const float DOUBLE_MODERATELY_RELAXED_PRECISION = 0.0001f;

        public const int MDL_BITMAP_HEADER_TYPE_MULTIPLIER = 4;
        public const int SUBBITMAP_HEADER_LENGTH = 24;
        public const int SUBBITMAP_HEADER_WIDTH_FLOAT_ADDRESS = 0;
        public const int SUBBITMAP_HEADER_HEIGHT_FLOAT_ADDRESS = 4;
        public const int SUBBITMAP_HEADER_WIDTH_POWER_OF_TWO_ADDRESS = 8;
        public const int SUBBITMAP_HEADER_HEIGHT_POWER_OF_TWO_ADDRESS = 12;
        public const int SUBBITMAP_HEADER_WIDTH_MINUS_ONE_ADDRESS = 16;
        public const int SUBBITMAP_HEADER_HEIGHT_MINUS_ONE_ADDRESS = 20;

        public const string BMP_EXTENSION = ".BMP";
        public const string MDL_EXTENSION = ".MDL";
        public const string HAK_EXTENSION = ".hak";
        public const string LDR_EXTENSION = ".ldr";
    }

    public static class qfg5modelShared
    {
        public static float bytesToFloat(byte[] tempByteArray)
        {
            return BitConverter.ToSingle(tempByteArray, 0);
        }

        public static string convertFloatToManagedString(float f)
        {
            return f.ToString("0.000000");
        }

        public static string convertIntToManagedString(int i)
        {
            return i.ToString();
        }
    }
}
