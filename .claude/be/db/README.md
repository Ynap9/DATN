# Lệnh migration và cập nhật database

> Chạy ở thư mục gốc repo. Đổi schema phải hỏi trước (skill `task-workflow`). Chi tiết: [architecture/04-infrastructure-data.md](../architecture/04-infrastructure-data.md).

```bash
dotnet ef migrations add <Ten> --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api --output-dir Migrations/<Auth|Core|Construction>
dotnet ef migrations has-pending-model-changes --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
dotnet ef database update --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
dotnet ef migrations remove --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
```

Thứ tự: `add` → xem lại file sinh ra → `database update` (dùng connection string `SO_HOA_DB` của `sh.be.api`); `remove` chỉ gỡ migration **chưa** update vào DB.

Cần `dotnet-ef` đúng bản EF của dự án: `dotnet tool install --global dotnet-ef --version 9.0.8`.
