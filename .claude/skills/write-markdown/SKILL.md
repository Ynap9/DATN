---
name: write-markdown
description: Quy ước viết và sửa mọi file Markdown (.md) của repo ĐATN — tài liệu kiến trúc trong .claude/be và .claude/fe, plan, báo cáo, README, memory. Đọc TRƯỚC khi tạo hoặc sửa một file .md để nó khớp ngôn ngữ, bố cục, độ dài và cách bảo trì của dự án. Áp dụng cho phiên chính và cho mọi agent được sinh ra.
---

# Skill: Viết Markdown (ĐATN)

Đọc trước khi tạo hoặc sửa bất kỳ `.md` nào. Mục tiêu: mọi tài liệu đọc như **một hệ thống** — cùng ngôn
ngữ, cùng bố cục, cùng cách cập nhật.

## Ngôn ngữ

- **Toàn bộ tài liệu viết TIẾNG VIỆT** — đồ án và nghiệp vụ là tiếng Việt.
- Giữ nguyên tên định danh trong code: `IRoleRepository`, `PermissionGrantPolicy`, `ShDbContext`, tên route,
  tên hằng số, tên thư mục.
- Thuật ngữ kiến trúc giữ tiếng Anh khi đó là tên chuẩn (aggregate, repository, dependency rule), giải thích
  bằng tiếng Việt ở lần đầu xuất hiện.
- Sửa file đã có thì **theo ngôn ngữ của file đó**, không đổi giữa dòng.
- ⚠️ **Code trong repo không thêm comment** (luật của người dùng). Chú thích `//` bên trong khối code của tài
  liệu là lời giảng cho người đọc nên được phép và viết tiếng Việt.

## Độ dài — giữ file nhỏ

| Loại file | Trần |
|---|---|
| `architecture/`, `plans/` | **100 dòng** |
| Tài liệu chủ đề khác trong `.claude/` | **180 dòng** |

Vượt trần thì xử theo mức vượt:

| Vượt | Việc phải làm |
|---|---|
| **≤ 10 dòng** | **KHÔNG tách.** Nén lại: siết câu, gộp gạch đầu dòng hoặc dòng bảng gần nhau. |
| **> 10 dòng** | **TÁCH thành nhiều file**: một `README.md` làm mục lục + các file đánh số. |

Một chủ đề một file; liên kết file liên quan bằng đường dẫn tương đối.

### Tách thì các phần PHẢI còn nối với nhau

Mỗi bộ file tách ra cần **cả hai**:

1. **Mục lục** — `README.md` liệt kê các phần theo thứ tự đọc, mỗi phần một dòng nêu nội dung.
2. **Con trỏ** ở đầu và cuối mỗi phần:

```markdown
> **Phần 2/5** · trước: [01-ten-phan-truoc.md] · mục lục: [README.md]      ← đặt ngay dưới H1
> **Tiếp:** [03-ten-phan-sau.md] — một câu nêu nội dung phần sau.          ← dòng cuối file
```

Đừng để lại một phần đọc như thể nó là cả tài liệu — đó là cách một agent thi công nửa bản kế hoạch.

## Bố cục

- Đúng **một** `#` H1; ngay dưới là con trỏ (nếu thuộc bộ tách) hoặc một dòng `>` nêu mục đích kèm ngày cập nhật.
- `##` H2 cho từng mục. Dùng **bảng** cho danh sách thư mục, interface, endpoint, mã lỗi.
- Khối code **luôn** khai ngôn ngữ (```csharp / ```typescript / ```bash / ```text / ```jsonc).
- GitHub-flavored Markdown; liên kết nội bộ luôn là **đường dẫn tương đối** tới file `.md`.
- Ghi **vì sao** chứ không thuật lại code. Con số phải nói rõ **đo ở đâu ra**.

## Khuôn từng loại tài liệu

**Architecture** — `.claude/<be|fe>/architecture/`: README mục lục + file đánh số, mỗi file một tầng/project:
trách nhiệm, được tham chiếu gì, thư mục bên trong, quy tắc "được / không được", bẫy `⚠️`.

**Plan** — `.claude/<be|fe>/plans/<ten-viec>.plan.md`, ≤100 dòng:

- **Trạng thái** ở đầu file (`✅ đã thi công` / `🔶 làm dở` / `📝 chờ duyệt` / `🔬 nghiên cứu`).
- **Input** — cái đã có, điều kiện tiên quyết.
- **Steps** — theo thứ tự, mỗi bước tự kiểm được; bước cuối là build (xem skill `task-workflow`, Pha 3).
- **Expected output** — kết quả cụ thể. **Điểm cần chú ý** — mỗi đánh đổi không hiển nhiên một dòng.

**Tài liệu chủ đề** — quyết định đã chốt + vì sao, giới hạn đã biết. Phần **chưa thi công** gom xuống cuối file
dưới nhãn 🔬, không trộn vào phần đang chạy.

## Ghi lại bẫy đã sập

**Không có thư mục bug log riêng**: bẫy ghi thành một dòng `⚠️` **ngay trong tài liệu sở hữu chỗ đó**, cạnh
phần mô tả cơ chế. Khuôn: hiện tượng → nguyên nhân gốc → cách tránh, và nêu rõ **đừng "sửa" theo hướng nào**.

```markdown
⚠️ Bẫy: `ExecuteDeleteAsync` cũng bị global query filter lọc, nên bản ghi đã xoá mềm còn nằm lại và lần
insert sau trùng khoá ghép. Xoá cứng theo khoá ghép phải gọi `IgnoreQueryFilters()` trước.
```

Bẫy lớn tới mức cần cả một mục thì mở `##` riêng trong đúng file đó, đừng mở file mới.

## Bảo trì

- Code đổi lệch khỏi thứ một tài liệu đang mô tả ⇒ **sửa tài liệu đó trong CÙNG task**, không hẹn lần sau.
- Xong một plan thì cập nhật trạng thái ở đầu file plan.
- Memory dự án: một fact một file, thêm **đúng một dòng** trỏ vào `MEMORY.md`, không nhồi nội dung vào index.

## Trước khi lưu — checklist

1. Tiếng Việt (hoặc theo đúng file đang sửa)? Trong trần dòng — vượt ≤10 thì nén, vượt >10 thì tách kèm mục lục
   và con trỏ tiếp/trước?
2. Một H1, các H2 rõ, khối code có khai ngôn ngữ, link tương đối còn đúng?
3. Plan có Input · Steps (bước build cuối) · Expected output · Điểm cần chú ý?
4. Tài liệu nào khác đang mô tả cùng chỗ này cần sửa theo?
5. Phát hiện bẫy nào thì đã ghi `⚠️` vào đúng tài liệu sở hữu chỗ đó chưa?
