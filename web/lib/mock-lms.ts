export const currentUser = {
  name: "Nguyễn Linh",
  initials: "NL",
  role: "Học viên",
  adminRole: "L&D Admin",
  team: "Phòng Nhân sự",
  organization: "BetterWork Vietnam",
  tenant: "BWV-001",
  quarter: "Q2/2025",
  rank: "Vàng",
  points: 2840,
  nextRank: "Bạch Kim",
  pointsToNextRank: 180,
};

export const operatingLenses = [
  { label: "Tổ chức", value: "BetterWork Vietnam", detail: "Tenant · betterwork.vn" },
  { label: "Vai trò", value: "L&D Admin", detail: "Đang online" },
  { label: "Kỳ báo cáo", value: "01/06 - 30/06/2026", detail: "Q2/2026" },
];

export const learnerQueue = [
  {
    id: "01",
    title: "An toàn thông tin cho nhân sự tuyến đầu",
    reason: "Bắt buộc theo khung giờ Q2",
    scope: "Nội bộ",
    due: "Còn 2 ngày",
    module: "Bài 3: Quản lý truy cập",
    progress: 78,
    tone: "success",
  },
  {
    id: "02",
    title: "Bảo mật dữ liệu khách hàng",
    reason: "Bảo vệ thông tin · Tuân thủ · Quy trình chuẩn",
    scope: "Nội bộ",
    due: "Còn 5 ngày",
    module: "Bài 2: Mã hóa dữ liệu",
    progress: 62,
    tone: "success",
  },
  {
    id: "03",
    title: "Quy trình phê duyệt và phân quyền",
    reason: "Nhánh phòng ban · Role · Audience",
    scope: "Nội bộ",
    due: "Còn 7 ngày",
    module: "Bài 1: Tổng quan",
    progress: 45,
    tone: "warning",
  },
  {
    id: "04",
    title: "Ứng xử chuyên nghiệp nơi làm việc",
    reason: "Văn hóa · Giao tiếp · Hợp tác",
    scope: "Nội bộ",
    due: "Còn 12 ngày",
    module: "Bài 2: Giao tiếp tích cực",
    progress: 30,
    tone: "warning",
  },
  {
    id: "05",
    title: "Phòng chống rửa tiền và gian lận",
    reason: "Tuân thủ pháp luật · AML · KYC",
    scope: "Nội bộ",
    due: "Quá hạn 1 ngày",
    module: "Bài 1: Nhận diện rủi ro",
    progress: 10,
    tone: "danger",
  },
];

export const leaderboard = [
  { rank: "1", name: "Trần Minh Khoa", team: "Phòng ATTT", points: "3.420", medal: "gold" },
  { rank: "2", name: "Lê Phương Anh", team: "Phòng Pháp chế", points: "3.120", medal: "silver" },
  { rank: "3", name: "Đỗ Hoàng Nam", team: "Khối Vận hành", points: "2.980", medal: "bronze" },
  { rank: "38", name: "Nguyễn Linh", team: "Phòng Nhân sự", points: "2.840", medal: "self" },
];

export const learningFlow = [
  { label: "Đã giao", value: "1.840", detail: "khóa học", icon: "assigned", tone: "coral" },
  { label: "Đang học", value: "1.126", detail: "khóa học", icon: "learning", tone: "blue" },
  { label: "Hoàn thành", value: "582", detail: "khóa học", icon: "complete", tone: "teal" },
  { label: "Sẵn sàng xuất bản", value: "36", detail: "khóa học", icon: "publish", tone: "gold" },
] as const;

export const trainingQueue = [
  { id: "01", title: "An toàn thông tin cho nhân sự tuyến đầu", due: "2 ngày", scope: "Nội bộ", progress: 78 },
  { id: "02", title: "Quy định bảo mật dữ liệu khách hàng", due: "5 ngày", scope: "Nội bộ", progress: 62 },
  { id: "03", title: "Quản lý truy cập và phân quyền", due: "7 ngày", scope: "Nội bộ", progress: 48 },
  { id: "04", title: "Kỹ năng giao tiếp với khách hàng", due: "12 ngày", scope: "Nhà thầu", progress: 36 },
  { id: "05", title: "Huấn luyện ISO 9001 phiên bản 2015", due: "15 ngày", scope: "Đối tác", progress: 24 },
];

export const branchMap = [
  {
    branch: "Vận hành",
    city: "Hà Nội",
    assigned: "318 / 420",
    percent: 76,
    tone: "teal",
    scopes: [
      { label: "Nội bộ", value: "210 / 260", percent: 81 },
      { label: "Đối tác", value: "108 / 160", percent: 68 },
    ],
  },
  {
    branch: "Sản xuất",
    city: "Hải Phòng",
    assigned: "402 / 610",
    percent: 66,
    tone: "amber",
    scopes: [
      { label: "Nội bộ", value: "260 / 360", percent: 72 },
      { label: "Nhà thầu", value: "142 / 250", percent: 57 },
    ],
  },
  {
    branch: "Chất lượng",
    city: "Đà Nẵng",
    assigned: "178 / 240",
    percent: 45,
    tone: "red",
    scopes: [
      { label: "Nội bộ", value: "110 / 160", percent: 44 },
      { label: "Đối tác", value: "68 / 80", percent: 35 },
    ],
  },
  {
    branch: "Hỗ trợ",
    city: "Hồ Chí Minh",
    assigned: "350 / 570",
    percent: 61,
    tone: "teal",
    scopes: [
      { label: "Nội bộ", value: "280 / 420", percent: 67 },
      { label: "Khách mới", value: "70 / 150", percent: 47 },
    ],
  },
];

export const riskLanes = [
  { team: "Vận hành", completion: 76, risk: "3 khóa sát hạn", tone: "success" },
  { team: "Sản xuất", completion: 66, risk: "7 khóa quá hạn", tone: "warning" },
  { team: "Chất lượng", completion: 45, risk: "12 khóa quá hạn", tone: "danger" },
  { team: "Hỗ trợ", completion: 61, risk: "8 khóa sát hạn", tone: "warning" },
];

export const operationsMetrics = [
  { label: "Tỷ lệ hoàn thành", value: "68%", delta: "+12% so với kỳ trước", tone: "success" },
  { label: "Điểm trung bình", value: "2.840", delta: "/ 4.000", tone: "primary" },
  { label: "Giờ học YTD", value: "42,5", delta: "+8,6 giờ", tone: "success" },
  { label: "Ranking trung bình", value: "Vàng", delta: "+1 bậc", tone: "gold" },
  { label: "Tỷ lệ hoạt động", value: "92%", delta: "+5%", tone: "success" },
  { label: "Chi phí đào tạo / NV", value: "1.240.000đ", delta: "-6%", tone: "teal" },
];

export const publishPipeline = [
  { label: "Soạn thảo", value: "36", done: true },
  { label: "Rà soát nội dung", value: "18", done: true },
  { label: "Rà soát pháp lý", value: "12", done: false },
  { label: "Phê duyệt", value: "8", done: false },
  { label: "Công bố", value: "36", done: false },
];

export const sourceDocuments = [
  { title: "Chính sách bảo mật v2.1", type: "DOCX", tone: "document" },
  { title: "Quy trình xử lý sự cố", type: "PDF", tone: "danger" },
  { title: "Checklist kiểm toán", type: "XLSX", tone: "success" },
  { title: "Xem tất cả 18", type: "DMS", tone: "primary" },
];

export const aiAssist = [
  { title: "Tóm tắt nhanh", detail: "Tài liệu · 3 phút" },
  { title: "Quiz gợi ý", detail: "12 câu hỏi" },
  { title: "Gợi ý tiếp theo", detail: "Dựa trên tiến độ" },
];

export const progressRunway = [
  { label: "Được giao", detail: "07/06", tone: "danger", icon: "assigned" },
  { label: "Đang học", detail: "78%", tone: "primary", icon: "learning" },
  { label: "Bài kiểm tra", detail: "Chưa bắt đầu", tone: "warning", icon: "document" },
  { label: "Hoàn thành", detail: "40 điểm", tone: "success", icon: "complete" },
  { label: "Rank Vàng", detail: "Còn 180 điểm", tone: "gold", icon: "rank" },
] as const;

export const publishRunway = [
  { label: "Draft", detail: "Đang soạn", progress: "65%", tone: "danger", icon: "document" },
  { label: "Review", detail: "Đang review", progress: "2/3", tone: "warning", icon: "complete" },
  { label: "Legal", detail: "Chờ pháp chế", progress: "-", tone: "danger", icon: "document" },
  { label: "Approve", detail: "Chờ phê duyệt", progress: "-", tone: "gold", icon: "rank" },
  { label: "Publish", detail: "Sẵn sàng phát hành", progress: "-", tone: "success", icon: "complete" },
] as const;

export const studyRecords = [
  { title: "An toàn thông tin", lesson: "Bảo mật dữ liệu khách hàng", progress: 78, due: "Còn 2 ngày", score: "-", status: "Đang học" },
  { title: "Quy trình nội bộ", lesson: "Quy trình xử lý sự cố", progress: 42, due: "Còn 6 ngày", score: "-", status: "Đang học" },
  { title: "Văn hóa doanh nghiệp", lesson: "Giá trị cốt lõi", progress: 100, due: "Hoàn thành", score: "95", status: "Hoàn thành" },
  { title: "Kỹ năng giao tiếp", lesson: "Lắng nghe chủ động", progress: 0, due: "Quá hạn 1 ngày", score: "-", status: "Quá hạn" },
];

export const contentQueue = [
  { title: "An toàn thông tin cho NV mới", type: "E-learning", owner: "Nguyễn Linh", stage: "Review", updated: "07/06/2025" },
  { title: "Quy trình xử lý lô", type: "Tài liệu", owner: "Trần Minh", stage: "Draft", updated: "06/06/2025" },
  { title: "Chính sách bảo mật v2.1", type: "Tài liệu", owner: "Pháp chế", stage: "Legal", updated: "05/06/2025" },
  { title: "Kỹ năng giao tiếp", type: "E-learning", owner: "L&D Team", stage: "Approve", updated: "04/06/2025" },
];

export const readinessRisks = [
  { label: "Pháp chế", detail: "Chưa có xác nhận điều khoản", level: "Cao" },
  { label: "Tài liệu tham chiếu", detail: "2 tài liệu sắp hết hạn", level: "TB" },
  { label: "Quiz", detail: "Độ phủ mục tiêu học tập: 72%", level: "Thấp" },
];
