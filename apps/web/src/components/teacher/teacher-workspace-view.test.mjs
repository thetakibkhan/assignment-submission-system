import assert from "node:assert/strict";
import test from "node:test";

import {
  getSubmissionReviewSectionVisibility,
  getTeacherWorkspacePanelVisibility,
} from "./teacher-workspace-view.ts";

test("assignment management is the only visible workflow by default", () => {
  const visibility = getTeacherWorkspacePanelVisibility(false);

  assert.deepEqual(visibility, {
    assignmentManagement: true,
    submissionReview: false,
  });
});

test("submission review replaces assignment management", () => {
  const visibility = getTeacherWorkspacePanelVisibility(true);

  assert.deepEqual(visibility, {
    assignmentManagement: false,
    submissionReview: true,
  });
});


test("submission content keeps the answer, attachment, and result in distinct sections", () => {
  const visibility = getSubmissionReviewSectionVisibility({
    hasAttachment: true,
    isGraded: true,
  });

  assert.deepEqual(visibility, {
    writtenResponse: true,
    attachment: true,
    finalResult: true,
  });
});

test("optional review sections stay hidden when no file or result exists", () => {
  const visibility = getSubmissionReviewSectionVisibility({
    hasAttachment: false,
    isGraded: false,
  });

  assert.deepEqual(visibility, {
    writtenResponse: true,
    attachment: false,
    finalResult: false,
  });
});
