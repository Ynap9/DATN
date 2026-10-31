# Workspace và build

> **Phần 1/6** · mục lục: [README.md](README.md)

## angular.json

| Project | Loại | Root | Ghi chú |
|---|---|---|---|
| `admin` | application | `projects/admin` | Builder `@angular/build:application`, prefix `app`, style `scss` |
| `lib-components` | library | `projects/lib-components` | UI kit `@sh/ui`, entry `src/public-api.ts`; **không build riêng**, admin import thẳng mã nguồn |

`projects/shared/` không còn là project Angular, chỉ giữ `assets/` (SCSS layout, ảnh, font).

Build `admin`:

- copy `projects/shared/assets` ra `shared/assets` (đường dẫn ảnh trong `BaseConsts` dùng tiền tố này);
- `stylePreprocessorOptions.includePaths`: `projects/shared/assets`, `projects/shared/assets/layout`;
- style: `admin/src/tailwind.css` rồi `admin/src/styles.scss` (import `layout.scss`, `demo.scss`, `primeicons`);
- production thay `admin/src/environments/environment.ts` bằng `environment.prod.ts`, staging bằng `environment.staging.ts`;
- budget production: initial cảnh báo 2 MB / lỗi 3 MB; style mỗi component cảnh báo 8 kB / lỗi 16 kB.

⚠️ `ng build lib-components` không chạy được: `ng-packagr` chưa cài. Không cần cài — lib chỉ được dùng qua alias.

## Alias import (`tsconfig.json`)

| Alias | Trỏ tới | Dùng cho |
|---|---|---|
| `@sh/ui` | `projects/lib-components/src/public-api.ts` | Component `sh-*`, pipe, validator, `Page`, `Utils`, hằng UI |
| `@/*` | `projects/admin/src/app/*` | `@/core/http/http-context`, `@/shared/base/base.component`, `@/services/role.service` |
| `@env/*` | `projects/admin/src/environments/*` | `@env/environment` |

```typescript
import { ShTableComponent, Page } from '@sh/ui';
import { BaseComponent } from '@/shared/base/base.component';
import { environment } from '@env/environment';
```

Trong cùng một tầng (cùng tính năng, cùng thư mục `core/auth`…) vẫn import tương đối `./`, `../`.
⚠️ Bên trong `lib-components` **không** dùng alias nào và không import `public-api` của chính nó (dễ vòng import);
import trực tiếp file, ví dụ `'../../shared/models/page'`.

## Environment

| File | `api` | Dùng khi |
|---|---|---|
| `admin/src/environments/environment.ts` | `http://localhost:5265` | Mặc định, dev |
| `admin/src/environments/environment.prod.ts` | `''` (cùng origin) | `fileReplacements` của cấu hình production |
| `admin/src/environments/environment.staging.ts` | `''` (cùng origin) | `npm run build:staging` |
| `app/shared/models/environment.models.ts` | — | Interface `IEnvironment` |

⚠️ `IEnvironment` phải ở file riêng: bản prod thay chỗ `environment.ts`, nếu prod import `./environment` thì nó tự
import chính nó và build production lỗi `TS2724`.

## Lệnh

```bash
npm start                 # ng serve (project mặc định: admin)
npm run build:admin       # ng build admin, cấu hình production
npx ng build admin --configuration development
```

Đo ngày 2026-10-28 trên máy dev, sau khi dựng lại kiến trúc:

| Lệnh | Initial | Trước đó |
|---|---|---|
| `ng build admin --configuration development` | 2,16 MB | 2,33 MB (standalone), 5,95 MB (NgModule) |
| `npm run build:admin` | 720,58 kB, transfer ≈ 154 kB | 791,95 kB |

Đợt này bỏ `LibHelperService`, `ShComponentBase` cũ, pipe `imageResize` cùng gói `crypto-js`; chưa đo riêng từng
phần đóng góp bao nhiêu. Còn 1 cảnh báo `moment` không phải ESM — từ `utils.ts`.

> **Tiếp:** [02-app-admin.md](02-app-admin.md) — khung ứng dụng admin và ba tầng `core` / `shared` / `features`.
