// Learn more about F# at http://fsharp.net

open System
open System.Drawing
open System.Drawing.Imaging
open System.Runtime.InteropServices
open System.Media
open System.Diagnostics

module user32 =
    [<DllImport("user32.dll")>]
    extern bool GetCursorPos(Point* lpPoint)

module gdi32 =
    [<DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)>]
    extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop)

let simpleSound = new SoundPlayer(@"c:\Windows\Media\notify.wav")

let width = 215
let height = 214
let left = 46
let top = 706

let screenPollDelay = 100
let alertDelay = 3000

let screenOld = new Bitmap(width, height, PixelFormat.Format32bppArgb)
let screenNew = new Bitmap(width, height, PixelFormat.Format32bppArgb)

let updateColorBlock buffer =
    use gdest = Graphics.FromImage buffer
    use gsrc = Graphics.FromHwnd IntPtr.Zero
    let retval = gdi32.BitBlt((gdest.GetHdc()), 0, 0, width, height, (gsrc.GetHdc()), left, top, (int)CopyPixelOperation.SourceCopy)
    gdest.ReleaseHdc()
    gsrc.ReleaseHdc()

let compareBlocks (buffer1:Bitmap) (buffer2:Bitmap) =
    let mutable different = false

    for x in 0 .. width - 1 do
        for y in 0 .. height - 1 do
            let c1 = buffer1.GetPixel(x, y).GetHue()
            let c2 = buffer2.GetPixel(x, y).GetHue()
            if Math.Abs(c1 - c2) > 0.0f then
                different <- true
    different

let rec test screenOld screenNew delay = 
    async {
        do! Async.Sleep screenPollDelay

        updateColorBlock screenOld
        
        if compareBlocks screenOld screenNew then
            printf "%i " delay
            if delay < 0 then
                printf "yay "
                simpleSound.Play()
            return! test screenNew screenOld alertDelay
        else
            if delay <= 0 && delay + screenPollDelay > 0 then
                printf "clear "
            return! test screenNew screenOld (delay - screenPollDelay)
    }

updateColorBlock screenOld
updateColorBlock screenNew

test screenOld screenNew 0
|> Async.RunSynchronously

