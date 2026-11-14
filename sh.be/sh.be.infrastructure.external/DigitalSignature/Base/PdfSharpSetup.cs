using PdfSharp.Fonts;

namespace sh.be.infrastructure.external.DigitalSignature.Base
{
    public static class PdfSharpSetup
    {
        public static readonly object Lock = new();

        // PDFsharp giữ font resolver toàn cục và ném lỗi khi đổi sau lần dùng font đầu tiên, nên chỉ gán khi chưa có
        public static void EnsureFontResolver(IFontResolver fontResolver)
        {
            lock (Lock)
            {
                if (GlobalFontSettings.FontResolver != null)
                {
                    return;
                }
                GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                GlobalFontSettings.FontResolver = fontResolver;
            }
        }
    }
}
