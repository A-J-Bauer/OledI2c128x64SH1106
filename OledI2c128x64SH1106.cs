using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Device.I2c;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

// On the Raspberry Pi:
// ----------------------------------------------
// > sudo apt-get install libgdiplus
// > sudo apt-get install ttf-mscorefonts-installer
// > cd /home/pi/.config
// > sudo mkdir fontconfig
// > cd fontconfig
// > sudo nano fonts.conf
// add this:
// <fontconfig>
//   <match target = "font" >
//     <test name="family" qual="any">
//       <string>Andale Mono</string>
//     </test>
//     <edit name = "antialias" mode= "assign" >
//       < bool > false </ bool >
//     </ edit >
//   </ match >
// </ fontconfig >
//
// > fc-cache


namespace YourNamespace
{
    public static class OledI2c128x64SH1106
    {
        

        const byte WIDTH = 128;
        const byte HEIGHT = 64;
        const byte PAGES = HEIGHT / 8;

        //           B ... B
        //           Y ... Y
        //           T ... T
        //           E ... E
        //           0 ... 127
        //
        //           D0 .. D0
        //           D1 .. D1
        //           D2 .. D2
        // Page 0    D3 .. D3
        //           D4 .. D4
        //           D5 .. D5
        //           D6 .. D6
        //           D7 .. D7
        //
        //            .     .
        //            .     .
        //            .     .
        //            
        //           D0 .. D0
        //           D1 .. D1
        //           D2 .. D2
        // Page 7    D3 .. D3
        //           D4 .. D4
        //           D5 .. D5
        //           D6 .. D6
        //           D7 .. D7

        public static Bitmap bitmap = new Bitmap(WIDTH, HEIGHT, PixelFormat.Format24bppRgb);
        public static Font font = new Font("Andale Mono", 12, FontStyle.Regular, GraphicsUnit.Pixel);

        private static I2cConnectionSettings i2CConnectionSettings = new I2cConnectionSettings(0x01, 0x3C);
        private static I2cDevice i2cDevice = null;
        private static byte[] buffer = new byte[PAGES * WIDTH];
        private static Rectangle rectangle = new Rectangle(0, 0, WIDTH, HEIGHT);        

        private static readonly byte[] initCmd = new byte[]
        {
            0x00, // is command
            0x00, // 0x00-0x0F set lower column address
            0x10, // 0x10-0x1F set higher column address
            0x40, // set display start line 0x40-0x7F               
            0xD5, 0x80, // clock divider
            0xA8, 0x3F, // set multiplex ration mode 0xA8 0x00-0x3F
            0xD3, 0x00, // display offset 0            
            0x8D, 0x14, // enable charge pump
            0x20, 0x00, // memory adressing mode=horizontal
            0xA0, // set segment remap 0xA0/0xA1
            0xC0, // set common output scan direction 0xC0-0xC8
            0xDA, 0x12, // // com pins hardware configuration for 128x64
            0x81, 0x80, // contrast control 0x00-0xFF
            0xD9, 0x22, // pre-charge period
            0xDB, 0x20, // set vcomh deselect level
            0xA4, // set entire display OFF/ON 0xA4/0xA5
            0xA6, // display mode A6=normal, A7=inverse
            0x2E, // stop scrolling
            0xAF // display on/off 0xAE/0xAF            
        };

        private static byte[] pageCmd = new byte[]
        {
            0x00, // is command
            0xB0, // page address (B0-B7)
            0x00, // lower columns address =0
            0x10, // upper columns address =0
        };
        private static byte[] pageData = new byte[WIDTH + 1];

        private static void SendBuffer()
        {
            for (byte i = 0; i < PAGES; i++)
            {
                pageCmd[1] = (byte)(0xB0 + i); // page number
                i2cDevice.Write(pageCmd);

                pageData[0] = 0x40; // is data
                Buffer.BlockCopy(buffer, i * WIDTH, pageData, 1, WIDTH);
                i2cDevice.Write(pageData);
            }
        }

        public static bool Init()
        {            
            i2cDevice = I2cDevice.Create(i2CConnectionSettings);

            if (i2cDevice == null)
            {
                return false;
            }

            i2cDevice.Write(initCmd);

            Array.Clear(buffer, 0, buffer.Length);
            
            try
            {               
                SendBuffer();
            }
            catch (Exception)
            {
                return false;
            }            

            return true;
        }

        public static void Update()
        {
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            IntPtr ptr = bitmapData.Scan0;
            int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
            byte[] bmpRGB = new byte[bytes];
            Marshal.Copy(ptr, bmpRGB, 0, bytes);
            bitmap.UnlockBits(bitmapData);

            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j += 8)
                {
                    int l = 1;
                    byte data = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        byte bmpR = bmpRGB[((j + k) * WIDTH + i) * 3 + 0];
                        byte bmpG = bmpRGB[((j + k) * WIDTH + i) * 3 + 1];
                        byte bmpB = bmpRGB[((j + k) * WIDTH + i) * 3 + 2];
                        data |= (byte)(bmpR + bmpG + bmpB > 0 ? l : 0);
                        l *= 2;
                    }
                    buffer[j / 8 * WIDTH + i] = data;
                }
            }

            SendBuffer();
        }

        public static void Release()
        {
            if (i2cDevice == null)
            {
                i2cDevice.Dispose();
            }
        }

    }
}
