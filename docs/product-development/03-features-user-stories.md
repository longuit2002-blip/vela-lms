# P3 · Tính năng & User Stories / Features & User Stories

> Liên quan: [P1](01-vision-scope.md) · [P2 Roles](02-personas-roles.md) · [P4 Roadmap](04-roadmap-metrics.md) · Triển khai kỹ thuật trỏ tới các tài liệu T*.

**Quy ước:** mỗi Epic gắn module nguồn. User story dạng *"Là [role], tôi muốn… để…"*. Acceptance criteria (AC) viết Gherkin rút gọn. Ưu tiên: `[M]/[S]/[C]` (xem [P1 §6](01-vision-scope.md)).

**Mục lục Epics:** E1 Navigation/Shell · E2 Explore · E3 Dashboard cá nhân · E4 Library & Learning · E5 Video Player · E6 Gamification · E7 Reports · E8 Members · E9 DMS · E10 Publishing · E11 Training Mgmt · E12 AI LMS · E13 Header/Profile · E14 Onboarding/Tenant

---

## E1 · Navigation & App Shell `[M]`
*(Nguồn §1)*

**E1-S1** Là người dùng, tôi muốn 2 thanh điều hướng (top nav cho học/báo cáo, sidebar trái cho quản trị) để truy cập đúng khu vực theo vai trò.
- AC: top nav gồm Khám phá `/`, Từ công ty `/cua-ban`, Thư viện `/thu-vien`, Xếp hạng `/xep-hang`, Báo cáo `/bao-cao`.
- AC: sidebar admin gồm Thành viên, Tài liệu, Xuất bản, QL đào tạo, AI LMS, Hướng dẫn — **chỉ hiển thị mục mà role có quyền** (xem [P2 §3](02-personas-roles.md)).
- AC: là SPA — chuyển trang **không full reload** (client routing); URL đổi; có loading skeleton khi render trễ.
- AC: active state phản ánh route hiện tại.

**E1-S2** Là người dùng mobile, tôi muốn nav co gọn (drawer) để dùng trên màn hình nhỏ.

---

## E2 · Khám phá / Explore `[M]`
*(Nguồn §2, route `/`)*

**E2-S1** Là khách/người học, tôi muốn xem kho khóa học gom theo **6 nhóm kỹ năng** để tìm khóa phù hợp.
- AC: hiển thị banner hero kép (CTA "30 khóa miễn phí" + "Tạo LMS cho doanh nghiệp") và dải logo "550+ doanh nghiệp tin dùng".
- AC: nội dung nhóm theo skill group; mỗi nhóm hiển thị badge số lượng khóa.
- AC: **course card** gồm: thumbnail, lượt xem, tên khóa, thanh tiến độ %, số bài học, avatar nhóm người học + tổng số người học.
- AC: có **card lộ trình** (learning path) phân biệt rõ với card khóa đơn.
- AC: nội dung chỉ hiển thị theo audience scope của người xem (guest chỉ thấy khóa công khai).

**E2-S2** Là người học đã đăng nhập, tôi muốn thấy tiến độ % của mình trên course card để biết khóa nào đang dở.

---

## E3 · Dashboard cá nhân / "Từ công ty" `[M]`
*(Nguồn §3, route `/cua-ban`)*

**E3-S1** Là học viên, tôi muốn thấy danh sách khóa **được giao** để biết việc cần làm.
- AC: cột trái liệt kê khóa được giao; nếu rỗng → empty state + CTA "Khám phá thư viện".
- AC: mỗi mục hiển thị tiến độ, hạn (nếu có).

**E3-S2** Là học viên, tôi muốn **profile widget** bên phải hiển thị: vai trò, **rank hiện tại** + điểm còn thiếu để lên hạng, 4 chỉ số (Hoàn thành / Giờ học / Đang học / Chưa học), bộ lọc **Năm + Quý**, stickers, "Kế hoạch đào tạo tổ chức", và **leaderboard mini**.
- AC: 4 chỉ số cập nhật theo filter Năm/Quý.
- AC: thanh tiến độ rank hiển thị `điểm hiện tại / ngưỡng rank kế tiếp`.

---

## E4 · Thư viện & Trải nghiệm học / Library & Learning `[M]`
*(Nguồn §4, route `/thu-vien`, `/noi-dung/<slug>`)*

**E4-S1** Là người học, tôi muốn lọc thư viện theo **đa danh mục** (checkbox) để thu hẹp kết quả.
- AC: layout 2 cột: trái = checkbox lọc đa danh mục (mỗi nhóm có badge số lượng); phải = lưới course/document card.
- AC: chọn nhiều danh mục → kết quả là hợp/giao theo thiết kế filter (mặc định: union trong nhóm, intersection giữa nhóm).
- AC: hiển thị cả **course** và **document** card.

**E4-S2** Là người học, tôi muốn trang **chi tiết khóa học** thể hiện "HÀNH TRÌNH HỌC TẬP".
- AC: card hành trình hiển thị "Hoàn thành x/y" + "Tiến độ %".
- AC: bài học gom theo **module/chương**; mỗi lesson là VIDEO với trạng thái **Hoàn thành / Chưa bắt đầu**; mỗi lesson hoàn thành = 1 task.
- AC: 2 tab: **Nội dung** / **Báo cáo**.
- AC: phần "Những gì bạn sẽ học" + CTA "Học bài đầu tiên".
- AC: nếu khóa bật **học theo thứ tự (sequential)** → lesson sau khóa cho tới khi lesson trước hoàn thành.

**E4-S3** Là học viên, tôi muốn đánh dấu hoàn thành lesson khi xem hết video để cập nhật tiến độ & điểm.
- AC: lesson chuyển "Hoàn thành" khi đạt ngưỡng xem (vd ≥ 95% thời lượng) hoặc bấm hoàn thành nếu policy cho phép.
- AC: hoàn thành → tiến độ khóa & điểm rank cập nhật (xem E6).

---

## E5 · Video Player `[M]`
*(Nguồn §4.2)*

**E5-S1** Là học viên, tôi muốn player tùy biến: play/pause, volume, time, **chất lượng (HD)**, **tốc độ (1x…)**, **picture-in-picture**, **fullscreen**.

**E5-S2** Là tổ chức, tôi muốn **watermark email người học chạy động trên video** để chống chia sẻ trái phép.
- AC: watermark hiển thị email (hoặc mã NV) của người đang xem, **di chuyển/đổi vị trí định kỳ** (chống che/crop).
- AC: video phát qua **HLS + signed URL ngắn hạn**; không cho tải trực tiếp file gốc (xem [T6](../technical-design/06-media-video-scorm.md)).
- AC: tua/seek không reset tiến độ đã xem.

---

## E6 · Xếp hạng & Gamification `[M]`
*(Nguồn §5, route `/xep-hang`)*

**E6-S1** Là người học, tôi muốn hệ thống **9 bậc rank** (Đồng→Thách Đấu) để có động lực.
- AC: rank tính từ điểm tích lũy; ngưỡng cấu hình được.
- AC: hành động sinh điểm: hoàn thành lesson/khóa, đạt điểm thi, đủ khung giờ… (cấu hình ở [T7](../technical-design/07-gamification-reporting.md)).

**E6-S2** Là người dùng, tôi muốn leaderboard với 2 tab **Phòng ban / Chức vụ** và filter scope **TỔ CHỨC / KH-ĐỐI TÁC / KHÁCH**, theo kỳ **Tháng / Quý / Năm / Toàn thời gian**.
- AC: leaderboard sắp xếp theo điểm trong kỳ; hiển thị thứ hạng, avatar, tên, điểm.
- AC: vị trí của "tôi" được highlight kể cả ngoài top.

**E6-S3** Là người học, tôi muốn nhận **sticker/huy hiệu** khi đạt mốc thành tích.

---

## E7 · Báo cáo / Reports `[M]` (cơ bản) / `[S]` (nâng cao)
*(Nguồn §6, routes `/bao-cao`, `/bao-cao-dao-tao`)*

**E7-S1 — Báo cáo Xuất bản** `[M]` Là admin, tôi muốn thấy 6 thẻ chỉ số (Tổng xuất bản, Sự kiện, Kỳ thi, Khóa học, Lộ trình, Tài liệu khác) + filter Năm/Quý + biểu đồ tròn tỷ lệ hoàn thành.
- AC: 2 tab Xuất bản / Đào tạo.
- AC: số liệu lọc theo Năm/Quý.

**E7-S2 — Báo cáo Đào tạo** `[S]` Là L&D/BGĐ, tôi muốn KPI: tổng lớp, lượt HV, khung đào tạo YTD, điểm HV ≥ 8.5, % tham dự, lượt GV, tổng giờ dạy, điểm GV, điểm đánh giá TB; biểu đồ loại hình đào tạo, xếp hạng giảng viên, % hoàn thành khung theo phòng ban.
- AC: lọc theo kỳ & phòng ban; export được.
- AC: định nghĩa từng KPI thống nhất với [T7 §4](../technical-design/07-gamification-reporting.md).

**E7-S3** Là khóa học, tab **Báo cáo** trong trang chi tiết khóa hiển thị tiến độ học viên của khóa đó.

---

## E8 · Thành viên / Members `[M]`
*(Nguồn §7, route `/thanh-vien`)*

**E8-S1** Là admin, tôi muốn 2 tab **Phòng ban / Chức vụ** + cây tổ chức (TỔ CHỨC → phòng ban + nhóm KH-ĐỐI TÁC, KHÁCH) để duyệt người dùng theo cơ cấu.
- AC: bảng thành viên: tên + @handle + badge vai trò, liên hệ, phòng ban/chức vụ, trạng thái + last-seen.
- AC: chọn node cây → lọc danh sách theo node (gồm phòng con).

**E8-S2 — Tạo tài khoản** `[M]` Là admin, tôi muốn modal "Thêm mới một tài khoản" với **3 phương thức**: Tạo đơn / Danh sách email (hàng loạt) / Từ file Excel (import).
- AC: trường: email, SĐT, họ tên, mã NV, **mật khẩu mặc định**, vị trí công việc (phòng ban + chức vụ).
- AC: import Excel có **file mẫu** tải về + báo lỗi theo dòng.
- AC: ⚠️ **Thao tác nhạy cảm** (tạo tài khoản/đặt mật khẩu) — yêu cầu xác nhận rõ ràng, **không auto-submit**; log audit.
- AC: trùng email/mã NV → cảnh báo, không tạo trùng.

**E8-S3** Là admin, tôi muốn khóa/mở/đổi vai trò/đổi phòng ban của user.

---

## E9 · Tài liệu / DMS `[M]`
*(Nguồn §8, route `/tai-lieu`)*

**E9-S1** Là người dùng, tôi muốn quản lý tài liệu **theo cây tổ chức** + nhóm "CHIA SẺ VỚI TÔI", cấu trúc **folder-based**.
- AC: CRUD thư mục & tài liệu; phân quyền theo phòng ban/scope.
- AC: chia sẻ tài liệu/thư mục cho user/phòng khác → xuất hiện ở "Chia sẻ với tôi".

**E9-S2** Là người dùng, tôi muốn **tìm kiếm** thư mục / tài liệu / **câu hỏi** (gợi ý có ngân hàng câu hỏi).
- AC: search trả kết quả gộp 3 loại; tôn trọng phân quyền.

---

## E10 · Xuất bản / Publishing Center `[M]` (core) → `[S]/[C]` (loại nâng cao)
*(Nguồn §9, route `/xuat-ban`, `/tao-xuat-ban/...`) — ⭐ module trung tâm*

**E10-S1** Là người tạo nội dung, tôi muốn trung tâm xuất bản với 2 tab **Xuất bản / Đào tạo**, toggle **Grid/List**, và **cây đích** (Tổ chức, KH-Đối tác, Thư viện nội bộ, Thư viện KH-Đối tác, Thư viện ngoài, Thư mục nháp).

**E10-S2 — Dropdown "Xuất bản đào tạo" (4 loại)**: **Khóa học** `[M]` · **Lộ trình** `[S]` · **Kỳ thi** `[S]` · **Lớp học/Sự kiện/Workshop** `[S]`.

**E10-S3 — Dropdown "Xuất bản nội dung" (5 loại)**: **Một tài liệu** `[M]` · **Nhiều tài liệu** `[M]` · **Podcast** `[S]` · **Video** `[S]` · **SCORM** `[C]`.

**E10-S4 — Trình tạo Khóa học** `[M]` *(route `/tao-xuat-ban/xuat-ban-khoa-hoc`)* với 3 tab:
- **Nội dung:** cấu trúc module/chương → lesson (video), kéo-thả sắp xếp.
- **Chỉnh sửa:** tên, danh mục, thumbnail, **mô tả (rich-text editor đầy đủ)**, panel chọn tài liệu (Theo phòng ban / Được chia sẻ) + "Tạo thư mục".
- **Cài đặt xuất bản:** chọn **đối tượng truy cập (5 nhóm)** + thêm nhanh bằng **email/mã NV/SĐT**; các toggle/field:
  - Công khai (public)
  - Tiếp tục học khi hết hạn
  - **Học theo thứ tự (sequential)**
  - Loại hình đào tạo
  - **Điểm xếp hạng hoàn thành** (points on completion)
  - Lĩnh vực (skill domain)
  - Hết hạn sau khi tham gia (ngày)
  - Thời gian đào tạo (giờ)
  - nút **Xuất bản**.
- AC: lưu nháp được; validate trước publish (phải có ≥1 lesson, có đối tượng).
- AC: ⚠️ publish là thao tác có hệ quả → xác nhận; ghi audit; ai có quyền publish theo [P2 §3](02-personas-roles.md).

---

## E11 · Quản lý đào tạo / Training Management `[S]`
*(Nguồn §10, route `/quan-ly-dao-tao`) — 3 tab*

**E11-S1 — Báo cáo đào tạo theo nhân sự:** bảng từng nhân sự (Điểm Star priority, Tổng giờ, Tiến độ %, Khác).

**E11-S2 — Cài đặt khung giờ đào tạo:** tổng giờ yêu cầu theo **phòng ban/chức vụ** + Thêm Tag / **Import Excel** / Tải file mẫu.
- AC: khung giờ định nghĩa "giờ đào tạo bắt buộc" dùng cho compliance & báo cáo % hoàn thành khung.

**E11-S3 — Loại hình đào tạo:** **7 loại mặc định** (E-Learning, ĐT nội bộ trực tiếp, ĐT bên ngoài, ĐT hãng, ĐT outsource, Hội thảo, ĐT khách hàng) — **CRUD**.

---

## E12 · AI LMS `[C]`
*(Nguồn §11, route `/ai-lms`) — ⭐ trợ lý AI*

**E12-S1** Là người tạo nội dung, tôi muốn giao diện chat AI ("Hỏi bất kỳ điều gì") + **mic (voice)** + **đính kèm (+)**.

**E12-S2 — 4 quick-action:**
- **Tạo bài học/đọc bằng AI** — sinh nội dung lesson/script từ prompt + tài liệu đính kèm.
- **Tạo video đào tạo bằng AI** — sinh kịch bản + voice-over (TTS) cho video.
- **Tạo khóa học bằng AI** — sinh outline module/lesson cho cả khóa.
- **Tra cứu thông tin** — RAG trên tài liệu/khóa của tổ chức.
- AC: kết quả AI là **bản nháp** → người dùng review/sửa rồi mới publish (không tự publish).
- AC: AI chỉ truy cập nội dung trong scope/quyền của người dùng.
- AC: stream phản hồi (SignalR/SSE); hiển thị nguồn khi tra cứu.

---

## E13 · Header tiện ích & Profile `[M]`
*(Nguồn §12)*

**E13-S1 — Global search:** ô tìm kiếm toàn cục mở từ header; tìm xuyên khóa/tài liệu/người dùng (theo quyền).

**E13-S2 — Profile dropdown:** Quản lý tài khoản, **Quản lý hệ thống** (admin), **Ngôn ngữ (Tiếng Việt)**, Đổi mật khẩu, Đăng xuất.

**E13-S3 — Footer:** chính sách bảo mật/dịch vụ, hướng dẫn, link tải **App Store / Google Play** (mobile app).

---

## E14 · Onboarding & Tenant `[M]` *(suy luận — cần cho greenfield, không có trong hand-over)*

**E14-S1** Là BetterWork operator, tôi muốn tạo **tổ chức (tenant)** mới + tài khoản OrgOwner đầu tiên.
- AC: tạo organization → seed cây tổ chức tối thiểu, 9 rank mặc định, 7 loại hình đào tạo, 6 skill group.

**E14-S2** Là OrgOwner mới, tôi muốn wizard onboarding: nhập thông tin tổ chức, import user, tạo khóa đầu tiên.

**E14-S3** Là người dùng, tôi muốn đăng nhập (email + mật khẩu), đổi mật khẩu lần đầu (nếu dùng mật khẩu mặc định), quên mật khẩu.
- AC: chi tiết AuthN ở [T5](../technical-design/05-auth-rbac-tenancy.md).

---

## Phụ lục · Bảng việc "Chưa thực hiện" từ hand-over (cần thiết kế đầy đủ khi build)
*(Nguồn §14)*

| Hạng mục | Trạng thái nguồn | Xử lý trong spec |
|---|---|---|
| Tạo tài khoản thật / submit publish / chạy AI | Chưa làm (cần quyền) | Đặc tả đầy đủ flow + audit (E8, E10, E12) |
| Tab "Báo cáo" trong từng khóa | Chưa khám phá | E7-S3 |
| Trang `/huong-dan-su-dung` | Chưa làm | Help center tĩnh (Phase 2) |
| "Quản lý hệ thống" | Chưa khám phá | Org settings + roles + audit (T5, T9) |
