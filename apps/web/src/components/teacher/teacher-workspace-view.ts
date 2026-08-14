export interface TeacherWorkspacePanelVisibility {
  assignmentManagement: boolean;
  submissionReview: boolean;
}

export function getTeacherWorkspacePanelVisibility(
  isReviewingSubmission: boolean,
): TeacherWorkspacePanelVisibility {
  return {
    assignmentManagement: !isReviewingSubmission,
    submissionReview: isReviewingSubmission,
  };
}

interface SubmissionReviewSectionOptions {
  hasAttachment: boolean;
  isGraded: boolean;
}

export function getSubmissionReviewSectionVisibility({
  hasAttachment,
  isGraded,
}: SubmissionReviewSectionOptions) {
  return {
    writtenResponse: true,
    attachment: hasAttachment,
    finalResult: isGraded,
  };
}

export function selectSubmissionAfterRefresh<T extends { id: string }>(
  submissions: readonly T[],
  selectedSubmissionId: string | null,
): T | null {
  return submissions.find((submission) => submission.id === selectedSubmissionId)
    ?? submissions[0]
    ?? null;
}
