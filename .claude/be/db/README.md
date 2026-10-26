# Lệnh migration và cập nhật database

> Chạy ở thư mục gốc repo. Đổi schema phải hỏi trước (skill `task-workflow`). Chi tiết: [architecture/04-infrastructure-data.md](../architecture/04-infrastructure-data.md).

```bash
dotnet ef migrations add <Ten> --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api --output-dir Migrations/<Auth|Core|Construction|DigitalSignature|Packaging|Operation>
dotnet ef migrations has-pending-model-changes --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
dotnet ef database update --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
dotnet ef migrations remove --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
```

Thứ tự: `add` → xem lại file sinh ra → `database update` (dùng connection string `SO_HOA_DB` của `sh.be.api`); `remove` chỉ gỡ migration **chưa** update vào DB.

Cần `dotnet-ef` đúng bản EF của dự án: `dotnet tool install --global dotnet-ef --version 9.0.8`.

`Add_OperationModule` (`Migrations/Operation/`, Task 9) phải chạy **sau** `Add_QaAssignment` (bảng `SubmissionUser`): tạo schema `operation`,
7 bảng và đúng 1 index `IX_Submission_ProjectId_DocumentTypeId_Box_ProfileRange` trên `construction.Submission`, không thêm cột.
Hồ sơ cũ không có dòng `SubmissionUser` = chưa giao, không cần script dữ liệu.

## Dữ liệu test hồ sơ (DB dev)

`test-data-ho-so.sql` tạo 15 hồ sơ + 1 hồ sơ khác loại văn bản (dự án 3, loại văn bản 3/4), 5 trường (`Ghi chú` cố ý không cấu hình
cho dự án), user `tester` (role QA, được cấp tạm `Submission.View` + `Check` + `Qa`) và `viewer` (role User, không quyền), mật khẩu như
`admin`. Mọi dòng gắn `CreatedBy = 'TESTDATA'`; `test-data-ho-so-xoa.sql` xoá sạch, kể cả log kiểm tra, log QA và quyền cấp tạm.

```bash
sqlcmd -C -I -f 65001 -S 127.0.0.1,14330 -U sa -P <mật khẩu> -d SO_HOA_DB -i .claude/be/db/test-data-ho-so.sql
```

⚠️ Thiếu `-f 65001` thì `sqlcmd` đọc file theo bảng mã ANSI, tiếng Việt lưu vào DB bị lỗi font.
⚠️ Vừa `migrations add` xong mà chạy `database update` / `has-pending-model-changes` với `--no-build` thì DLL chưa có migration mới, EF báo "pending changes" sai. Build lại trước rồi mới dùng `--no-build`.
