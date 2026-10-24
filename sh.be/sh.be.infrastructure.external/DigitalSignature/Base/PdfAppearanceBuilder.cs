using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using sh.be.infrastructure.external.DigitalSignature.Models;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.DigitalSignature.Base
{
    public class PdfAppearanceBuilder
    {
        private static readonly object PdfSharpLock = new();

        public PdfAppearanceBuilder(IFontResolver fontResolver)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            GlobalFontSettings.FontResolver = fontResolver;
        }

        public PdfRect MeasureText(string firstLine, string secondLine, string fontFamily, double fontSize)
        {
            lock (PdfSharpLock)
            {
                var font = new XFont(fontFamily, fontSize);
                var padding = fontSize * PdfSigningConst.APPEARANCE_PADDING_RATIO;
                using var gfx = XGraphics.CreateMeasureContext(new XSize(1, 1), XGraphicsUnit.Point, XPageDirection.Downwards);
                return new PdfRect
                {
                    Width = Math.Max(gfx.MeasureString(firstLine, font).Width, gfx.MeasureString(secondLine, font).Width) + padding * 2,
                    Height = font.GetHeight() * 2 + padding * 2,
                };
            }
        }

        public PdfAppearance BuildText(string firstLine, string secondLine, string? color, string fontFamily, double fontSize,
            int firstObjectNumber, double width, double height)
        {
            var textColor = HexColorReader.Read(color);
            var tempPdf = Draw(width, height, (gfx, box) =>
            {
                var font = new XFont(fontFamily, fontSize);
                var padding = fontSize * PdfSigningConst.APPEARANCE_PADDING_RATIO;
                var lineHeight = font.GetHeight();
                var brush = new XSolidBrush(textColor == null
                    ? XColors.Black
                    : XColor.FromArgb((int)Math.Round(textColor.Red * 255), (int)Math.Round(textColor.Green * 255), (int)Math.Round(textColor.Blue * 255)));
                gfx.DrawString(firstLine, font, brush, new XRect(padding, padding, box.Width - padding * 2, lineHeight), XStringFormats.TopLeft);
                gfx.DrawString(secondLine, font, brush, new XRect(padding, padding + lineHeight, box.Width - padding * 2, lineHeight), XStringFormats.TopLeft);
            });
            return Transplant(tempPdf, firstObjectNumber, width, height);
        }

        public PdfAppearance BuildEmpty(int firstObjectNumber, double width, double height)
        {
            return Transplant(Draw(width, height, (_, _) => { }), firstObjectNumber, width, height);
        }

        public byte[] DrawImage(byte[] image, int strokeWidth, double width, double height)
        {
            using var stream = new MemoryStream(image);
            return Draw(width, height, (gfx, box) =>
            {
                var xImage = XImage.FromStream(stream);
                foreach (var layer in GetStrokeLayers(box.Width, box.Height, strokeWidth))
                {
                    gfx.DrawImage(xImage, new XRect(layer.X, layer.Y, layer.Width, layer.Height));
                }
            });
        }

        public PdfAppearance BuildImage(byte[] drawnImagePdf, int intensity, string? color, int firstObjectNumber, double width, double height)
        {
            var appearance = Transplant(drawnImagePdf, firstObjectNumber, width, height);
            ApplyIntensityAndColor(appearance, intensity, color);
            return appearance;
        }

        private static List<PdfRect> GetStrokeLayers(double width, double height, int strokeWidth)
        {
            var radius = width * PdfSigningConst.STROKE_RADIUS_RATIO * strokeWidth / 100d;
            if (radius <= 0 || width <= radius * 2 || height <= radius * 2)
            {
                return [new PdfRect { Width = width, Height = height }];
            }

            var core = new PdfRect { X = radius, Y = radius, Width = width - radius * 2, Height = height - radius * 2 };
            var layers = new List<PdfRect>();
            for (var i = 0; i < PdfSigningConst.STROKE_DIRECTIONS; i++)
            {
                var angle = 2 * Math.PI * i / PdfSigningConst.STROKE_DIRECTIONS;
                layers.Add(new PdfRect { X = core.X + radius * Math.Cos(angle), Y = core.Y + radius * Math.Sin(angle), Width = core.Width, Height = core.Height });
            }
            layers.Add(core);
            return layers;
        }

        private static byte[] Draw(double width, double height, Action<XGraphics, XRect> draw)
        {
            lock (PdfSharpLock)
            {
                try
                {
                    using var document = new PdfDocument();
                    document.Options.CompressContentStreams = false;
                    var page = document.AddPage();
                    page.Width = XUnit.FromPoint(width);
                    page.Height = XUnit.FromPoint(height);
                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        draw(gfx, new XRect(0, 0, width, height));
                    }

                    using var stream = new MemoryStream();
                    document.Save(stream, false);
                    return stream.ToArray();
                }
                catch (Exception ex) when (ex is not UserFriendlyException)
                {
                    throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorAppearanceBuildFailed);
                }
            }
        }

        private static PdfAppearance Transplant(byte[] tempPdf, int firstObjectNumber, double width, double height)
        {
            var temp = PdfRevisionReader.Load(tempPdf);
            var catalog = PdfRevisionReader.GetObjectBody(temp, temp.RootObjectNumber) ?? String.Empty;
            var pages = Regex.Match(catalog, @"/Pages\s+(\d+)\s+\d+\s+R");
            var pagesBody = pages.Success ? PdfRevisionReader.GetObjectBody(temp, int.Parse(pages.Groups[1].Value)) : null;
            var kid = Regex.Match(pagesBody ?? String.Empty, @"/Kids\s*\[\s*(\d+)");
            var pageBody = kid.Success ? PdfRevisionReader.GetObjectBody(temp, int.Parse(kid.Groups[1].Value)) : null;
            var contents = Regex.Match(pageBody ?? String.Empty, @"/Contents\s+(\d+)\s+\d+\s+R");
            if (pageBody == null || !contents.Success)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorAppearanceBuildFailed);
            }

            var contentNumber = int.Parse(contents.Groups[1].Value);
            var contentDict = PdfRevisionReader.GetObjectBody(temp, contentNumber) ?? "<<>>";
            var contentData = PdfRevisionReader.GetRawStreamBytes(temp, contentNumber) ?? [];
            var resources = ReadResources(temp, pageBody);

            var order = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>(FindReferences(resources));
            while (queue.Count > 0)
            {
                var number = queue.Dequeue();
                if (!visited.Add(number))
                {
                    continue;
                }
                var body = PdfRevisionReader.GetObjectBody(temp, number);
                if (body == null)
                {
                    continue;
                }
                order.Add(number);
                foreach (var reference in FindReferences(body).Where(r => !visited.Contains(r)))
                {
                    queue.Enqueue(reference);
                }
            }

            var map = new Dictionary<int, int>();
            var next = firstObjectNumber + 1;
            foreach (var number in order)
            {
                map[number] = next++;
            }

            var formDict = new StringBuilder("<</Type /XObject/Subtype /Form/FormType 1/BBox [0 0 ")
                .Append(PdfObjectWriter.FormatNumber(width)).Append(' ').Append(PdfObjectWriter.FormatNumber(height))
                .Append("]/Resources ").Append(Renumber(resources, map));
            var filter = Regex.Match(contentDict, @"/Filter\s*(/\w+|\[[^\]]*\])");
            if (filter.Success)
            {
                formDict.Append("/Filter ").Append(filter.Groups[1].Value);
            }
            formDict.Append("/Length ").Append(contentData.Length).Append(">>");

            var appearance = new PdfAppearance { FormObjectNumber = firstObjectNumber };
            appearance.Objects.Add(new PdfIndirectObject { Number = firstObjectNumber, DictText = formDict.ToString(), StreamData = contentData });
            foreach (var number in order)
            {
                var data = PdfRevisionReader.GetRawStreamBytes(temp, number);
                var dict = Renumber(PdfRevisionReader.GetObjectBody(temp, number)!, map);
                if (data != null)
                {
                    dict = Regex.IsMatch(dict, @"/Length\s+\d+")
                        ? Regex.Replace(dict, @"/Length\s+\d+(\s+\d+\s+R)?", "/Length " + data.Length)
                        : dict[..dict.LastIndexOf(">>", StringComparison.Ordinal)] + "/Length " + data.Length + ">>";
                }
                appearance.Objects.Add(new PdfIndirectObject { Number = map[number], DictText = dict, StreamData = data });
            }
            return appearance;
        }

        private static string ReadResources(PdfRevision temp, string pageBody)
        {
            var at = pageBody.IndexOf("/Resources", StringComparison.Ordinal);
            if (at < 0)
            {
                return "<<>>";
            }

            var p = PdfRevisionReader.SkipWhiteSpace(pageBody, at + "/Resources".Length);
            if (p < pageBody.Length && pageBody[p] == '<')
            {
                return PdfRevisionReader.ExtractDictionary(pageBody, p);
            }

            var reference = Regex.Match(pageBody[p..], @"^(\d+)\s+\d+\s+R");
            return reference.Success ? PdfRevisionReader.GetObjectBody(temp, int.Parse(reference.Groups[1].Value)) ?? "<<>>" : "<<>>";
        }

        private static void ApplyIntensityAndColor(PdfAppearance appearance, int intensity, string? color)
        {
            var factor = intensity / 100d;
            var inkColor = HexColorReader.Read(color);
            if (Math.Abs(factor - 1d) < 0.001 && inkColor == null)
            {
                return;
            }

            var masks = appearance.Objects
                .SelectMany(o => Regex.Matches(o.DictText, @"/SMask\s+(\d+)\s+\d+\s+R").Select(m => int.Parse(m.Groups[1].Value)))
                .ToHashSet();
            var brightness = Math.Max(PdfSigningConst.MIN_BRIGHTNESS, 1 - (factor - 1) * PdfSigningConst.INTENSITY_BRIGHTNESS_FACTOR);
            var low = brightness * (1 - factor) / 2;
            var high = low + brightness * factor;

            foreach (var obj in appearance.Objects)
            {
                if (masks.Contains(obj.Number)
                    || !Regex.IsMatch(obj.DictText, @"/Subtype\s*/Image")
                    || Regex.IsMatch(obj.DictText, @"/ImageMask\s+true")
                    || Regex.IsMatch(obj.DictText, @"/Decode\s*\["))
                {
                    continue;
                }

                var components = CountColorComponents(obj.DictText);
                if (components == 0)
                {
                    continue;
                }

                var bits = ReadBitsPerComponent(obj.DictText);
                if (components == 1 && inkColor != null && bits <= 8)
                {
                    obj.DictText = Regex.Replace(obj.DictText, @"/ColorSpace\s*/DeviceGray", "/ColorSpace " + BuildIndexedPalette(low, high, (1 << bits) - 1, inkColor));
                    continue;
                }

                var close = obj.DictText.LastIndexOf(">>", StringComparison.Ordinal);
                obj.DictText = obj.DictText[..close] + BuildDecodeArray(low, high, components, inkColor) + obj.DictText[close..];
            }
        }

        private static string BuildDecodeArray(double low, double high, int components, RgbColor? color)
        {
            var tint = color != null && components == 3;
            var channels = new[] { color?.Red ?? 0d, color?.Green ?? 0d, color?.Blue ?? 0d };
            var values = new List<string>();
            for (var i = 0; i < components; i++)
            {
                var channel = tint ? channels[i] : 0d;
                values.Add(FormatColorValue(channel + (1 - channel) * low));
                values.Add(FormatColorValue(channel + (1 - channel) * high));
            }
            return "/Decode [" + string.Join(" ", values) + "]";
        }

        private static string BuildIndexedPalette(double low, double high, int maxIndex, RgbColor color)
        {
            var palette = new StringBuilder();
            for (var i = 0; i <= maxIndex; i++)
            {
                var gray = Math.Clamp(low + (double)i / maxIndex * (high - low), 0d, 1d);
                foreach (var channel in new[] { color.Red, color.Green, color.Blue })
                {
                    palette.Append(((int)Math.Round(Math.Clamp(channel + (1 - channel) * gray, 0d, 1d) * 255)).ToString("X2"));
                }
            }
            return $"[/Indexed /DeviceRGB {maxIndex} <{palette}>]";
        }

        private static int ReadBitsPerComponent(string dictText)
        {
            var match = Regex.Match(dictText, @"/BitsPerComponent\s+(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 8;
        }

        private static string FormatColorValue(double value)
        {
            return PdfObjectWriter.FormatNumber(Math.Clamp(value, 0d, 1d));
        }

        private static int CountColorComponents(string dictText)
        {
            if (Regex.IsMatch(dictText, @"/ColorSpace\s*/DeviceRGB"))
            {
                return 3;
            }
            if (Regex.IsMatch(dictText, @"/ColorSpace\s*/DeviceGray"))
            {
                return 1;
            }
            return Regex.IsMatch(dictText, @"/ColorSpace\s*/DeviceCMYK") ? 4 : 0;
        }

        private static IEnumerable<int> FindReferences(string dictRaw)
        {
            return Regex.Matches(dictRaw, @"(\d+)\s+\d+\s+R\b").Select(m => int.Parse(m.Groups[1].Value));
        }

        private static string Renumber(string dictRaw, IReadOnlyDictionary<int, int> map)
        {
            return Regex.Replace(dictRaw, @"(\d+)(\s+\d+\s+R\b)", match =>
            {
                var number = int.Parse(match.Groups[1].Value);
                return (map.TryGetValue(number, out var target) ? target : number) + match.Groups[2].Value;
            });
        }
    }
}
