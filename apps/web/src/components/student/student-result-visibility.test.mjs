import assert from "node:assert/strict";
import test from "node:test";

import { shouldShowStudentResult } from "./student-result-visibility.ts";

test("graded results stay hidden before the deadline", () => {
  assert.equal(shouldShowStudentResult({
    deadlinePassed: false,
    marks: 18,
    status: "Graded",
  }), false);
});

test("graded results appear after the deadline", () => {
  assert.equal(shouldShowStudentResult({
    deadlinePassed: true,
    marks: 18,
    status: "Graded",
  }), true);
});

test("results stay hidden after the deadline until grading is finalized", () => {
  assert.equal(shouldShowStudentResult({
    deadlinePassed: true,
    marks: null,
    status: "UnderReview",
  }), false);
});
