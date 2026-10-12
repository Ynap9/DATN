# Loại văn bản (Construction)

> **Phần 5/5** · trước: [04-nhat-ky-he-thong.md](04-nhat-ky-he-thong.md) · mục lục: [README.md](README.md)

Danh mục loại văn bản, bảng `construction.DocumentType`. Entity `DocumentType` có `Id`, `Name`, `DefaultView`
(mặc định `1`), cột tạo / sửa và xoá mềm.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Danh sách có phân trang | `GET api/construction/document-type/paging` | `Function.Construction.DocumentType.View` |
| Xem chi tiết | `GET api/construction/document-type/{id}` | `Function.Construction.DocumentType.View` |
| Tạo mới | `POST api/construction/document-type` | `Function.Construction.DocumentType.Create` |
| Cập nhật | `PUT api/construction/document-type/{id}` | `Function.Construction.DocumentType.Update` |
| Xoá | `DELETE api/construction/document-type/{id}` | `Function.Construction.DocumentType.Delete` |

Body tạo / sửa: `{ "name": "...", "defaultView": 1 }`. Bỏ `defaultView` khi tạo thì nhận `1`.

## Luật nghiệp vụ

- `Name` **duy nhất** trong các bản ghi chưa xoá (`DocumentTypeDomainService.EnsureUniqueNameAsync`) → trùng trả `409`
  "Tên loại văn bản đã tồn tại". Khi sửa, bản ghi đang sửa không tính là trùng với chính nó.
- So khớp tên **không phân biệt hoa thường** (theo collation mặc định của SQL Server).
- Xoá là **xoá mềm**: bản ghi biến mất khỏi danh sách và chi tiết (lỗi `301`), tên được dùng lại.
- Danh sách lọc `keyword` theo `Name`, sắp mới tạo trước; `pageSize = -1` lấy tất cả.
- Mỗi thao tác ghi để lại audit log `Tạo / Cập nhật / Xóa loại văn bản` kèm giá trị cũ và mới.

## Đã kiểm thực tế

Ngày 2026-10-12, gọi API thật bằng tài khoản `admin`: tạo, tạo trùng (`409`), phân trang, xem chi tiết, sửa
`defaultView = 0` (lưu đúng `0`), xoá, xem lại sau xoá (`301`), gọi không token (`401`), Swagger nhóm
`construction` (`200`), seeder thêm đủ 6 quyền, audit log ghi đủ 3 thao tác.

## Giới hạn

⚠️ Chưa có unique index trong DB cho `Name`; hai request tạo cùng tên gần như đồng thời vẫn có thể lọt. Muốn chặn hẳn
cần index có lọc `Deleted = 0` — là đổi schema, hỏi trước.

⚠️ Chưa kiểm `Name` rỗng; `POST` với `name` rỗng hiện vẫn tạo được bản ghi.

> **Hết** · mục lục: [README.md](README.md)
