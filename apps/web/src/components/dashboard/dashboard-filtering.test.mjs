import assert from "node:assert/strict";
import test from "node:test";

import { filterDashboardItems } from "./dashboard-filtering.ts";

const assignments = [
  { status: "Draft", title: "Essay outline" },
  { status: "Published", title: "Science report" },
];

test("filters only the already-authorized items by search text and status", () => {
  assert.deepEqual(filterDashboardItems(assignments, "report", "Published"), [assignments[1]]);
});

test("keeps all already-authorized items when filters are empty", () => {
  assert.deepEqual(filterDashboardItems(assignments, "", "All"), assignments);
});
