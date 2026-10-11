---
name: task-workflow
description: Cách chạy một việc nhiều bước trong repo ĐATN (sh.be + sh.fe) — nạp tài liệu kiến trúc trong .claude trước, gom TẤT CẢ câu hỏi vào MỘT lượt, giữ một task list hiện rõ và tick từng việc khi xong, chốt bằng build sạch rồi cập nhật tài liệu. Áp dụng cho phiên chính và cho mọi agent được sinh ra. Dùng ở đầu mọi việc không tầm thường.
---

# Skill: Khuôn chạy việc (ĐATN)

Áp dụng cho phiên chính và **mọi** agent được sinh ra. Bốn pha: nạp tri thức → hỏi một lượt → theo dõi hiện rõ
→ chốt.

## Pha 0 — Nạp tri thức trước khi động vào code

Tri thức dự án nằm trong `.claude/` (tiếng Việt), **không** suy ra lại từ code mỗi lần:

1. Phần backend `sh.be` ⇒ [`.claude/be/architecture/`](../../be/architecture/README.md): đọc `README.md` trước,
   rồi đúng file của tầng định sửa. Sửa code bất kỳ ⇒ đọc thêm
   [`07-conventions.md`](../../be/architecture/07-conventions.md).
2. Thêm chức năng hoặc module ⇒ [`08-them-chuc-nang.md`](../../be/architecture/08-them-chuc-nang.md).
3. Phần frontend `sh.fe` ⇒ [`.claude/fe/architecture/`](../../fe/architecture/README.md), quy ước ở
   [`05-conventions.md`](../../fe/architecture/05-conventions.md). Việc còn dở ở `.claude/fe/plans/` — đọc
   [`khop-voi-be.plan.md`](../../fe/plans/khop-voi-be.plan.md) trước khi làm màn hình gọi API.

## Pha 1 — Hỏi đúng một lượt

- Trước khi bắt tay, gom **TẤT CẢ** câu hỏi còn treo rồi hỏi **cùng một lượt** (một lời gọi `AskUserQuestion`,
  tối đa 4 câu).
- **Không** nhỏ giọt mỗi lượt một câu.
- Chỉ hỏi thứ thực sự làm đổi việc mình sẽ làm. Cái nào có mặc định hợp lý thì **tự chọn, nói ra, rồi đi tiếp**.
- Không có gì chặn thì bỏ qua pha này, vào Pha 2 luôn.

Bốn thứ **phải hỏi**, không được tự quyết:

| Thay đổi | Vì sao phải hỏi |
|---|---|
| Schema database (entity, `ShDbContext`, migration mới) | Đổi DB đang chạy, khó lùi |
| Route, envelope `ApiResponse`, tên trường DTO mà FE đang dùng | Gãy `sh.fe` |
| Key quyền trong `PermissionConstants`, tên role trong `RoleConstants` | Seeder đồng bộ lại quyền, role đang gán bị ảnh hưởng |
| Đổi chiều tham chiếu giữa các project `.csproj` | Phá dependency rule của Clean Architecture |

## Pha 2 — Theo dõi việc hiện rõ

- Chẻ việc thành các bước cụ thể và tạo **task list hiện rõ** để người dùng thấy kế hoạch.
- Đúng **một** việc `in_progress` mỗi lúc; đặt `in_progress` khi bắt đầu, đặt `completed` ngay khi xong.
- Giữ danh sách đúng thực tế — thêm bước phát hiện giữa đường, xoá bước đã vô nghĩa.
- Tick **khi làm xong từng việc**, không dồn tick một lượt ở cuối.
- **Chỉ sửa chỗ cần thiết**: không tự ý đổi tên, dọn code, hay "tiện tay" refactor ngoài yêu cầu.

## Pha 3 — Chốt công việc

| Sửa gì | Chốt bằng |
|---|---|
| `sh.be` | `dotnet build sh.be/sh.be.api/sh.be.api.sln` — 0 lỗi, không thêm warning mới |
| Entity, `ShDbContext`, cấu hình EF | Thêm `dotnet ef migrations has-pending-model-changes --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api` — phải là "No changes" trừ khi đã được duyệt đổi schema |
| `sh.fe` | `npm run build` trong `sh.fe/` (app admin: `npm run build:admin`) |

Rồi trong **cùng task đó**:

1. Sửa tài liệu nào vừa lệch khỏi code — xem [../write-markdown/SKILL.md](../write-markdown/SKILL.md).
2. Nói thẳng phần nào **chưa** làm hoặc **chưa** kiểm được (ví dụ API ghi chưa gọi thử) và vì sao.
3. **Không commit** khi người dùng chưa yêu cầu.

## Gặp bẫy thì ghi lại

Bẫy phát hiện trong lúc làm hoặc lúc review được ghi thành một dòng `⚠️` **ngay trong tài liệu sở hữu chỗ đó**
(không có thư mục bug log riêng): hiện tượng → nguyên nhân gốc → cách tránh. Khuôn ở
[../write-markdown/SKILL.md](../write-markdown/SKILL.md).

## Khi giao việc cho agent

Nhét vào prompt của agent yêu cầu chạy theo **đúng khuôn này**:

1. đọc `.claude/be/architecture/README.md` + file của tầng liên quan trước khi sửa gì,
2. gom mọi câu hỏi trả về **một lượt**, không nhỏ giọt,
3. giữ và báo lại **checklist** việc con, tick từng cái,
4. **ghi bẫy** vào đúng tài liệu sở hữu chỗ đó,
5. chốt bằng build sạch của đúng phần mình sửa, không commit.

## Checklist nhanh

1. Đã đọc tài liệu kiến trúc của phần định sửa?
2. Đã gom và hỏi hết câu hỏi trong một lượt (hoặc xác nhận không cần hỏi)?
3. Có task list hiện rõ, đúng một việc `in_progress`, tick khi xong từng việc?
4. Build sạch, EF không có thay đổi ngoài ý muốn, tài liệu đã cập nhật cùng task?
5. Gặp bẫy nào đã ghi `⚠️` vào đúng tài liệu chưa?
