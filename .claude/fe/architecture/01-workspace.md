# Workspace và build

> **Phần 1/6** · mục lục: [README.md](README.md)

## angular.json

| Project | Loại | Root | Ghi chú |
|---|---|---|---|
| `admin` | application | `projects/admin` | Builder `@angular/build:application`, prefix `app`, style `scss` |
| `shared` | library | `projects/shared` | ng-packagr; `src/` hiện chỉ có component mẫu `lib-shared` |
| `lib-components` | library | `projects/lib-components` | ng-packagr; entry `src/public-api.ts` |

Schematics của `admin` đặt `standalone: false` cho component, directive, pipe — `ng generate` sinh đúng kiểu
NgModule.

Build `admin`:

- copy `projects/shared/assets` ra `shared/assets` (đường dẫn ảnh trong `BaseConsts` dùng tiền tố này);
- `stylePreprocessorOptions.includePaths`: `projects/shared/assets`, `projects/shared/assets/layout`;
- style: `admin/src/tailwind.css` rồi `admin/src/styles.scss` (import `layout.scss`, `demo.scss`, `primeicons`);
- budget production: initial cảnh báo 2 MB / lỗi 3 MB; style mỗi component cảnh báo 8 kB / lỗi 16 kB.

## Import giữa các project

Admin import thư viện bằng **đường dẫn tương đối**, không qua alias:

```typescript
import { MyLibComponentsModule } from '../../../../lib-components/src/public-api';
import { AuthGuard } from './../../../shared/guard/auth.guard';
```

⚠️ `tsconfig.json` khai alias `@lib-components/*` → `./src/lib-components/*`, nhưng thư mục đó **không tồn
tại**; alias `lib-components` / `shared` trỏ vào `dist/` (chỉ có sau khi build thư viện). Đừng dùng các alias này
cho tới khi sửa `paths` (đã ghi trong [../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md)).

## Environment

| File | `api` | Dùng khi |
|---|---|---|
| `shared/environments/environment.ts` | `http://localhost:5265` | Mặc định, dev |
| `environment.dev.ts` / `environment.stag.ts` | `http://localhost:5265` / `''` | Chưa gắn vào configuration nào |
| `environment.prod.ts` | `''` (cùng origin) | `fileReplacements` của cấu hình production |

`localhost:5265` khớp cổng `sh.be.api` khi chạy `dotnet run` ở máy dev.

## Lệnh

```bash
npm start                 # ng serve (project mặc định: admin)
npm run start:admin       # ng serve admin
npm run build:admin       # ng build admin, cấu hình production
npx ng build admin --configuration development
```

Đo ngày 2026-10-11: `ng build admin --configuration development` thành công sau 15,6 giây; initial 5,92 MB, lazy
`auth-module` 4,22 kB, `admin-module` 1,87 kB.

> **Tiếp:** [02-app-admin.md](02-app-admin.md) — module, routing và layout của ứng dụng admin.
