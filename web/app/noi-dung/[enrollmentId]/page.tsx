"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ApiError, AuthRequiredError, getEnrolledCourse, type EnrolledLesson } from "@/lib/api";
import { useRequireAuth } from "@/lib/use-auth";
import { ProgressBar, StatusPill } from "@/components/vela/ui";
import { LessonPlayer } from "@/components/learning/lesson-player";

export default function EnrolledCoursePage() {
  const router = useRouter();
  const params = useParams<{ enrollmentId: string }>();
  const enrollmentId = params.enrollmentId;
  const status = useRequireAuth();
  const [selectedLessonId, setSelectedLessonId] = useState<string | null>(null);

  const { data: course, isPending, isError, error } = useQuery({
    queryKey: ["enrolled-course", enrollmentId],
    queryFn: () => getEnrolledCourse(enrollmentId),
    enabled: status === "authenticated",
    retry: false,
  });

  useEffect(() => {
    if (isError && error instanceof AuthRequiredError) router.replace("/login");
    else if (isError && error instanceof ApiError && error.status === 403) router.replace("/change-password");
  }, [isError, error, router]);

  // The selected lesson defaults to the first one until the learner picks another (no effect needed).
  const allLessons: EnrolledLesson[] = course ? course.modules.flatMap((m) => m.lessons) : [];
  const selected = allLessons.find((l) => l.id === selectedLessonId) ?? allLessons[0] ?? null;

  return (
    <main className="workspace-page min-h-screen">
      <div className="workspace-pad">
        <Link href="/cua-ban" className="vela-focus inline-flex items-center gap-1.5 text-sm font-semibold text-primary hover:underline">
          <span aria-hidden="true">←</span> Khóa học của bạn
        </Link>

        {status === "checking" || isPending ? (
          <div className="mt-5 grid gap-4 lg:grid-cols-[320px_1fr]" aria-label="Đang tải">
            <div className="h-72 animate-pulse rounded-xl bg-learning-apricot/50" />
            <div className="h-72 animate-pulse rounded-xl bg-learning-apricot/50" />
          </div>
        ) : isError ? (
          <div className="mt-5 rounded-xl border border-danger/25 bg-danger-subtle p-5 text-sm font-medium text-danger" role="alert">
            {error instanceof ApiError && error.status === 404
              ? "Không tìm thấy khóa học này trong danh sách được giao của bạn."
              : "Không tải được khóa học. Vui lòng thử lại."}
          </div>
        ) : (
          <>
            <header className="mt-4">
              <h1 className="text-2xl font-black leading-tight text-foreground">{course!.courseTitle}</h1>
              <div className="mt-3 flex max-w-md items-center gap-3">
                <ProgressBar value={course!.progressPercent} tone="primary" />
                <span className="font-mono text-sm font-semibold text-primary">{course!.progressPercent}%</span>
                <StatusPill tone={course!.status === "Completed" ? "success" : "warm"}>
                  {course!.status === "Completed" ? "Hoàn thành" : course!.status === "InProgress" ? "Đang học" : "Chưa bắt đầu"}
                </StatusPill>
              </div>
            </header>

            <div className="mt-5 grid gap-5 lg:grid-cols-[320px_1fr]">
              {/* Module / lesson list — nested ordered lists for programmatic structure. */}
              <nav className="panel p-3" aria-label="Nội dung khóa học">
                <ol className="grid gap-4">
                  {course!.modules.map((module) => (
                    <li key={module.id}>
                      <p className="px-2 py-1 text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{module.title}</p>
                      <ol className="mt-1 grid gap-1">
                        {module.lessons.map((lesson) => {
                          const isActive = selected?.id === lesson.id;
                          return (
                            <li key={lesson.id}>
                              <button
                                type="button"
                                onClick={() => setSelectedLessonId(lesson.id)}
                                aria-current={isActive ? "true" : undefined}
                                aria-label={`${lesson.title} — ${lesson.completed ? "đã hoàn thành" : "chưa hoàn thành"}`}
                                className={`vela-focus flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-left text-sm font-medium transition-colors ${
                                  isActive ? "bg-primary-subtle text-primary" : "text-foreground hover:bg-surface-raised"
                                }`}
                              >
                                <span className="min-w-0 truncate">{lesson.title}</span>
                                {lesson.completed ? (
                                  <StatusPill tone="success">✓</StatusPill>
                                ) : (
                                  <span className="shrink-0 text-xs font-medium text-muted">{Math.round(lesson.durationSeconds / 60)}′</span>
                                )}
                              </button>
                            </li>
                          );
                        })}
                      </ol>
                    </li>
                  ))}
                </ol>
              </nav>

              {/* Player for the selected lesson. */}
              <section className="panel p-5">
                {selected ? (
                  <>
                    <h2 className="mb-4 text-lg font-bold text-foreground">{selected.title}</h2>
                    <LessonPlayer
                      key={selected.id}
                      enrollmentId={course!.enrollmentId}
                      lessonId={selected.id}
                      title={selected.title}
                      videoUrl={selected.videoUrl}
                      completed={selected.completed}
                    />
                  </>
                ) : (
                  <p className="text-sm font-medium text-muted">Khóa học này chưa có bài học.</p>
                )}
              </section>
            </div>
          </>
        )}
      </div>
    </main>
  );
}
