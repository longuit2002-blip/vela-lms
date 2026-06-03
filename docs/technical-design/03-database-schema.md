# T3 · Database Schema (PostgreSQL)

> Liên quan: [T2 Domain](02-domain-model-erd.md) · [T5 Auth/RLS](05-auth-rbac-tenancy.md) · [T7 Gamification](07-gamification-reporting.md)

Schema vật lý cho PostgreSQL 16, ánh xạ từ [T2](02-domain-model-erd.md). DDL dưới là **canonical** (EF Core migrations sinh từ đây/đồng bộ với đây). Quy ước: `snake_case`, PK `id uuid` (UUID v7), `organization_id` trên mọi bảng tenant-scoped, audit cột `created_at/updated_at/created_by/updated_by`.

---

## 1. Extensions & conventions

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pg_trgm;     -- fuzzy search
CREATE EXTENSION IF NOT EXISTS vector;      -- pgvector (RAG)
-- ltree optional: CREATE EXTENSION ltree;  -- alternative org-tree

-- Common audit columns convention (applied per table):
--   created_at timestamptz NOT NULL DEFAULT now(),
--   updated_at timestamptz NOT NULL DEFAULT now(),
--   created_by uuid, updated_by uuid
-- Soft delete where needed: deleted_at timestamptz NULL
```

Enums dùng `text` + `CHECK` (dễ migrate hơn native enum) hoặc lookup table khi cần CRUD.

---

## 2. Identity & Org

```sql
CREATE TABLE organizations (
  id           uuid PRIMARY KEY,
  name         text NOT NULL,
  slug         text NOT NULL UNIQUE,
  status       text NOT NULL DEFAULT 'active' CHECK (status IN ('active','suspended')),
  time_zone    text NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
  locale       text NOT NULL DEFAULT 'vi-VN',
  settings     jsonb NOT NULL DEFAULT '{}',   -- rank thresholds overrides, feature flags
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE departments (
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  parent_id       uuid REFERENCES departments(id) ON DELETE CASCADE,
  name            text NOT NULL,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_departments_org ON departments(organization_id);
CREATE INDEX ix_departments_parent ON departments(parent_id);

-- Closure table for fast subtree queries & branch-scoped permissions (ADR-004)
CREATE TABLE department_closure (
  ancestor_id   uuid NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
  descendant_id uuid NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
  depth         int  NOT NULL,
  PRIMARY KEY (ancestor_id, descendant_id)
);
CREATE INDEX ix_dept_closure_desc ON department_closure(descendant_id);

CREATE TABLE positions (
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  name            text NOT NULL,
  UNIQUE (organization_id, name)
);

CREATE TABLE users (
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  email           text NOT NULL,
  phone           text,
  full_name       text NOT NULL,
  employee_code   text,
  handle          text,
  avatar_url      text,
  department_id   uuid REFERENCES departments(id),
  position_id     uuid REFERENCES positions(id),
  audience_scope  text NOT NULL DEFAULT 'INTERNAL'
                    CHECK (audience_scope IN ('INTERNAL','PARTNER','GUEST')),
  password_hash   text,
  must_change_password boolean NOT NULL DEFAULT true,
  status          text NOT NULL DEFAULT 'active' CHECK (status IN ('active','locked','invited')),
  last_seen_at    timestamptz,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  deleted_at      timestamptz,
  UNIQUE (organization_id, email),
  UNIQUE (organization_id, employee_code)
);
CREATE INDEX ix_users_org_dept ON users(organization_id, department_id);
CREATE INDEX ix_users_org_scope ON users(organization_id, audience_scope);

CREATE TABLE roles (
  id              uuid PRIMARY KEY,
  organization_id uuid REFERENCES organizations(id) ON DELETE CASCADE, -- NULL = system role
  code            text NOT NULL,         -- OrgOwner, DeptManager, Instructor...
  name            text NOT NULL,
  is_system       boolean NOT NULL DEFAULT false,
  permissions     text[] NOT NULL DEFAULT '{}',  -- permission codes (see T5)
  UNIQUE (organization_id, code)
);

CREATE TABLE user_roles (
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  role_id uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
  PRIMARY KEY (user_id, role_id)
);

CREATE TABLE refresh_tokens (
  id              uuid PRIMARY KEY,
  user_id         uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash      text NOT NULL,
  expires_at      timestamptz NOT NULL,
  revoked_at      timestamptz,
  created_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_refresh_user ON refresh_tokens(user_id);
```

---

## 3. Categories / Skill groups

```sql
CREATE TABLE categories (        -- skill groups (6 seed) + sub-categories/domains
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  parent_id       uuid REFERENCES categories(id),
  name            text NOT NULL,
  sort_order      int NOT NULL DEFAULT 0
);
CREATE INDEX ix_categories_org ON categories(organization_id);
```

---

## 4. Media assets

```sql
CREATE TABLE media_assets (
  id               uuid PRIMARY KEY,
  organization_id  uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  kind             text NOT NULL CHECK (kind IN ('video','audio','file','scorm','image')),
  original_key     text NOT NULL,          -- S3 key of source upload
  hls_manifest_key text,                   -- S3 key of master .m3u8 (video)
  status           text NOT NULL DEFAULT 'uploaded'
                     CHECK (status IN ('uploaded','transcoding','ready','failed')),
  duration_seconds int,
  size_bytes       bigint,
  renditions       jsonb NOT NULL DEFAULT '[]',  -- [{height:720,bitrate,...}]
  meta             jsonb NOT NULL DEFAULT '{}',
  created_at       timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_media_org_status ON media_assets(organization_id, status);
```

---

## 5. Content & Publishing

```sql
CREATE TABLE courses (
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  title           text NOT NULL,
  slug            text NOT NULL,
  category_id     uuid REFERENCES categories(id),
  thumbnail_url   text,
  description_html text,                   -- TipTap rich-text (sanitized)
  status          text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','published','archived')),
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  UNIQUE (organization_id, slug)
);

CREATE TABLE modules (                     -- chapters
  id         uuid PRIMARY KEY,
  course_id  uuid NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  title      text NOT NULL,
  sort_order int  NOT NULL DEFAULT 0
);
CREATE INDEX ix_modules_course ON modules(course_id, sort_order);

CREATE TABLE lessons (
  id               uuid PRIMARY KEY,
  module_id        uuid NOT NULL REFERENCES modules(id) ON DELETE CASCADE,
  title            text NOT NULL,
  sort_order       int  NOT NULL DEFAULT 0,
  type             text NOT NULL DEFAULT 'video' CHECK (type IN ('video','document','scorm','quiz')),
  media_asset_id   uuid REFERENCES media_assets(id),
  document_id      uuid,                   -- FK added after documents table
  duration_seconds int,
  created_at       timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_lessons_module ON lessons(module_id, sort_order);

CREATE TABLE learning_paths (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  title text NOT NULL, description_html text,
  status text NOT NULL DEFAULT 'draft'
);
CREATE TABLE learning_path_items (
  path_id uuid NOT NULL REFERENCES learning_paths(id) ON DELETE CASCADE,
  course_id uuid NOT NULL REFERENCES courses(id),
  sort_order int NOT NULL DEFAULT 0,
  PRIMARY KEY (path_id, course_id)
);

CREATE TABLE training_types (              -- 7 seed + CRUD
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  name text NOT NULL, is_system boolean NOT NULL DEFAULT false,
  UNIQUE (organization_id, name)
);

-- Unified publication "facade" (polymorphic to content)
CREATE TABLE publications (
  id                 uuid PRIMARY KEY,
  organization_id    uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  kind               text NOT NULL CHECK (kind IN
                       ('course','learning_path','exam','training_session',
                        'document','document_set','podcast','video','scorm')),
  content_type       text NOT NULL,        -- table name of content
  content_id         uuid NOT NULL,        -- id in that table
  title              text NOT NULL,
  status             text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','published','archived')),
  audience_scopes    text[] NOT NULL DEFAULT '{}', -- INTERNAL/PARTNER/GUEST
  is_public          boolean NOT NULL DEFAULT false,
  sequential         boolean NOT NULL DEFAULT false,
  continue_after_expiry boolean NOT NULL DEFAULT false,
  training_type_id   uuid REFERENCES training_types(id),
  skill_domain_id    uuid REFERENCES categories(id),
  completion_points  int  NOT NULL DEFAULT 0,
  training_hours     numeric(6,2) NOT NULL DEFAULT 0,
  expires_after_days int,
  published_at       timestamptz,
  published_by       uuid REFERENCES users(id),
  created_at         timestamptz NOT NULL DEFAULT now(),
  updated_at         timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_pub_org_status ON publications(organization_id, status);
CREATE INDEX ix_pub_content ON publications(content_type, content_id);
CREATE INDEX ix_pub_scopes ON publications USING gin (audience_scopes);

-- Audience targeting (who can access a publication)
CREATE TABLE publication_targets (
  id             uuid PRIMARY KEY,
  publication_id uuid NOT NULL REFERENCES publications(id) ON DELETE CASCADE,
  target_type    text NOT NULL CHECK (target_type IN ('user','department','position','group')),
  target_id      uuid NOT NULL
);
CREATE INDEX ix_pub_targets ON publication_targets(publication_id);
CREATE INDEX ix_pub_targets_lookup ON publication_targets(target_type, target_id);
```

---

## 6. Exams & Question Bank

```sql
CREATE TABLE question_banks (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  name text NOT NULL
);
CREATE TABLE questions (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  bank_id uuid REFERENCES question_banks(id) ON DELETE SET NULL,
  stem text NOT NULL,
  type text NOT NULL DEFAULT 'single' CHECK (type IN ('single','multiple','truefalse','text')),
  options jsonb NOT NULL DEFAULT '[]',     -- [{id,label}]
  answer  jsonb NOT NULL DEFAULT '[]',     -- correct option ids
  tags text[] NOT NULL DEFAULT '{}',
  search_tsv tsvector
);
CREATE INDEX ix_questions_search ON questions USING gin (search_tsv);

CREATE TABLE exams (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  title text NOT NULL, pass_score numeric(5,2) NOT NULL DEFAULT 0,
  duration_minutes int, attempts_allowed int NOT NULL DEFAULT 1, shuffle boolean NOT NULL DEFAULT true
);
CREATE TABLE exam_questions (
  exam_id uuid NOT NULL REFERENCES exams(id) ON DELETE CASCADE,
  question_id uuid NOT NULL REFERENCES questions(id),
  sort_order int NOT NULL DEFAULT 0, points numeric(5,2) NOT NULL DEFAULT 1,
  PRIMARY KEY (exam_id, question_id)
);
```

---

## 7. Learning (enrollment & progress)

```sql
CREATE TABLE enrollments (
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  user_id         uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  publication_id  uuid NOT NULL REFERENCES publications(id) ON DELETE CASCADE,
  source          text NOT NULL DEFAULT 'assigned' CHECK (source IN ('assigned','self')),
  status          text NOT NULL DEFAULT 'not_started'
                    CHECK (status IN ('not_started','in_progress','completed','expired')),
  progress_percent numeric(5,2) NOT NULL DEFAULT 0,
  started_at      timestamptz, completed_at timestamptz, expires_at timestamptz,
  created_at      timestamptz NOT NULL DEFAULT now(),
  UNIQUE (user_id, publication_id)
);
CREATE INDEX ix_enroll_user ON enrollments(user_id, status);
CREATE INDEX ix_enroll_pub ON enrollments(publication_id, status);

CREATE TABLE lesson_progress (
  id            uuid PRIMARY KEY,
  enrollment_id uuid NOT NULL REFERENCES enrollments(id) ON DELETE CASCADE,
  lesson_id     uuid NOT NULL REFERENCES lessons(id),
  status        text NOT NULL DEFAULT 'not_started'
                  CHECK (status IN ('not_started','in_progress','completed')),
  watched_seconds int NOT NULL DEFAULT 0,
  watch_ratio   numeric(4,3) NOT NULL DEFAULT 0,   -- anti-cheat (0..1)
  completed_at  timestamptz,
  UNIQUE (enrollment_id, lesson_id)
);

CREATE TABLE exam_attempts (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL,
  exam_id uuid NOT NULL REFERENCES exams(id), user_id uuid NOT NULL REFERENCES users(id),
  answers jsonb NOT NULL DEFAULT '[]', score numeric(5,2), passed boolean,
  started_at timestamptz NOT NULL DEFAULT now(), submitted_at timestamptz
);
CREATE INDEX ix_attempts_user ON exam_attempts(user_id, exam_id);
```

---

## 8. Instructor-led training

```sql
CREATE TABLE training_sessions (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  type text NOT NULL CHECK (type IN ('class','event','workshop')),
  title text NOT NULL, training_type_id uuid REFERENCES training_types(id),
  starts_at timestamptz, ends_at timestamptz, location text, is_online boolean DEFAULT false,
  capacity int, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE session_instructors (
  session_id uuid REFERENCES training_sessions(id) ON DELETE CASCADE,
  user_id uuid REFERENCES users(id), PRIMARY KEY (session_id, user_id)
);
CREATE TABLE attendances (
  id uuid PRIMARY KEY, session_id uuid NOT NULL REFERENCES training_sessions(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES users(id),
  present boolean NOT NULL DEFAULT false, score numeric(5,2),
  evaluation_score numeric(5,2),  -- learner rating of session
  UNIQUE (session_id, user_id)
);
```

---

## 9. Training management (frameworks & assignments)

```sql
CREATE TABLE training_hours_frameworks (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  scope text NOT NULL CHECK (scope IN ('department','position')),
  target_id uuid NOT NULL,           -- department_id | position_id
  required_hours numeric(6,2) NOT NULL,
  period text NOT NULL DEFAULT 'year' CHECK (period IN ('year','quarter')),
  tags text[] NOT NULL DEFAULT '{}'
);

CREATE TABLE assignments (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  publication_id uuid NOT NULL REFERENCES publications(id) ON DELETE CASCADE,
  target_type text NOT NULL CHECK (target_type IN ('user','department','position','group')),
  target_id uuid NOT NULL, due_at timestamptz, assigned_by uuid REFERENCES users(id),
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_assign_target ON assignments(target_type, target_id);
```

---

## 10. Gamification

```sql
CREATE TABLE ranks (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  sort_order int NOT NULL, code text NOT NULL, name text NOT NULL, min_points int NOT NULL,
  UNIQUE (organization_id, code)
);   -- seed 9: Bronze..Challenger

CREATE TABLE learner_profiles (
  user_id uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
  organization_id uuid NOT NULL,
  total_points int NOT NULL DEFAULT 0,
  current_rank_id uuid REFERENCES ranks(id),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE point_transactions (         -- append-only source of truth
  id uuid PRIMARY KEY, organization_id uuid NOT NULL,
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  amount int NOT NULL,
  reason text NOT NULL,                   -- lesson_completed/course_completed/exam_passed/framework...
  ref_type text, ref_id uuid,
  audience_scope text NOT NULL,
  department_id uuid, position_id uuid,   -- denormalized for leaderboard grouping
  occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_points_user_time ON point_transactions(user_id, occurred_at);
CREATE INDEX ix_points_org_time ON point_transactions(organization_id, occurred_at);

CREATE TABLE stickers (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL, code text NOT NULL, name text NOT NULL,
  criteria jsonb NOT NULL DEFAULT '{}'
);
CREATE TABLE learner_stickers (
  user_id uuid REFERENCES users(id) ON DELETE CASCADE, sticker_id uuid REFERENCES stickers(id),
  awarded_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY (user_id, sticker_id)
);
```
> Leaderboard đọc/ghi nhanh qua **Redis ZSET** (key theo org/scope/group/period); Postgres `point_transactions` là nguồn để rebuild — xem [T7](07-gamification-reporting.md).

---

## 11. DMS

```sql
CREATE TABLE folders (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  parent_id uuid REFERENCES folders(id) ON DELETE CASCADE,
  name text NOT NULL,
  owner_scope text NOT NULL DEFAULT 'department' CHECK (owner_scope IN ('department','user')),
  owner_id uuid NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE documents (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  folder_id uuid REFERENCES folders(id) ON DELETE SET NULL,
  title text NOT NULL, type text, media_asset_id uuid REFERENCES media_assets(id),
  file_key text, visibility text NOT NULL DEFAULT 'department',
  search_tsv tsvector, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_docs_search ON documents USING gin (search_tsv);
-- now add deferred FK from lessons.document_id
ALTER TABLE lessons ADD CONSTRAINT fk_lessons_document
  FOREIGN KEY (document_id) REFERENCES documents(id);

CREATE TABLE shares (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL,
  resource_type text NOT NULL CHECK (resource_type IN ('folder','document')),
  resource_id uuid NOT NULL,
  granted_to_type text NOT NULL CHECK (granted_to_type IN ('user','department')),
  granted_to_id uuid NOT NULL,
  permission text NOT NULL DEFAULT 'read' CHECK (permission IN ('read','edit')),
  UNIQUE (resource_type, resource_id, granted_to_type, granted_to_id)
);
```

---

## 12. AI

```sql
CREATE TABLE ai_sessions (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL, user_id uuid NOT NULL REFERENCES users(id),
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE ai_messages (
  id uuid PRIMARY KEY, session_id uuid NOT NULL REFERENCES ai_sessions(id) ON DELETE CASCADE,
  role text NOT NULL CHECK (role IN ('user','assistant','system')),
  content text NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE ai_drafts (
  id uuid PRIMARY KEY, session_id uuid REFERENCES ai_sessions(id) ON DELETE SET NULL,
  organization_id uuid NOT NULL, kind text NOT NULL CHECK (kind IN ('lesson','video','course','lookup')),
  content jsonb NOT NULL, source_refs jsonb NOT NULL DEFAULT '[]',
  status text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','accepted','discarded'))
);
-- RAG embeddings
CREATE TABLE doc_embeddings (
  id uuid PRIMARY KEY, organization_id uuid NOT NULL,
  source_type text NOT NULL, source_id uuid NOT NULL,
  chunk text NOT NULL, embedding vector(1536)
);
CREATE INDEX ix_doc_embeddings_ann ON doc_embeddings USING hnsw (embedding vector_cosine_ops);
```

---

## 13. Analytics, audit & outbox

```sql
CREATE TABLE learning_events (             -- append-only analytics (see P4 §3.2)
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  organization_id uuid NOT NULL, user_id uuid, audience_scope text,
  type text NOT NULL, props jsonb NOT NULL DEFAULT '{}',
  occurred_at timestamptz NOT NULL DEFAULT now()
) PARTITION BY RANGE (occurred_at);        -- monthly partitions
CREATE INDEX ix_events_org_type_time ON learning_events(organization_id, type, occurred_at);

CREATE TABLE audit_logs (
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  organization_id uuid, actor_id uuid, action text NOT NULL,
  resource_type text, resource_id uuid, before jsonb, after jsonb,
  ip inet, occurred_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE outbox_messages (             -- reliable domain-event dispatch
  id uuid PRIMARY KEY, type text NOT NULL, payload jsonb NOT NULL,
  occurred_at timestamptz NOT NULL DEFAULT now(), processed_at timestamptz, attempts int NOT NULL DEFAULT 0
);
CREATE INDEX ix_outbox_unprocessed ON outbox_messages(processed_at) WHERE processed_at IS NULL;
```

---

## 14. Tenant isolation (RLS) — tóm tắt

Bật **Row-Level Security** trên các bảng tenant-scoped; policy dùng GUC `app.current_org`:

```sql
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON users
  USING (organization_id = current_setting('app.current_org')::uuid);
-- App đặt SET LOCAL app.current_org = '<org>' đầu mỗi request/transaction.
```
Chi tiết chiến lược RLS + cách app set context, và edge cases: [T5 §5](05-auth-rbac-tenancy.md).

---

## 15. Migrations & seed
- EF Core migrations (code-first) đồng bộ với DDL trên; mỗi PR thêm migration, không sửa migration cũ đã merge.
- **Seed mỗi organization mới:** 9 `ranks`, 7 `training_types` (is_system), 6 `categories` (skill groups), system `roles`. (Xem [P3 E14](../product-development/03-features-user-stories.md).)
- Index review định kỳ theo query thực tế (xem [T9 perf](09-infra-security-nfr.md)).
