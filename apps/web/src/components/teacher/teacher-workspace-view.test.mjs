import assert from "node:assert/strict";
import test from "node:test";

import { getTeacherWorkspacePanelVisibility } from "./teacher-workspace-view.ts";

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
