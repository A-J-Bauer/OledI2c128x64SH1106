# OledI2c128x64SH1106.cs
A single static C# class for the I2C Oled 128x64 display with SH1106 driver for the Raspberry and .Net Core

![Repo Image](https://repository-images.githubusercontent.com/234853723/af21c280-3b4c-11ea-8967-9ed15f41b516)


## Prepare your Pi:

On your Raspberry Pi you need to install libgdiplus and some Windows fonts:
```
sudo apt-get install libgdiplus
sudo apt-get install ttf-mscorefonts-installer
```

Also you need to set Antialiasing off for the font being used:
```
cd /home/pi/.config
sudo mkdir fontconfig
cd fontconfig
sudo nano fonts.conf
```
add
```
<fontconfig>
  <match target="font">
    <test name="family" qual="any">
      <string>Andale Mono</string>
    </test>
    <edit name="antialias" mode="assign">
      <bool>false</bool>
    </edit>
  </match>
</fontconfig>
```
update font cache
```
fc-cache
```

## Usage:
```
using System.Drawing;
using OledI2C;
```

Initialize once with:
```
OledI2c128x64SH1106.Init();
```

Rotate if needed:
```
OledI2c128x64SH1106.Rotate(true);
```

Change the bitmap with GDI+ functions, e.g.:
```
using (Graphics g = Graphics.FromImage(OledI2c128x64SH1106.bitmap))
{
  g.Clear(Color.Black);
  //g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit; // you wish :-) see comments below
  g.DrawRectangle(Pens.White, new Rectangle(0, 0, 127, 63));
  g.DrawString("Hello World", OledI2c128x64SH1106.font, Brushes.White, 2, 2);                        
  g.DrawString("127.0.0.1", OledI2c128x64SH1106.font, Brushes.White, 2, 20);
}
```

Update:
```
OledI2c128x64SH1106.Update();
```

Release when not used any more (e.g. in Dispose):
```
OledI2c128x64SH1106.Release();
```
## Comments
This is a black/white only display so antialiasing is in the way when drawing to a bitmap and converting it into display pages.
"TextRenderingHint.SingleBitPerPixelGridFit" is not working on the Pi/Linux so the extra config file needs to be created for the font being used.

