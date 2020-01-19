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


namespace OledI2C
{
    public static class OledI2c128x64SH1106
    {        
        private const byte SH_WIDTH = 132; // SH1106 SRAM 132x64
        private const byte SH_HEIGHT = 64;
        private const byte SH_PAGES = SH_HEIGHT / 8;
        private const byte PX_WIDTH = 128;
        private const byte PX_HEIGHT = 64;

        //           B ... B
        //           Y ... Y
        //           T ... T
        //           E ... E
        //           0 ... 131
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

        private static I2cConnectionSettings i2CConnectionSettings = new I2cConnectionSettings(0x01, 0x3C);
        private static I2cDevice i2cDevice = null;
        private static byte[] buffer = new byte[SH_PAGES * SH_WIDTH];
        private static Rectangle rectangle = new Rectangle(0, 0, PX_WIDTH, PX_HEIGHT);

        public static Bitmap bitmap = new Bitmap(PX_WIDTH, PX_HEIGHT, PixelFormat.Format24bppRgb);        
        public static Font font = new Font("Andale Mono", 12, FontStyle.Regular, GraphicsUnit.Pixel);

        private static readonly byte[] initCmd = new byte[]
        {
            0x00, // is command
            0x00,       // SH1106 [01] 0x00-0x0F set lower column address
            0x10,       // SH1106 [02] 0x10-0x1F set higher column address
            0x32,       // SH1106 [03] 0x30-0x33 set pump voltage value
            0x40,       // SH1106 [04] set display start line 0x40-0x7F       
            0x81, 0x80, // SH1106 [05] set contrast control register 0x00-0xFF
            0xA0,       // SH1106 [06] set segment re-map 0xA0/0xA1 normal/reverse
            0xA4,       // SH1106 [07] set entire display normal/force on 0xA4/0xA5
            0xA6,       // SH1106 [08] set normal/reverse display 0xA6/0xA7
            0xA8, 0x3F, // SH1106 [09] set multiplex ratio 0x00-0x3F        
            0xAD, 0x8B, // SH1106 [10] set dc-dc off/on 0x8A-0x8B disable/on when display on
            0xC0,       // SH1106 [13] set output scan direction 0xC0/0xC8
            0xD3, 0x00, // SH1106 [14] set display offset 0x00-0x3F
            0xD5, 0x80, // SH1106 [15] set display clock divide ratio/oscillator 
            0xD9, 0x22, // SH1106 [16] set discharge/precharge period 0x00-0xFF           
            0xDA, 0x12, // SH1106 [17] set common pads hardware configuration 0x02/0x12 sequential/alternative                        
            0xDB, 0x20, // SH1106 [18] set vcom deselect level 0x00-0xFF

            0xAF        // SH1106 [11] display off/on 0xAE/0xAF 
        };
        private static byte[] pageCmd = new byte[]

        {
            0x00, // is command
            0xB0, // page address (B0-B7)
            0x00, // lower columns address =0
            0x10, // upper columns address =0
        };
        private static byte[] pageData = new byte[SH_WIDTH + 1];

        private static void SendBuffer()
        {            
            for (byte i = 0; i < SH_PAGES; i++)
            {
                pageCmd[1] = (byte)(0xB0 + i); // page number SH1106 [12]
                i2cDevice.Write(pageCmd);

                pageData[0] = 0x40; // is data
                Buffer.BlockCopy(buffer, i * SH_WIDTH, pageData, 1, SH_WIDTH);
                i2cDevice.Write(pageData);
            }
        }

        public static bool Init()
        {                        
            if ((i2cDevice = I2cDevice.Create(i2CConnectionSettings)) == null)
            {
                return false;
            }
            
            i2cDevice.Write(initCmd);

            Array.Clear(buffer, 0, buffer.Length);

            SendBuffer();

            return true;
        }

       public static void Rotate(bool rotate)
        {
            i2cDevice.Write(new byte[]
            {
                0x00, // is command              
                (byte)(rotate?0xA1:0xA0),   // SH1106 [06] set segment re-map 0xA0/0xA1 normal/reverse                
                (byte)(rotate?0xC8:0xC0),   // SH1106 [13] set output scan direction 0xC0/0xC8               
            });
        }

        public static void Power(bool on)
        {
            i2cDevice.Write(new byte[]
            {
                0x00, // is command               
                (byte)(on?0xAF:0xAE)       // SH1106 [11] display off/on 0xAE/0xAF 
            });
        }

        public static void Update()
        {            
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            IntPtr ptr = bitmapData.Scan0;
            int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
            byte[] bmpRGB = new byte[bytes];
            Marshal.Copy(ptr, bmpRGB, 0, bytes);
            bitmap.UnlockBits(bitmapData);

            int bufferOffset = (SH_WIDTH - PX_WIDTH) / 2;
            for (int i = 0; i < PX_WIDTH; i++)
            {
                for (int j = 0; j < PX_HEIGHT; j += 8)
                {
                    int l = 1;
                    byte data = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        byte bmpR = bmpRGB[((j + k) * PX_WIDTH + i) * 3 + 0];
                        byte bmpG = bmpRGB[((j + k) * PX_WIDTH + i) * 3 + 1];
                        byte bmpB = bmpRGB[((j + k) * PX_WIDTH + i) * 3 + 2];
                        data |= (byte)(bmpR + bmpG + bmpB > 0 ? l : 0);
                        l *= 2;
                    }
                    buffer[j / 8 * SH_WIDTH + i+ bufferOffset] = data;
                }
            }

            SendBuffer();
        }

        public static void Release()
        {
            if (i2cDevice != null)
            {
                Power(false);

                i2cDevice.Dispose();
                i2cDevice = null;
            }
        }

    }
}
