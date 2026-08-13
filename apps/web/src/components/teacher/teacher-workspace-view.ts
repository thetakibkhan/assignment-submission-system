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
