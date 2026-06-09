"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { completeLesson } from "@/lib/api";

/**
 * Video lesson player for the first learning-loop slice: a plain HTML5 video (no HLS/watermark yet)
 * plus a mark-complete control. The complete button is always enabled in this slice (watch-ratio
 * gating is deferred); completing does not stop the video. Render with a `key={lessonId}` so switching
 * lessons remounts and resets the load-error state.
 */
export function LessonPlayer({
  enrollmentId,
  lessonId,
  title,
  videoUrl,
  completed,
}: {
  enrollmentId: string;
  lessonId: string;
  title: string;
  videoUrl: string;
  completed: boolean;
}) {
  const queryClient = useQueryClient();
  const [videoError, setVideoError] = useState(false);

  const mutation = useMutation({
    mutationFn: () => completeLesson(enrollmentId, lessonId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["enrolled-course", enrollmentId] });
      queryClient.invalidateQueries({ queryKey: ["my-enrollments"] });
    },
  });

  return (
    <div className="grid gap-4">
      <div className="overflow-hidden rounded-lg border border-border bg-ink-rail">
        {videoError ? (
          <div className="grid aspect-video place-items-center p-6 text-center text-sm font-medium text-white/80">
            Không tải được video. Kiểm tra đường dẫn hoặc thử lại sau.
          </div>
        ) : (
          <video
            src={videoUrl}
            controls
            aria-label={title}
            className="aspect-video w-full bg-black"
            onError={() => setVideoError(true)}
          />
        )}
      </div>

      <div className="flex flex-wrap items-center gap-3">
        {completed ? (
          <span className="inline-flex min-h-11 items-center gap-2 rounded-lg bg-success-subtle px-4 text-sm font-semibold text-success">
            <span aria-hidden="true">✓</span> Đã hoàn thành
          </span>
        ) : (
          <button
            type="button"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
            className="vela-focus inline-flex min-h-11 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {mutation.isPending ? "Đang lưu..." : "Đánh dấu hoàn thành"}
          </button>
        )}
        {mutation.isError ? (
          <p className="text-sm font-medium text-danger" role="alert">
            Không lưu được. Vui lòng thử lại.
          </p>
        ) : null}
      </div>
    </div>
  );
}
