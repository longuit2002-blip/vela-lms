# D0 · Brand Identity — Vela

> Liên quan: [D1 Foundations/Tokens](01-foundations.md) · [D2 Components](02-components.md) · [D3 Patterns & Content](03-patterns-content.md)
>
> Thương hiệu của sản phẩm. **Vela là nhận diện riêng** — không tái sử dụng identity của sản phẩm nguồn (GOS ACADEMY / BetterWork) mà spec này tham chiếu để xây lại.

---

## 1. Tên / Name

**Vela** — /ˈveɪlə/ (EN) · "Vê-la" (VN).

- Latin **vela** = "những cánh buồm"; cũng là chòm sao **Vela** ("the sails").
- Ý nghĩa cho sản phẩm: một **hành trình học tập có động lực** (gió căng buồm) + một **ngôi sao dẫn đường** cho sự phát triển. Bắt đúng hai cốt lõi của sản phẩm: "hành trình học tập" và **lộ trình thăng hạng** (9 rank, leaderboard, thành tích).
- Ngắn (2 âm tiết), phát âm sạch ở cả tiếng Việt lẫn tiếng Anh; trừu tượng nên **dễ sở hữu** (trademark/domain).

> ⚠️ "Vela" có prior use ở mảng lân cận (VELA Education Fund; hệ thống AI-training "Vela" của IBM). **Không** thấy va chạm trực tiếp trong corporate-LMS. Verify domain/trademark trước khi thương mại hóa quốc tế.

---

## 2. Định vị / Positioning

Nền tảng **đào tạo doanh nghiệp (enterprise LMS) Việt-Nam-first**: học · thi · đo lường · tạo nội dung bằng AI — **gamified**.

- **Tagline** — VN: *"Căng buồm tri thức."* · EN: *"Set learning in motion."*
- **One-liner:** "Vela — nền tảng đào tạo doanh nghiệp all-in-one: gamified, hỗ trợ AI, chống rò rỉ nội dung."

---

## 3. Logo concept (định hướng, chưa phải artwork cuối)

- **Mark:** một **cánh buồm / chevron hướng lên** đồng thời gợi (a) glyph **▶ play** (học qua video) và (b) nét **đi lên** (thăng hạng). Một **ngôi sao 4 cánh** nhỏ (chòm Vela) làm điểm nhấn thành tích/rank — **dùng chung ngôn ngữ thị giác với `RankBadge`** ([D2](02-components.md)).
- **Dựng hình:** buồm = tam giác nghiêng phải (chuyển động); sao đặt góc trên-phải (tùy chọn).
- **Wordmark:** "Vela" bằng brand sans (Be Vietnam Pro / Inter), medium; chữ "V" có thể lặp lại nét chevron của buồm.
- **Biến thể:** chỉ-mark (app icon, favicon) · mark + wordmark (ngang) · monochrome.

```
   ◤            V e l a
  ◤◣  ★    (buồm/chevron + sao)
```

---

## 4. Màu thương hiệu (gắn với design tokens — [D1 §2](01-foundations.md))

| Vai trò | Token | Hex |
|---|---|---|
| **Vela Blue** (primary — biển/trời, tin cậy) | `--color-primary` | `#2563EB` |
| Accent thành tích/sao | `--rank-gold` | `#E0A21A` |
| Nền/chữ/viền | theo D1 (`--color-bg/surface/text/border`) | — |

---

## 5. Typography & Voice

- **Type:** Be Vietnam Pro / Inter ([D1 §3](01-foundations.md)) — Việt-first, hiện đại, sạch.
- **Voice** ([D3 §4](03-patterns-content.md)): chuyên nghiệp · thân thiện · khích lệ; xưng **"bạn"**.

---

## 6. Usage

- **Tên sản phẩm trong UI:** "**Vela**" (đứng một mình). Ngữ cảnh mô tả: "Vela — LMS doanh nghiệp".
- **Không** dùng lại nhận diện sản phẩm nguồn (GOS ACADEMY / BetterWork) — chỉ tham chiếu khi nói về provenance.
- **Repo / package:** `vela-lms`. Org/platform operator trong docs = "Vela".
