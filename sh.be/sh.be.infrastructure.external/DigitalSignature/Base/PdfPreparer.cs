using sh.be.applications.Base.Dtos.DigitalSignature;
using sh.be.infrastructure.external.DigitalSignature.Models;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.DigitalSignature.Base
{
    public class PdfPreparer
    {
        private readonly PdfAppearanceBuilder _appearanceBuilder;

        public PdfPreparer(PdfAppearanceBuilder appearanceBuilder)
        {
            _appearanceBuilder = appearanceBuilder;
        }

        public PdfPrepared Prepare(byte[] pdf, PdfSigningContextDto context, DateTime signedAt)
        {
            var revision = PdfRevisionReader.Load(pdf);
            var catalog = PdfRevisionReader.GetObjectBody(revision, revision.RootObjectNumber)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            var pages = Regex.Match(catalog, @"/Pages\s+(\d+)\s+\d+\s+R");
            if (!pages.Success)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var pagesNumber = int.Parse(pages.Groups[1].Value);
            var pageOrder = ReadPageOrder(revision, pagesNumber);
            if (pageOrder.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var next = revision.Size;
            var fieldNumber = next++;
            var valueNumber = next++;
            var plan = PlanAnnotations(revision, pageOrder, pagesNumber, context, signedAt, next);
            next = plan.NextObjectNumber;

            var widgets = plan.Annotations.Where(a => a.IsSignature).ToList();
            var stamps = plan.Annotations.Where(a => !a.IsSignature).ToList();
            if (widgets.Count == 1)
            {
                widgets[0].Number = fieldNumber;
            }
            else
            {
                widgets.ForEach(widget => widget.Number = next++);
            }
            stamps.ForEach(stamp => stamp.Number = next++);

            var objects = new List<PdfIndirectObject> { new() { Number = revision.RootObjectNumber, DictText = BuildCatalog(catalog, fieldNumber) } };
            foreach (var group in plan.Annotations.GroupBy(a => a.PageObjectNumber))
            {
                var page = PdfRevisionReader.GetObjectBody(revision, group.Key)
                    ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
                foreach (var annotation in group)
                {
                    page = PdfObjectWriter.AppendToArray(page, "Annots", annotation.Number)
                        ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
                }
                objects.Add(new PdfIndirectObject { Number = group.Key, DictText = page });
            }
            objects.AddRange(BuildSignatureField(widgets, fieldNumber, valueNumber));
            objects.AddRange(stamps.Select(stamp => new PdfIndirectObject { Number = stamp.Number, DictText = PdfObjectWriter.Write(BuildAnnotation(stamp, "Stamp")) }));
            objects.AddRange(plan.AppearanceObjects);
            objects.Add(new PdfIndirectObject { Number = valueNumber, DictText = BuildSignatureValue(context, signedAt) });

            return WriteRevision(pdf, revision, objects, next);
        }

        private PdfAnnotationPlan PlanAnnotations(PdfRevision revision, List<int> pageOrder, int pagesNumber, PdfSigningContextDto context,
            DateTime signedAt, int nextObjectNumber)
        {
            var options = context.Options;
            var plan = new PdfAnnotationPlan { NextObjectNumber = nextObjectNumber };
            var signedAtText = signedAt.ToString(PdfSigningConst.APPEARANCE_TIME_FORMAT, CultureInfo.InvariantCulture);

            foreach (var position in options.Positions)
            {
                var image = position.Kind switch
                {
                    TemplatePositionConst.STAMP => options.StampImage,
                    TemplatePositionConst.WET_SIGNATURE => options.WetSignatureImage,
                    _ => null,
                };
                var isText = position.Kind == TemplatePositionConst.DIGITAL_SIGNATURE;
                if (isText ? !options.IsShowDigitalSignature : image == null || image.Length == 0)
                {
                    continue;
                }

                var pageIndex = Math.Clamp(position.PageNumber - 1, 0, pageOrder.Count - 1);
                var pageNumber = pageOrder[pageIndex];
                var page = PdfRevisionReader.GetObjectBody(revision, pageNumber)
                    ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
                var pageBox = ReadPageBox(revision, page, pagesNumber);
                var fontSize = options.DigitalSignatureFontSize * Math.Min(pageBox.Width / PdfSigningConst.REFERENCE_PAGE_WIDTH, pageBox.Height / PdfSigningConst.REFERENCE_PAGE_HEIGHT);
                var rect = ToPoints(position, pageBox);
                if (isText)
                {
                    rect = FitText(rect, _appearanceBuilder.MeasureText(context.SignerName, signedAtText, options.DigitalSignatureFont, fontSize), pageBox);
                }
                else
                {
                    rect = position.Kind == TemplatePositionConst.STAMP ? FitStamp(rect, image!) : FitWetSignature(rect, pageBox, image!);
                }
                rect = ClampToPage(rect, pageBox);
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    continue;
                }

                var appearance = isText
                    ? _appearanceBuilder.BuildText(context.SignerName, signedAtText, options.DigitalSignatureColor, options.DigitalSignatureFont, fontSize,
                        plan.NextObjectNumber, rect.Width, rect.Height)
                    : BuildImageAppearance(context, position.Kind, image!, plan.NextObjectNumber, rect);
                plan.AppearanceObjects.AddRange(appearance.Objects);
                plan.NextObjectNumber = appearance.Objects.Max(o => o.Number) + 1;
                plan.Annotations.Add(new PdfAnnotation
                {
                    PageObjectNumber = pageNumber,
                    Rect = rect,
                    AppearanceObjectNumber = appearance.FormObjectNumber,
                    IsSignature = isText || options.IsDigitalSignatureInImage,
                });
            }

            if (!plan.Annotations.Any(a => a.IsSignature))
            {
                plan.Annotations.Add(new PdfAnnotation { PageObjectNumber = pageOrder[0], IsSignature = true });
            }
            return plan;
        }

        private PdfAppearance BuildImageAppearance(PdfSigningContextDto context, int kind, byte[] image, int firstObjectNumber, PdfRect rect)
        {
            var options = context.Options;
            var isStamp = kind == TemplatePositionConst.STAMP;
            var cacheKey = string.Create(CultureInfo.InvariantCulture, $"{kind}|{rect.Width:0.###}|{rect.Height:0.###}");
            var drawn = context.AppearanceCache.GetOrAdd(cacheKey, _ => _appearanceBuilder.DrawImage(image,
                isStamp ? TemplateConst.MIN_STROKE_WIDTH : options.WetSignatureStrokeWidth, rect.Width, rect.Height));
            return _appearanceBuilder.BuildImage(drawn, isStamp ? options.StampIntensity : options.WetSignatureIntensity,
                isStamp ? null : options.WetSignatureColor, firstObjectNumber, rect.Width, rect.Height);
        }

        private static List<PdfIndirectObject> BuildSignatureField(List<PdfAnnotation> widgets, int fieldNumber, int valueNumber)
        {
            var fieldName = new PdfString(PdfSigningConst.SIGNATURE_FIELD_PREFIX + fieldNumber);
            if (widgets.Count == 1)
            {
                var widget = BuildAnnotation(widgets[0], "Widget")
                    .Add("FT", new PdfName("Sig")).Add("Ff", 0).Add("T", fieldName).Add("V", new PdfRef(valueNumber));
                return [new PdfIndirectObject { Number = fieldNumber, DictText = PdfObjectWriter.Write(widget) }];
            }

            var field = new PdfDict()
                .Add("FT", new PdfName("Sig")).Add("Ff", 0).Add("T", fieldName).Add("V", new PdfRef(valueNumber))
                .Add("Kids", widgets.Select(w => new PdfRef(w.Number)).ToArray());
            var result = new List<PdfIndirectObject> { new() { Number = fieldNumber, DictText = PdfObjectWriter.Write(field) } };
            result.AddRange(widgets.Select(w => new PdfIndirectObject
            {
                Number = w.Number,
                DictText = PdfObjectWriter.Write(BuildAnnotation(w, "Widget").Add("Parent", new PdfRef(fieldNumber))),
            }));
            return result;
        }

        private static PdfDict BuildAnnotation(PdfAnnotation annotation, string subtype)
        {
            var dict = new PdfDict()
                .Add("Type", new PdfName("Annot"))
                .Add("Subtype", new PdfName(subtype))
                .Add("P", new PdfRef(annotation.PageObjectNumber))
                .Add("F", 4)
                .Add("Rect", new object[] { annotation.Rect.X, annotation.Rect.Y, annotation.Rect.Right, annotation.Rect.Top });
            if (annotation.AppearanceObjectNumber is int form)
            {
                dict.Add("AP", new PdfDict().Add("N", new PdfRef(form)));
            }
            return dict;
        }

        private static string BuildSignatureValue(PdfSigningContextDto context, DateTime signedAt)
        {
            var builder = new StringBuilder("<</Type /Sig/Filter /Adobe.PPKLite/SubFilter /").Append(PdfSigningConst.SUB_FILTER);
            builder.Append("/Name ");
            PdfObjectWriter.WriteString(builder, context.SignerName);
            builder.Append("/Reason ");
            PdfObjectWriter.WriteString(builder, context.Options.SigningReason ?? PdfSigningConst.DEFAULT_REASON);
            builder.Append("/Location ");
            PdfObjectWriter.WriteString(builder, context.Options.SigningLocation ?? PdfSigningConst.DEFAULT_LOCATION);
            builder.Append("/M (D:")
                .Append(signedAt.ToString(PdfSigningConst.PDF_DATE_FORMAT, CultureInfo.InvariantCulture))
                .Append('+').Append(PdfSigningConst.VIETNAM_UTC_OFFSET_HOURS.ToString("00", CultureInfo.InvariantCulture)).Append("'00')");
            builder.Append(BuildByteRangePlaceholder());
            builder.Append("/Contents <").Append('0', PdfSigningConst.SIGNATURE_PLACEHOLDER_BYTES * 2).Append(">>>");
            return builder.ToString();
        }

        private static string BuildByteRangePlaceholder()
        {
            return "/ByteRange [" + string.Join(" ", Enumerable.Repeat(new string(' ', PdfSigningConst.BYTE_RANGE_FIELD_WIDTH), 4)) + "]";
        }

        private static PdfPrepared WriteRevision(byte[] pdf, PdfRevision revision, List<PdfIndirectObject> objects, int xrefNumber)
        {
            var builder = new StringBuilder();
            var offsets = new Dictionary<int, long>();
            var baseLength = pdf.Length;
            if (pdf[^1] != '\n' && pdf[^1] != '\r')
            {
                builder.Append('\n');
            }

            foreach (var obj in objects)
            {
                offsets[obj.Number] = baseLength + builder.Length;
                builder.Append(obj.Number.ToString(CultureInfo.InvariantCulture)).Append(" 0 obj\n").Append(obj.DictText);
                if (obj.StreamData != null)
                {
                    builder.Append("\nstream\n").Append(Encoding.Latin1.GetString(obj.StreamData)).Append("\nendstream");
                }
                builder.Append("\nendobj\n");
            }

            var xrefOffset = baseLength + builder.Length;
            if (revision.LastXrefIsStream)
            {
                offsets[xrefNumber] = xrefOffset;
            }
            var newSize = Math.Max(revision.Size, offsets.Keys.Max() + 1);
            builder.Append(revision.LastXrefIsStream
                ? BuildXrefStream(revision, xrefNumber, offsets, newSize)
                : BuildXrefTable(revision, offsets, newSize, xrefOffset));

            var appended = Encoding.Latin1.GetBytes(builder.ToString());
            var output = new byte[baseLength + appended.Length];
            Buffer.BlockCopy(pdf, 0, output, 0, baseLength);
            Buffer.BlockCopy(appended, 0, output, baseLength, appended.Length);
            return PatchByteRange(output, baseLength);
        }

        private static PdfPrepared PatchByteRange(byte[] output, int baseLength)
        {
            var text = Encoding.Latin1.GetString(output);
            var contentsAt = text.LastIndexOf("/Contents <", StringComparison.Ordinal);
            var byteRangeAt = text.LastIndexOf("/ByteRange [", StringComparison.Ordinal);
            if (contentsAt < baseLength || byteRangeAt < baseLength)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var hexLength = PdfSigningConst.SIGNATURE_PLACEHOLDER_BYTES * 2;
            var gapStart = contentsAt + "/Contents ".Length;
            var gapEnd = gapStart + hexLength + 2;
            var ranges = new long[] { 0, gapStart, gapEnd, output.Length - gapEnd };
            var byteRange = "/ByteRange [" + string.Join(" ", ranges.Select(v => v.ToString(CultureInfo.InvariantCulture).PadRight(PdfSigningConst.BYTE_RANGE_FIELD_WIDTH))) + "]";
            if (byteRange.Length != BuildByteRangePlaceholder().Length)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }
            Encoding.Latin1.GetBytes(byteRange).CopyTo(output, byteRangeAt);

            var signedContent = new byte[ranges[1] + ranges[3]];
            Buffer.BlockCopy(output, 0, signedContent, 0, (int)ranges[1]);
            Buffer.BlockCopy(output, (int)ranges[2], signedContent, (int)ranges[1], (int)ranges[3]);
            return new PdfPrepared
            {
                Bytes = output,
                SignedContent = signedContent,
                ContentsHexStart = gapStart + 1,
                ContentsHexLength = hexLength,
            };
        }

        private static List<int> ReadPageOrder(PdfRevision revision, int pagesNumber)
        {
            var order = new List<int>();
            var stack = new Stack<int>();
            var visited = new HashSet<int>();
            stack.Push(pagesNumber);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var body = visited.Add(node) ? PdfRevisionReader.GetObjectBody(revision, node) : null;
                if (body == null)
                {
                    continue;
                }

                var kids = Regex.Match(body, @"/Kids\s*\[(.*?)\]", RegexOptions.Singleline);
                if (!kids.Success)
                {
                    order.Add(node);
                    continue;
                }

                var children = Regex.Matches(kids.Groups[1].Value, @"(\d+)\s+\d+\s+R").Select(m => int.Parse(m.Groups[1].Value)).ToList();
                for (var i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i]);
                }
            }
            return order;
        }

        private static PdfRect ReadPageBox(PdfRevision revision, string page, int pagesNumber)
        {
            var match = Regex.Match(page, @"/MediaBox\s*\[([^\]]*)\]");
            if (!match.Success)
            {
                match = Regex.Match(PdfRevisionReader.GetObjectBody(revision, pagesNumber) ?? String.Empty, @"/MediaBox\s*\[([^\]]*)\]");
            }

            var box = match.Success
                ? match.Groups[1].Value.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray()
                : [];
            if (box.Length != 4)
            {
                box = [0d, 0d, PdfSigningConst.REFERENCE_PAGE_WIDTH, PdfSigningConst.REFERENCE_PAGE_HEIGHT];
            }

            return new PdfRect
            {
                X = Math.Min(box[0], box[2]),
                Y = Math.Min(box[1], box[3]),
                Width = Math.Abs(box[2] - box[0]),
                Height = Math.Abs(box[3] - box[1]),
            };
        }

        private static PdfRect ToPoints(PdfSigningPositionDto position, PdfRect pageBox)
        {
            var height = position.HeightRatio * pageBox.Height;
            return new PdfRect
            {
                X = pageBox.X + position.XRatio * pageBox.Width,
                Y = pageBox.Y + pageBox.Height - position.YRatio * pageBox.Height - height,
                Width = position.WidthRatio * pageBox.Width,
                Height = height,
            };
        }

        private static PdfRect FitText(PdfRect rect, PdfRect textSize, PdfRect pageBox)
        {
            var alignRight = rect.X + rect.Width / 2 > pageBox.X + pageBox.Width / 2;
            return new PdfRect
            {
                X = alignRight ? rect.Right - textSize.Width : rect.X,
                Y = rect.Top - textSize.Height,
                Width = textSize.Width,
                Height = textSize.Height,
            };
        }

        private static PdfRect FitStamp(PdfRect rect, byte[] image)
        {
            var size = ImageSizeReader.Read(image);
            var width = PdfSigningConst.STAMP_WIDTH_MM / PdfSigningConst.MM_PER_INCH * PdfSigningConst.POINTS_PER_INCH;
            return CenterIn(rect, width, width * size.HeightPoints / size.WidthPoints);
        }

        private static PdfRect FitWetSignature(PdfRect rect, PdfRect pageBox, byte[] image)
        {
            var size = ImageSizeReader.Read(image);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return rect;
            }

            var ratio = size.WidthPoints / size.HeightPoints;
            var width = rect.Width;
            var height = width / ratio;
            if (height > rect.Height)
            {
                height = rect.Height;
                width = height * ratio;
            }

            var maxWidth = pageBox.Width * PdfSigningConst.MAX_WET_SIGNATURE_WIDTH_RATIO;
            if (width > maxWidth)
            {
                width = maxWidth;
                height = width / ratio;
            }
            return CenterIn(rect, width, height);
        }

        private static PdfRect CenterIn(PdfRect rect, double width, double height)
        {
            return new PdfRect { X = rect.X + (rect.Width - width) / 2, Y = rect.Y + (rect.Height - height) / 2, Width = width, Height = height };
        }

        private static PdfRect ClampToPage(PdfRect rect, PdfRect pageBox)
        {
            var x = rect.Width >= pageBox.Width
                ? pageBox.X + (pageBox.Width - rect.Width) / 2
                : Math.Clamp(rect.X, pageBox.X, pageBox.X + pageBox.Width - rect.Width);
            var y = rect.Height >= pageBox.Height
                ? pageBox.Y + (pageBox.Height - rect.Height) / 2
                : Math.Clamp(rect.Y, pageBox.Y, pageBox.Y + pageBox.Height - rect.Height);
            return new PdfRect { X = x, Y = y, Width = rect.Width, Height = rect.Height };
        }

        private static string BuildCatalog(string catalog, int fieldNumber)
        {
            var at = catalog.IndexOf("/AcroForm", StringComparison.Ordinal);
            if (at < 0)
            {
                var close = catalog.LastIndexOf(">>", StringComparison.Ordinal);
                if (close < 0)
                {
                    throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
                }
                return catalog[..close] + "/AcroForm<</Fields[" + fieldNumber + " 0 R]/SigFlags 3/DA(/Helv 0 Tf 0 g )>>" + catalog[close..];
            }

            var p = PdfRevisionReader.SkipWhiteSpace(catalog, at + "/AcroForm".Length);
            if (!PdfRevisionReader.IsDictionaryStart(catalog, p))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var acroForm = PdfRevisionReader.ExtractDictionary(catalog, p);
            var updated = PdfObjectWriter.AppendToArray(acroForm, "Fields", fieldNumber)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            if (!Regex.IsMatch(updated, @"/SigFlags\s+\d"))
            {
                var close = updated.LastIndexOf(">>", StringComparison.Ordinal);
                updated = updated[..close] + "/SigFlags 3" + updated[close..];
            }
            return catalog[..p] + updated + catalog[(p + acroForm.Length)..];
        }

        private static string BuildXrefStream(PdfRevision revision, int xrefNumber, Dictionary<int, long> offsets, int newSize)
        {
            var index = new List<int>();
            var data = new List<byte>();
            foreach (var (start, count) in GroupConsecutive(offsets.Keys))
            {
                index.Add(start);
                index.Add(count);
                for (var k = 0; k < count; k++)
                {
                    var offset = offsets[start + k];
                    data.AddRange([1, (byte)(offset >> 24 & 0xFF), (byte)(offset >> 16 & 0xFF), (byte)(offset >> 8 & 0xFF), (byte)(offset & 0xFF), 0, 0]);
                }
            }

            var builder = new StringBuilder();
            builder.Append(xrefNumber).Append(" 0 obj\n<</Type /XRef/Size ").Append(newSize)
                .Append("/Index [").Append(string.Join(" ", index)).Append("]/W [1 4 2]/Root ").Append(revision.RootObjectNumber).Append(" 0 R");
            AppendTrailerReferences(builder, revision);
            builder.Append("/Length ").Append(data.Count).Append(">>\nstream\n").Append(Encoding.Latin1.GetString(data.ToArray()))
                .Append("\nendstream\nendobj\nstartxref\n").Append(offsets[xrefNumber]).Append("\n%%EOF\n");
            return builder.ToString();
        }

        private static string BuildXrefTable(PdfRevision revision, Dictionary<int, long> offsets, int newSize, long xrefStart)
        {
            var builder = new StringBuilder("xref\n");
            foreach (var (start, count) in GroupConsecutive(offsets.Keys))
            {
                builder.Append(start).Append(' ').Append(count).Append('\n');
                for (var k = 0; k < count; k++)
                {
                    builder.Append(offsets[start + k].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n\r\n");
                }
            }

            builder.Append("trailer\n<</Size ").Append(newSize).Append("/Root ").Append(revision.RootObjectNumber).Append(" 0 R");
            AppendTrailerReferences(builder, revision);
            builder.Append(">>\nstartxref\n").Append(xrefStart).Append("\n%%EOF\n");
            return builder.ToString();
        }

        private static void AppendTrailerReferences(StringBuilder builder, PdfRevision revision)
        {
            if (revision.InfoRaw != null)
            {
                builder.Append("/Info ").Append(revision.InfoRaw);
            }
            if (revision.IdRaw != null)
            {
                builder.Append("/ID ").Append(revision.IdRaw);
            }
            builder.Append("/Prev ").Append(revision.LastXrefOffset);
        }

        private static IEnumerable<(int Start, int Count)> GroupConsecutive(IEnumerable<int> numbers)
        {
            var sorted = numbers.OrderBy(n => n).ToList();
            var i = 0;
            while (i < sorted.Count)
            {
                var count = 1;
                while (i + count < sorted.Count && sorted[i + count] == sorted[i] + count)
                {
                    count++;
                }
                yield return (sorted[i], count);
                i += count;
            }
        }
    }
}
