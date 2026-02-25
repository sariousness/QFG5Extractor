using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace QFG5Extractor.qfg5pano
{
    class Config
    {
        //bmp
        public const int bmpPixelDataStartIndex = 1078;

        //nod
        public const int nodPaletteStartIndex = 168;

        //img
        public const int imgBitmapWidthOffset = 0x0;
        public const int imgBitmapHeightOffset = 0x4;
        public const int imgHeaderSize = 32;
        public const int imgPixelDataStartIndex = imgHeaderSize * 2;
    }

    public class PanoConverter
    {
        public static void exportFromFiles(string nodFile, string imgFile, string destination)
        {
            if (!File.Exists(nodFile))
            {
                throw new FileNotFoundException("NOD file not found.", nodFile);
            }

            if (!File.Exists(imgFile))
            {
                throw new FileNotFoundException("IMG file not found.", imgFile);
            }

            //get palette data from nod
            byte[] paletteBytes = File.ReadAllBytes(nodFile);
            byte[] pixelBytes = File.ReadAllBytes(imgFile);

            if (paletteBytes.Length < 8)
            {
                throw new InvalidDataException("NOD file is too small: " + nodFile);
            }

            if (pixelBytes.Length < 38)
            {
                throw new InvalidDataException("IMG file is too small: " + imgFile);
            }

            //palette version: 0 = demo, 4 = retail
            int paletteVersion = paletteBytes[6];

            int bitmapWidth = BitConverter.ToInt16(new byte[2] { pixelBytes[32], pixelBytes[33] }, 0);
            int bitmapHeight = BitConverter.ToInt16(new byte[2] { pixelBytes[36], pixelBytes[37] }, 0);

            //unpack bits
            byte[] pixelData = unpackBits(ref bitmapWidth, ref bitmapHeight, ref pixelBytes, ref paletteVersion);

            Bitmap newBitmap;

            unsafe
            {
                fixed (byte* pixelDataPointer = pixelData)
                {
                    IntPtr pointer = (IntPtr)pixelDataPointer;

                    //create new bitmap object
                    newBitmap = new Bitmap(
                        bitmapWidth,
                        bitmapHeight,
                        bitmapWidth,
                        PixelFormat.Format8bppIndexed,
                        pointer
                    );
                }
            }

            ColorPalette tmpPalette = newBitmap.Palette;

            int i = Config.nodPaletteStartIndex;
            int j = 0;

            for (; i + 2 < paletteBytes.Length && j < tmpPalette.Entries.Length; i += 4)
            {
                tmpPalette.Entries[j] = Color.FromArgb(paletteBytes[i], paletteBytes[i + 1], paletteBytes[i + 2]);
                j++;
            }

            newBitmap.Palette = tmpPalette;
            newBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            newBitmap.Save(destination, ImageFormat.Bmp);
        }

        static byte[] unpackBits(ref int bitmapWidth, ref int bitmapHeight, ref byte[] pixelBytes, ref int fileVersion)
        {
            int i = Config.imgPixelDataStartIndex;
            int j = 0;
            
            int p = 0; //pixel
            
            int count;

            byte[] pixelData = new byte[bitmapWidth * bitmapHeight];

            //unpack bits
            while (i < pixelBytes.Length)
            {
                count = pixelBytes[i];

                if (count == 0)
                {
                    i++;
                }
                else if (count < 128)
                {
                    for (j = 0; j < count; j++)
                    {
                        pixelData[p] = (fileVersion > 0)? pixelBytes[i + 1] : (byte)(pixelBytes[i + 1] - 85);
                        p++;

                        if (p == pixelData.Length)
                        {
                            break;
                        }
                    }
                    i += 2;
                }
                else
                {
                    count = 256 - count;

                    for (j = 0; j < count; j++)
                    {
                        pixelData[p] = (fileVersion > 0)? pixelBytes[i + j + 1] : (byte)(pixelBytes[i + j + 1] - 85);
                        p++;

                        if (p == pixelData.Length)
                        {
                            break;
                        }
                    }
                    i = i + j + 1;
                }

                if (p == pixelData.Length)
                {
                    break;
                }
            }

            return pixelData;
        }
    }
}
