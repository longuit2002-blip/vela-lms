# T6 · Media, Video Pipeline, Watermark & SCORM

> Liên quan: [P3 E5](../product-development/03-features-user-stories.md) · [T3 media_assets](03-database-schema.md) · [T4 §4.4](04-api-design.md) · [T9 Security](09-infra-security-nfr.md)

Đây là module **lợi thế cạnh tranh & rủi ro cao**: player tùy biến (không YouTube embed) với **watermark email động chống leak** + HLS adaptive + SCORM. Yêu cầu lõi từ hand-over (§4.2, §14).

---

## 1. Mục tiêu / Goals
1. Phát video mượt, adaptive bitrate (HLS, nhiều chất lượng, HD).
2. **Chống chia sẻ trái phép:** không lộ file gốc; signed URL ngắn hạn; **watermark email động** chạy trên video.
3. Player tùy biến: play, volume, time, chất lượng, tốc độ, PiP, fullscreen; tua không reset tiến độ.
4. Hỗ trợ podcast (audio), tài liệu, và **SCORM 1.2/2004** (Phase 3).

---

## 2. Upload → Transcode → Ready (pipeline)

```
Client                         API                       Storage / Worker
  │  POST /media/upload-url      │                              │
  │ ───────────────────────────►│  presign PUT (S3, short TTL) │
  │ ◄───────────────────────────│  {uploadUrl, key}            │
  │  PUT mp4 ──────────────────────────────────────────────────►│  (S3 raw bucket)
  │  POST /media {key,kind}      │                              │
  │ ───────────────────────────►│  insert media_assets(uploaded)│
  │                              │  enqueue Hangfire: Transcode │──►│ Worker
  │                              │                              │   FFmpeg → HLS renditions
  │                              │                              │   upload .m3u8 + .ts to S3
  │                              │  update status=ready         │◄──│ MediaTranscoded event
  │  GET /media/{id} → ready     │                              │
```

### 2.1 FFmpeg → HLS (multi-bitrate)
Sinh master playlist + renditions (vd 1080p/720p/480p/360p):
```bash
ffmpeg -i source.mp4 \
  -filter_complex "[0:v]split=4[v1][v2][v3][v4]; \
     [v1]scale=w=1920:h=1080[v1o];[v2]scale=w=1280:h=720[v2o]; \
     [v3]scale=w=854:h=480[v3o];[v4]scale=w=640:h=360[v4o]" \
  -map "[v1o]" -c:v:0 h264 -b:v:0 5000k  -map a:0 \
  -map "[v2o]" -c:v:1 h264 -b:v:1 2800k  -map a:0 \
  -map "[v3o]" -c:v:2 h264 -b:v:2 1400k  -map a:0 \
  -map "[v4o]" -c:v:3 h264 -b:v:3 800k   -map a:0 \
  -f hls -hls_time 6 -hls_playlist_type vod \
  -hls_segment_filename "out/%v/seg_%03d.ts" \
  -master_pl_name master.m3u8 -var_stream_map "v:0,a:0 v:1,a:1 v:2,a:2 v:3,a:3" \
  out/%v/index.m3u8
```
- Segment 6s; thumbnail/poster sinh kèm; lưu `renditions[]` vào `media_assets`.
- Worker idempotent (re-run an toàn); retry/backoff qua Hangfire.
- **Encryption (option):** HLS AES-128 — key cấp qua endpoint có auth (nâng cấp DRM-lite). MVP: signed URL + watermark đủ; AES-128 ở Phase 2 nếu yêu cầu.

---

## 3. Playback security (signed URL)

### 3.1 Quy trình
```
Client mở lesson → GET /media/{id}/playback
  API:
    1. authZ: user có quyền học publication chứa lesson? (scope/assign — T5 §4.2)
    2. media.status = ready?
    3. cấp signed URL cho master.m3u8 (TTL ngắn, vd 2–5 phút) qua CDN signed cookie/URL
    4. trả { manifestUrl, watermark: { text: <email|empcode>, ... }, ttl }
Player tải manifest qua CDN; mỗi segment cũng cần ký (signed cookie phủ path).
```
- **Không** trả S3 URL gốc trực tiếp; chỉ qua CDN với **signed URL/cookie** (CloudFront signed cookies phủ cả thư mục segment).
- TTL ngắn → link chia sẻ hết hạn nhanh.
- Quyền kiểm tại thời điểm cấp; refresh khi gần hết hạn.

### 3.2 Vì sao đủ chống leak "thường"
| Tấn công | Phòng thủ |
|---|---|
| Copy URL gửi người khác | Signed URL hết hạn (phút); gắn IP/cookie phiên |
| Tải file gốc | Không expose; chỉ segment HLS qua CDN ký |
| Quay màn hình | **Watermark email động** → truy được người leak |
| Chia sẻ tài khoản | Watermark hiện email; rate-limit phiên; (tuỳ chọn) giới hạn thiết bị |
> Không hệ thống nào chống 100% screen-record; mục tiêu là **truy vết & răn đe** + chặn copy file dễ dàng.

---

## 4. Dynamic Email Watermark (chống leak)

### 4.1 Thiết kế
- **Client-side overlay** (mặc định, rẻ, co giãn): lớp DOM/canvas phủ trên `<video>` hiển thị `email` (hoặc mã NV) người đang xem.
- **Động:** vị trí watermark **đổi định kỳ** (vd mỗi 3–5s nhảy vị trí ngẫu nhiên trong khung), opacity thấp, có thể nghiêng → chống crop/che cố định.
- Nội dung lấy từ phiên đăng nhập (không nhận từ client để giả mạo): API trả `watermark.text` cùng playback.

```tsx
// Web overlay (rút gọn)
function Watermark({ text }: { text: string }) {
  const [pos, setPos] = useState({ top: '10%', left: '10%' });
  useEffect(() => {
    const id = setInterval(() => setPos({
      top: `${10 + Math.floor(/*seeded*/ rand()*70)}%`,
      left:`${10 + Math.floor(rand()*70)}%`,
    }), 4000);
    return () => clearInterval(id);
  }, []);
  return <div className="pointer-events-none absolute select-none opacity-30 text-xs"
              style={{ ...pos, transform:'rotate(-18deg)' }}>{text} · {nowHHmm()}</div>;
}
```

### 4.2 Server-side burn-in (tuỳ chọn, mạnh hơn)
Khi cần chống bypass DOM: **burn watermark vào pixel** bằng FFmpeg `drawtext` tại thời điểm phục vụ (per-user transcode/segment — tốn kém) hoặc dùng forensic watermark theo phiên. Quyết định: **MVP dùng client overlay**; nâng cấp burn-in/forensic cho nội dung mật cao (Phase 3, cấu hình per-publication).

```bash
# burn-in ví dụ (per-user, đắt — chỉ nội dung nhạy cảm)
ffmpeg -i in.m3u8 -vf "drawtext=text='lan@cty.vn':fontcolor=white@0.3:fontsize=24:\
  x='if(eq(mod(n,150),0),rand(0,w-tw),x)':y='if(eq(mod(n,150),0),rand(0,h-th),y)'" out.m3u8
```

---

## 5. Player (frontend) — yêu cầu chức năng
- Dựa **Vidstack/Video.js + HLS.js** (fallback native HLS Safari).
- Controls: play/pause, seek bar (buffered/played), volume/mute, time, **quality menu (Auto/1080/720/…)**, **speed (0.5–2x)**, **PiP**, fullscreen.
- **Tiến độ:** gửi `POST /lessons/{id}/progress` throttle (vd mỗi 10s) với `watchedSeconds`, `watchRatio`; lưu vị trí để resume; tua không reset đã-xem.
- **Hoàn thành:** đạt ngưỡng watch (≥95%) → enable/auto `POST /lessons/{id}/complete`.
- **Anti-cheat:** đo `watchRatio` (thời lượng xem thật / thời lượng video); seek-skip lớn không tính hoàn thành (chống auto-play cày điểm — guardrail [P4 §3.3](../product-development/04-roadmap-metrics.md)).
- Watermark overlay luôn bật; không cho ẩn.

---

## 6. Audio/Podcast & Document
- **Podcast/Audio:** cùng pipeline (HLS audio hoặc progressive có signed URL); player audio + watermark text (ít rủi ro hơn).
- **Document:** PDF/Office → xem qua viewer (PDF.js); tải bị giới hạn theo policy; watermark text trên viewer tùy chọn.

---

## 7. SCORM (Phase 3)

### 7.1 Import
- Upload gói `.zip` SCORM → Worker giải nén, đọc `imsmanifest.xml`, lưu vào S3 (`media_assets.kind='scorm'`), trích cấu trúc SCO.

### 7.2 Runtime
- Phục vụ nội dung SCORM trong **iframe sandbox**; cung cấp **SCORM API adapter** (JS) implement:
  - SCORM 1.2: `LMSInitialize, LMSGetValue, LMSSetValue, LMSCommit, LMSFinish` (cmi.core.*).
  - SCORM 2004: `Initialize, GetValue, SetValue, Commit, Terminate` (cmi.*).
- Adapter map `cmi.core.lesson_status`, `cmi.score.raw`, `cmi.session_time` → cập nhật `lesson_progress`/`enrollments` qua API.
- Theo dõi completion & score như lesson thường → tích hợp gamification/báo cáo.

### 7.3 Lưu ý
- SCORM chạy trong iframe → vẫn áp signed URL + scope; cô lập origin để an toàn.
- xAPI/cmi5 cân nhắc tương lai nếu cần tracking phong phú hơn.

---

## 8. Storage & lifecycle
- Buckets: `raw-uploads` (private, TTL dọn sau transcode), `hls-output` (private, phục vụ qua CDN ký), `documents`, `scorm`, `thumbnails` (public-ish/CDN).
- Lifecycle: raw upload xóa sau X ngày khi đã `ready`; backup HLS.
- Mỗi object gắn `organization_id` trong key prefix (`{org}/{assetId}/...`) để cô lập & dọn theo tenant.

---

## 9. Quan sát & chỉ số / Observability
- Job transcode: thời gian, tỷ lệ fail, retry (Hangfire dashboard).
- Playback: startup time, rebuffer ratio, lỗi 403 signed URL.
- Anti-leak: số lần phát hiện chia sẻ (từ watermark report), phiên bất thường.
