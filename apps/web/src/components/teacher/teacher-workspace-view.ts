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
