# OledI2c128x64SH1106.cs
A single static C# class for the I2C Oled 128x64 display with SH1106 driver for the raspberry and .Net Core

## Prepare your Pi:

On your Raspberry Pi you need to install libgdiplus and some Windows fonts:
```
sudo apt-get install libgdiplus
sudo apt-get install ttf-mscorefonts-installer
```

Also you need to set Antialiasing off for the font used:
```
cd /home/pi/.config
sudo mkdir fontconfig
cd fontconfig
sudo nano fonts.conf
```
```
<fontconfig>
  <match target = "font" >
    <test name="family" qual="any">
      <string>Andale Mono</string>
    </test>
    <edit name="antialias" mode="assign">
      < bool > false </ bool >
    </ edit >
  </ match >
</ fontconfig >
```

## Usage:

Initialize once with:
```
OledI2c128x64SH1106.Init();
```

Change the bitmap e.g.:
```
using (Graphics g = Graphics.FromImage(OledI2c128x64SH1106.bitmap))
{
  g.Clear(Color.Black);
  //g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit; // you wish :-) see comments below
  g.DrawString("Hello World", OledI2c128x64SH1106.font, Brushes.White, 2, 2);                        
  g.DrawString("127.0.0.1", OledI2c128x64SH1106.font, Brushes.White, 2, 20);
}
```

Update:
```
OledI2c128x64SH1106.Update();
```

Release when done:
```
OledI2c128x64SH1106.Release();
```
## Comments
This is a black/white only display so antialiasing is in the way when drawing to a bitmap and converting it into display page.
Sadly '''TextRenderingHint.SingleBitPerPixelGridFit''' is not working on the Pi and the extra config file needs to be created for the font being used. This is just a quick hack, feel free to improve.

