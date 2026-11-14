# Đơn giá và thống kê tiền công (Operation)

> **Phần 24/26** · trước: [23-chia-phieu.md](23-chia-phieu.md) · mục lục: [README.md](README.md)

Task 12, chốt 2026-11-12. Đơn giá nằm trên dòng thành viên `ProjectMember` (`CompanyPrice`, `ActualPrice`), sửa đè, không lịch sử.
Thống kê khối lượng và tiền theo người trên một màn. Thay bảng `ProjectPriceDefault` và `ProjectMemberPrice` (đã xoá trong migration
`Update_ProjectMember_Price`).

**Vì sao:** Admin thoả thuận đơn giá với Leader (giá công ty); Leader thoả thuận với nhân viên (giá thực nhận). Phần chênh là hoa hồng
Leader, tính ngoài hệ thống, Leader không có lương trong hệ thống. Kiểm xong một văn bản là cộng tiền nhập liệu; QA chấm một lượt là cộng
tiền QA.

## Các case đã chốt

| # | Case | Chốt |
|---|---|---|
| 12.1 | Giá lưu ở đâu | 2 cột trên `ProjectMember`: `CompanyPrice`, `ActualPrice`. Sửa đè, không lịch sử |
| 12.2 | Giá áp cho ngày nào | Giá mới nhất áp cho mọi khối lượng, kể cả phần đã làm trước khi đổi giá |
| 12.3 | Ai đặt giá | Admin đặt cả hai. Leader chỉ sửa `ActualPrice` của nhân viên mình quản lý |
| 12.4 | Giá mặc định theo vai trò | Không có. Xoá `ProjectPriceDefault`, `ProjectMemberPrice` |
| 12.5 | Chưa đặt giá | Vẫn giao việc, vẫn làm. Tiền = 0, cờ `isPriceMissing`. Đặt giá sau thì tính cho phần đã làm |
| 12.6 | Leader | Không có giá, không có tiền trong hệ thống |
| 12.7 | Tiền nhập liệu (Data) | Số trường **tính tiền** có dữ liệu của văn bản × giá (Task 12B; paging / tiến trình của tôi cùng luật từ Task 30). Cộng khi văn bản được kiểm xong |
| 12.8 | QA trả, nhân viên sửa lại | Không cộng lần hai: một văn bản tính một lần cho người giữ |
| 12.9 | Văn bản về pool (trả lần 3) | Trừ tiền văn bản của người giữ cũ. Người mới kiểm xong thì người mới được |
| 12.10 | Admin / Leader kiểm hộ | Tiền tính cho nhân viên đang giữ văn bản |
| 12.11 | Tiền QA | Mỗi lượt chấm (Đạt và Không đạt) × giá |
| 12.12 | Dự án không QA (Task 28) | Không có tiền QA |
| 12.13 | Kỳ thanh toán | Theo dự án. Số liệu tính lại theo trạng thái hiện tại tới khi chốt |
| 12.14 | Doanh thu, quỹ KPI | Không làm trong task này |
| 12.15 | Cách tính | Query trực tiếp + cache khối lượng. Không bảng tính sẵn, không job |
| 12.16 | Màn thống kê | Một màn theo người: khối lượng, đơn giá, thành tiền. Phạm vi xem ở dưới |

## Quyết định kỹ thuật

- **K1.** "Trường có dữ liệu" = dòng `SubmissionData` chưa xoá, `Data` sau trim khác rỗng, thuộc trường chưa xoá có `IsChargeable = true`
  (Task 12B, như 5sdb). Cờ đọc theo hiện tại: đổi cờ thì tiền mọi khối lượng đã làm đổi theo. Văn bản đã xoá không tính.
- **K2.** Văn bản "kiểm xong bởi lượt giữ hiện tại" = có dòng `SubmissionUser` Data của người đó **và** ít nhất một `SubmissionLog`
  (bất kỳ ai kiểm) có `CreatedDate` ≥ `CreatedDate` của dòng giữ. Về pool (dòng bị xoá) → tự trừ. Nhận lại từ pool mà người mới chưa
  kiểm → chưa tính.
- **K3.** Đơn vị: Data = 1 trường tính tiền (`IsChargeable`, chưa xoá) có dữ liệu; QA = 1 lượt chấm (1 dòng `SubmissionQaLog` chưa xoá).
- **K4.** Dự án `DONE` / `EXPIRED`: không sửa giá nữa, kể cả Admin (`818`). Số tiền đứng yên. Chốt kỳ là việc riêng, không thuộc task này.
- **K5.** Cache `IMemoryCache` chỉ cho khối lượng, theo dự án, 60 giây. Tiền = khối lượng × giá đọc mới mỗi lần.
- **K6.** Leader đặt `ActualPrice` khi chưa có `CompanyPrice` → `808`. Admin được đặt `CompanyPrice` trước, `ActualPrice` để trống.
- **K7.** Sửa giá nhận danh sách 1–500 thành viên; lỗi một dòng → cả request lỗi, không dòng nào lưu.
- **K8.** Thành viên đã gỡ nhưng còn văn bản đã kiểm đang giữ vẫn có dòng (`isRemoved = true`) và được tính tiền theo giá trên dòng
  đó. Người chấm QA không phải thành viên Role QA của cặp (Admin) → không có dòng, không tiền. Mỗi (loại văn bản, người, vai trò) chỉ
  một dòng (Task 12B): có dòng chưa gỡ thì lấy dòng đó, gỡ hẳn thì lấy dòng đã gỡ `Id` lớn nhất — gỡ rồi thêm lại không nhân đôi tiền.
  Thêm lại cùng vai trò thì dòng mới chép `CompanyPrice` / `ActualPrice` từ dòng đã gỡ mới nhất ([21](21-thanh-vien-du-an.md)).

## Luật sửa giá — `PUT member/price`

Kiểm theo thứ tự, mỗi dòng:

| # | Luật | Sai → mã |
|---|---|---|
| 1 | `memberId` không trùng trong `items` | `400` |
| 2 | Thành viên thuộc dự án, chưa gỡ | `806` |
| 3 | Người gọi quản lý được cặp (`EnsureCanManageAsync`, `requireOpenProject = true`) | `805` / `818` |
| 4 | Thành viên không phải Role Leader | `400` "Leader không có đơn giá" |
| 5 | Leader: thành viên do mình quản lý | `821` |
| 6 | Leader không gửi `companyPrice` | `809` |
| 7 | Giá sau khi áp: `ActualPrice` có mà `CompanyPrice` chưa có (K6); có `CompanyPrice` là luôn kiểm `0 ≤ company ≤ 100.000.000`; `0 ≤ actual ≤ company` khi có cả hai (Task 12B) | `808` |

Dòng không đổi gì → bỏ qua, không tính vào `updatedCount`. Lưu một lần (một transaction), một audit `ACTION_MEMBER_PRICE_UPDATE` cho cả
request, `oldData` / `newData` là danh sách `{ MemberId, UserId, CompanyPrice, ActualPrice }` của các dòng đổi.

## Phạm vi xem

| Người gọi | Dòng thấy | Giá công ty, tiền công ty |
|---|---|---|
| Admin / SuperAdmin | Mọi thành viên Data / QA | Có |
| Leader đang hoạt động của cặp | Thành viên Data / QA có `ManagedByUserId` = mình | Có |
| Còn lại (kể cả Leader ở cặp mình là Data / QA) | Chỉ dòng của chính mình | `null` |

Phạm vi xét theo từng cặp (dự án, loại văn bản): một người có thể là Leader ở cặp này và nhân viên ở cặp khác.

## Công thức

- **Data** (theo loại văn bản, người): `documentCount` = số văn bản đã kiểm xong đang giữ (K2); `quantity` = `fieldCount` = tổng trường
  tính tiền có dữ liệu của các văn bản đó (K1). Task 30 (2026-11-13) thêm điều kiện `IsChargeable` (trước đó đếm mọi trường), đọc cờ
  **hiện tại** nên đổi cờ thì khối lượng văn bản đã kiểm đổi theo (sau tối đa 60 giây cache) — cùng luật `filledFieldCount` ở [25](25-xoa-du-an-danh-sach-cap.md).
- **QA** (theo loại văn bản, người): `reviewCount` = số lượt chấm; `passedCount`, `failedCount`; `quantity` = `reviewCount` (K3).
- `actualAmount` = `quantity` × `ActualPrice` (null coi là 0); `companyAmount` = `quantity` × `CompanyPrice`, `null` với người không thấy
  giá công ty.
- Thành viên chưa làm gì vẫn có dòng với `quantity` 0. Thành viên đã gỡ mà `quantity` 0 thì bỏ.
- Tổng (`summary`): tổng `quantity` theo vai trò, tổng `actualAmount`, tổng `companyAmount` (`null` nếu người gọi không thấy giá công ty).
  Tổng tính trên **toàn bộ dòng trong phạm vi**, không theo trang.
- Sắp: loại văn bản, vai trò, họ tên.

## Hai API

Gốc `api/v1/operation/project/{projectId}`.

| Method | Route | Quyền | Việc |
|---|---|---|---|
| PUT | `member/price` | `Function.Operation.Price.ManageActual` | Body `{ items: [{ memberId, companyPrice?, actualPrice? }] }`, trả `{ updatedCount, members[] }` |
| GET | `wage/paging` | `Function.Operation.Wage.View` | Query `documentTypeId?`, `role?` (Data / QA), `keyword?`, `pageNumber`, `pageSize` (1–200). Trả dòng + `summary` |

Quyền `Price.ManageCompany` là key trong cây quyền để gán menu; API `member/price` không kiểm riêng nó, luật Admin nằm trong service
(`809`).

Mặc định gán bằng API (`PUT api/auth/role/permission`): Admin, Leader, Data, QA ([03](03-vai-tro-mac-dinh.md)). Admin có mọi key qua seeder.

## Mã lỗi

Không thêm mã mới. Dùng `808`, `809`, `806`, `805`, `818`, `821`, `303`, `312`, và `400` cho lỗi đầu vào. Đã bỏ `810`, `822`.

## ⚠️ Cache 60 giây

`IMemoryCache` khoá `wage:{projectId}`, 60 giây (`PriceConst.WAGE_CACHE_SECONDS`). Chỉ cache khối lượng; tiền và phạm vi xem tính lại
mỗi lần gọi, nên đổi giá thấy ngay. Kiểm văn bản hay chấm QA mới có thể trễ tới 60 giây.

⚠️ Bẫy khi test: cache không biết dữ liệu test đã bị xoá. Chạy lại test trong vòng 60 giây sau lần trước có thể thấy khối lượng cũ của
dự án đó. Đợi quá 60 giây rồi chạy lại.

## Đã kiểm thực tế

Dự án tạm `115`: một Leader, ba thành viên (hai Data, một QA), hồ sơ test chèn bằng SQL, cấp tạm quyền `Wage.View` cho Leader / Data / QA.
Xoá sạch sau test.

Ngày 2026-11-13 (Task 30, `ZZTEST30` đã dọn): loại văn bản 4 trường, 3 trường tính tiền, Data kiểm 4 văn bản → `quantity` 16 → **12**;
tắt `IsChargeable` thêm 1 trường, đợi hết cache 60 giây → 8.

## Chưa làm (task sau)

- Doanh thu, quỹ KPI (`bonus-pool`), lịch sử giá: không thuộc task này.
- Chốt kỳ thanh toán (TC-09), dashboard (TC-08), lọc theo khoảng ngày, xuất Excel thống kê, FE.

> **Tiếp:** [25-xoa-du-an-danh-sach-cap.md](25-xoa-du-an-danh-sach-cap.md) — xoá dự án kèm dữ liệu và danh sách cặp dự án – loại văn bản.
